#!/usr/bin/env python3
"""
generate_dashboard.py
Reads QuizScores.xlsx and writes a self-contained leaderboard HTML file
with all data embedded directly — no JSON file, no server needed.

Usage:
  python generate_dashboard.py
  python generate_dashboard.py --excel "\\\\Server\\Share\\QuizScores.xlsx" --out leaderboard.html
  python generate_dashboard.py --watch          # regenerate every 10 seconds (live mode)
  python generate_dashboard.py --title "June Quiz"
"""
import sys, argparse, time
from pathlib import Path
from datetime import datetime

try:
    import openpyxl
except ImportError:
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "openpyxl"])
    import openpyxl


# ── Read Excel ────────────────────────────────────────────────────────────────
def read_excel(excel_path: str) -> list[dict]:
    wb   = openpyxl.load_workbook(excel_path, read_only=True, data_only=True)
    ws   = wb.active
    rows = list(ws.iter_rows(values_only=True))
    # Row 0 = title banner, Row 1 = headers, Row 2+ = data
    entries = []
    for row in rows[2:]:
        if not row or row[1] is None:
            continue
        try:
            name      = str(row[1]).strip()
            score     = int(row[2])
            total     = int(row[3])
            time_sec  = int(row[5]) if row[5] is not None else 99999
            time_disp = str(row[6]) if row[6] else f"{time_sec}s"
            submitted = str(row[7]) if row[7] else "—"
            pct       = round(score / total * 100, 1) if total else 0
            entries.append(dict(name=name, score=score, total=total,
                                pct=pct, time_sec=time_sec,
                                time_disp=time_disp, submitted=submitted))
        except Exception as e:
            print(f"  Skipping row: {e}")
    wb.close()

    # Sort: highest score first, then fastest time
    entries.sort(key=lambda x: (-x["score"], x["time_sec"]))

    # Assign ranks (ties share a rank)
    rank = 1
    for i, e in enumerate(entries):
        if i > 0:
            p = entries[i - 1]
            if e["score"] != p["score"] or e["time_sec"] != p["time_sec"]:
                rank = i + 1
        e["rank"] = rank

    return entries


# ── Build HTML rows ───────────────────────────────────────────────────────────
def esc(s: str) -> str:
    return str(s).replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")

def pct_class(pct):
    return "pct-green" if pct >= 80 else "pct-amber" if pct >= 60 else "pct-red"

def build_podium(top3: list[dict]) -> str:
    if not top3:
        return '<p class="empty-sub">Waiting for participants…</p>'
    medals  = ["🥇","🥈","🥉"]
    # Classic layout: 2nd | 1st | 3rd
    order = [top3[1], top3[0], top3[2]] if len(top3) >= 3 else \
            [top3[1], top3[0]]          if len(top3) == 2 else top3
    cards = []
    for e in order:
        r     = e["rank"]
        cls   = f"rank-{r}" if r <= 3 else "rank-n"
        medal = medals[r-1] if r <= 3 else f"#{r}"
        cards.append(f"""
        <div class="podium-card {cls}">
          <span class="medal">{medal}</span>
          <div class="podium-name" title="{esc(e['name'])}">{esc(e['name'])}</div>
          <div class="podium-score">{e['score']}/{e['total']}</div>
          <div class="podium-pct">{e['pct']}%</div>
          <div class="podium-time">⏱ {esc(e['time_disp'])}</div>
        </div>""")
    return "\n".join(cards)

def build_rows(entries: list[dict]) -> str:
    if not entries:
        return ""
    min_time = min(e["time_sec"] for e in entries)
    rows = []
    for e in entries:
        r       = e["rank"]
        rbadge  = f"r{r}" if r <= 3 else "rn"
        row_cls = "row-1st" if r == 1 else ""
        bar_pct = round(e["score"] / e["total"] * 100, 1) if e["total"] else 0
        pc      = pct_class(e["pct"])
        fastest = e["time_sec"] == min_time and len(entries) > 1
        t_cls   = "time-fast" if fastest else "time-cell"
        bolt    = " ⚡" if fastest else ""
        rows.append(f"""
      <tr class="{row_cls}">
        <td class="center"><span class="rank-badge {rbadge}">{r}</span></td>
        <td class="name-cell">{esc(e['name'])}</td>
        <td>
          <div class="score-bar-wrap">
            <div class="score-bar"><div class="score-bar-fill" style="width:{bar_pct}%"></div></div>
            <span class="score-num">{e['score']}/{e['total']}</span>
          </div>
        </td>
        <td class="center"><span class="pct-pill {pc}">{e['pct']}%</span></td>
        <td class="center {t_cls}">{esc(e['time_disp'])}{bolt}</td>
        <td class="center" style="font-size:11px;color:var(--muted)">{esc(e['submitted'])}</td>
      </tr>""")
    return "\n".join(rows)

def build_stats(entries, total_q) -> dict:
    if not entries:
        return dict(count="—", top="—", fastest="—", avg="—", full="—")
    count   = len(entries)
    top     = f"{entries[0]['score']}/{total_q}"
    fast_e  = min(entries, key=lambda e: e["time_sec"])
    avg     = f"{round(sum(e['pct'] for e in entries)/count,1)}%"
    full    = sum(1 for e in entries if e["score"] == total_q)
    return dict(count=count, top=top,
                fastest=fast_e["time_disp"], avg=avg,
                full=full if full > 0 else "0")


# ── HTML template ─────────────────────────────────────────────────────────────
HTML = """<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>{title}</title>
  <style>
    *,*::before,*::after{{box-sizing:border-box;margin:0;padding:0}}
    :root{{
      --blue:#2962AC;--blue-dk:#1A3F7A;--blue-lt:#E8F0FB;
      --gold:#F5A623;--silver:#9CA3AF;--bronze:#CD7F32;
      --green:#1A7A3F;--green-lt:#E4F5EB;
      --amber:#9A5500;--amber-lt:#FEF3E2;
      --red:#B91C1C;--red-lt:#FEE8E6;
      --bg:#F4F7FB;--white:#FFF;--text:#1A202C;
      --muted:#718096;--border:#DDE3EE;
      --shadow:0 2px 12px rgba(41,98,172,.10);
      --radius:12px;
    }}
    body{{font-family:"Segoe UI",Arial,sans-serif;background:var(--bg);color:var(--text);min-height:100vh}}

    /* Header */
    .header{{background:linear-gradient(135deg,var(--blue-dk),var(--blue));color:#fff;
             padding:0 32px;height:72px;display:flex;align-items:center;
             justify-content:space-between;box-shadow:0 2px 16px rgba(0,0,0,.18);
             position:sticky;top:0;z-index:100}}
    .h-left{{display:flex;align-items:center;gap:14px}}
    .h-icon{{font-size:28px}}
    .h-title{{font-size:20px;font-weight:700}}
    .h-sub{{font-size:12px;color:rgba(255,255,255,.65);margin-top:2px}}
    .h-right{{display:flex;align-items:center;gap:16px}}
    .live-badge{{display:flex;align-items:center;gap:6px;background:rgba(255,255,255,.15);
                 border:1px solid rgba(255,255,255,.25);border-radius:20px;
                 padding:5px 14px;font-size:12px;font-weight:600;letter-spacing:.5px}}
    .live-dot{{width:8px;height:8px;background:#4ADE80;border-radius:50%;
               animation:pulse 1.4s infinite}}
    @keyframes pulse{{0%,100%{{opacity:1;transform:scale(1)}}50%{{opacity:.5;transform:scale(.75)}}}}
    .updated-txt{{font-size:11px;color:rgba(255,255,255,.6)}}

    /* Stats */
    .stats-row{{display:flex;gap:14px;padding:20px 32px 0;flex-wrap:wrap}}
    .stat-card{{background:var(--white);border-radius:var(--radius);border:1px solid var(--border);
                box-shadow:var(--shadow);padding:16px 22px;flex:1;min-width:120px}}
    .stat-value{{font-size:26px;font-weight:800;color:var(--blue)}}
    .stat-label{{font-size:11px;color:var(--muted);text-transform:uppercase;
                 letter-spacing:.6px;font-weight:600;margin-top:3px}}

    /* Podium */
    .section{{padding:20px 32px 0}}
    .section-title{{font-size:13px;font-weight:700;color:var(--muted);
                    text-transform:uppercase;letter-spacing:.8px;margin-bottom:14px;
                    display:flex;align-items:center;gap:8px}}
    .section-title::after{{content:"";flex:1;height:1px;background:var(--border)}}
    .podium{{display:flex;justify-content:center;align-items:flex-end;gap:14px;margin-bottom:4px}}
    .podium-card{{background:var(--white);border:2px solid var(--border);
                  border-radius:var(--radius);padding:18px 16px 14px;
                  text-align:center;flex:1;max-width:210px;
                  box-shadow:var(--shadow);transition:transform .2s}}
    .podium-card:hover{{transform:translateY(-3px)}}
    .podium-card.rank-1{{border-color:var(--gold);box-shadow:0 4px 20px rgba(245,166,35,.22);padding-top:26px}}
    .podium-card.rank-2{{border-color:var(--silver)}}
    .podium-card.rank-3{{border-color:var(--bronze)}}
    .medal{{font-size:32px;display:block;margin-bottom:6px}}
    .podium-name{{font-size:14px;font-weight:700;white-space:nowrap;
                  overflow:hidden;text-overflow:ellipsis}}
    .podium-score{{font-size:24px;font-weight:800;margin:4px 0}}
    .rank-1 .podium-score{{color:var(--gold)}}
    .rank-2 .podium-score{{color:var(--silver)}}
    .rank-3 .podium-score{{color:var(--bronze)}}
    .podium-pct{{font-size:12px;font-weight:600;color:var(--muted)}}
    .podium-time{{font-size:11px;color:var(--muted);background:var(--bg);
                  border-radius:20px;padding:2px 10px;display:inline-block;margin-top:5px}}

    /* Table */
    .table-section{{padding:20px 32px 32px}}
    .table-wrap{{background:var(--white);border-radius:var(--radius);
                 border:1px solid var(--border);box-shadow:var(--shadow);overflow:hidden}}
    table{{width:100%;border-collapse:collapse}}
    thead th{{background:var(--blue);color:#fff;font-size:11px;font-weight:700;
              text-transform:uppercase;letter-spacing:.7px;padding:13px 16px;text-align:left}}
    thead th.center{{text-align:center}}
    tbody tr{{border-bottom:1px solid var(--border);transition:background .15s}}
    tbody tr:last-child{{border-bottom:none}}
    tbody tr:hover{{background:var(--blue-lt)}}
    tbody tr.row-1st{{background:#FFFBEB}}
    tbody tr.row-1st:hover{{background:#FEF3C7}}
    td{{padding:12px 16px;font-size:13.5px}}
    td.center{{text-align:center}}
    .rank-badge{{display:inline-flex;align-items:center;justify-content:center;
                 width:28px;height:28px;border-radius:50%;font-weight:800;font-size:13px}}
    .r1{{background:#FEF3C7;color:#92400E;border:2px solid var(--gold)}}
    .r2{{background:#F1F5F9;color:#475569;border:2px solid var(--silver)}}
    .r3{{background:#FEF2E8;color:#7C3D0A;border:2px solid var(--bronze)}}
    .rn{{background:var(--bg);color:var(--muted);border:1px solid var(--border)}}
    .name-cell{{font-weight:600}}
    .score-bar-wrap{{display:flex;align-items:center;gap:10px}}
    .score-bar{{flex:1;height:8px;background:var(--border);border-radius:4px;overflow:hidden}}
    .score-bar-fill{{height:100%;border-radius:4px;
                     background:linear-gradient(90deg,var(--blue),#5B9BF8)}}
    .score-num{{font-weight:700;color:var(--blue);min-width:38px}}
    .pct-pill{{display:inline-block;padding:2px 10px;border-radius:20px;
               font-size:11px;font-weight:700}}
    .pct-green{{background:var(--green-lt);color:var(--green)}}
    .pct-amber{{background:var(--amber-lt);color:var(--amber)}}
    .pct-red{{background:var(--red-lt);color:var(--red)}}
    .time-cell{{color:var(--muted);font-size:12.5px}}
    .time-fast{{color:var(--green);font-weight:700}}

    /* Empty */
    .empty{{text-align:center;padding:60px 20px;color:var(--muted)}}
    .empty-icon{{font-size:48px;margin-bottom:12px}}
    .empty-title{{font-size:16px;font-weight:600;margin-bottom:6px;color:var(--text)}}
    .empty-sub{{font-size:13px}}

    @media(max-width:700px){{
      .header,.stats-row,.section,.table-section{{padding-left:14px;padding-right:14px}}
      .podium-card{{max-width:130px;padding:12px 8px}}
      td,thead th{{padding:9px 10px;font-size:12px}}
    }}
  </style>
</head>
<body>

<div class="header">
  <div class="h-left">
    <span class="h-icon">🏆</span>
    <div>
      <div class="h-title">{title}</div>
      <div class="h-sub">Fastest Fingers First  •  Live Rankings</div>
    </div>
  </div>
  <div class="h-right">
    <span class="updated-txt">Generated {generated}</span>
    <div class="live-badge"><span class="live-dot"></span> LIVE</div>
  </div>
</div>

<div class="stats-row">
  <div class="stat-card"><div class="stat-value">{s_count}</div><div class="stat-label">Participants</div></div>
  <div class="stat-card"><div class="stat-value">{s_top}</div><div class="stat-label">Top Score</div></div>
  <div class="stat-card"><div class="stat-value">{s_fastest}</div><div class="stat-label">Fastest Time</div></div>
  <div class="stat-card"><div class="stat-value">{s_avg}</div><div class="stat-label">Avg Score</div></div>
  <div class="stat-card"><div class="stat-value">{s_full}</div><div class="stat-label">Full Marks</div></div>
</div>

<div class="section">
  <div class="section-title">🥇 Top 3</div>
  <div class="podium">{podium}</div>
</div>

<div class="table-section">
  <div class="section-title">📋 Full Leaderboard</div>
  <div class="table-wrap">{table}</div>
</div>

</body>
</html>"""

TABLE_FULL = """
    <table>
      <thead>
        <tr>
          <th class="center" style="width:54px">Rank</th>
          <th>Name</th>
          <th style="min-width:180px">Score</th>
          <th class="center">Percentage</th>
          <th class="center">Time Taken</th>
          <th class="center">Submitted At</th>
        </tr>
      </thead>
      <tbody>{rows}</tbody>
    </table>"""

TABLE_EMPTY = """
    <div class="empty">
      <div class="empty-icon">📊</div>
      <div class="empty-title">No results yet</div>
      <div class="empty-sub">Run this script again once participants have submitted.</div>
    </div>"""


# ── Generate ──────────────────────────────────────────────────────────────────
def generate(excel_path: str, out_path: str, title: str):
    entries  = read_excel(excel_path)
    total_q  = entries[0]["total"] if entries else 0
    top3     = entries[:3]
    stats    = build_stats(entries, total_q)
    generated = datetime.now().strftime("%d-%b-%Y %H:%M:%S")

    table_html = TABLE_FULL.format(rows=build_rows(entries)) if entries else TABLE_EMPTY

    html = HTML.format(
        title     = esc(title),
        generated = generated,
        s_count   = stats["count"],
        s_top     = stats["top"],
        s_fastest = stats["fastest"],
        s_avg     = stats["avg"],
        s_full    = stats["full"],
        podium    = build_podium(top3),
        table     = table_html,
    )

    Path(out_path).write_text(html, encoding="utf-8")
    print(f"[{generated}]  {len(entries)} entries  →  {Path(out_path).resolve()}")


# ── Entry point ───────────────────────────────────────────────────────────────
if __name__ == "__main__":
    p = argparse.ArgumentParser(description="Generate Quiz Leaderboard HTML from Excel")
    p.add_argument("--excel", default="../QuizScores.xlsx",     help="Path to QuizScores.xlsx")
    p.add_argument("--out",   default="leaderboard.html",       help="Output HTML file")
    p.add_argument("--title", default="CATS Team Monthly Quiz", help="Quiz title shown in header")
    p.add_argument("--watch", action="store_true",
                   help="Keep regenerating every 10 seconds (use on quiz day with browser open)")
    p.add_argument("--interval", type=int, default=10,          help="Refresh interval in seconds (default 10)")
    args = p.parse_args()

    if args.watch:
        print(f"Watching: {args.excel}")
        print(f"Output:   {args.out}")
        print(f"Interval: every {args.interval}s  (Ctrl+C to stop)\n")
        while True:
            try:
                generate(args.excel, args.out, args.title)
            except FileNotFoundError:
                print(f"  Waiting for {args.excel}…")
            except Exception as e:
                print(f"  Error: {e}")
            time.sleep(args.interval)
    else:
        generate(args.excel, args.out, args.title)
