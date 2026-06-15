using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuizApp
{
    // ─────────────────────────────────────────────────────────────
    //  GradientStrip  (4 px accent bar at top of every window)
    // ─────────────────────────────────────────────────────────────
    class GradientStrip : Control
    {
        static readonly Color CA = Color.FromArgb(99,179,237);
        static readonly Color CB = Color.FromArgb(118,75,226);
        public GradientStrip()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint|
                     ControlStyles.OptimizedDoubleBuffer|
                     ControlStyles.UserPaint, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            using var br = new LinearGradientBrush(ClientRectangle, CA, CB, LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(br, ClientRectangle);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  RoundedPanel
    // ─────────────────────────────────────────────────────────────
    class RoundedPanel : Panel
    {
        readonly int _r;
        public RoundedPanel(int radius)
        {
            _r = radius;
            SetStyle(ControlStyles.AllPaintingInWmPaint|
                     ControlStyles.OptimizedDoubleBuffer|
                     ControlStyles.UserPaint, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundRect(ClientRectangle, _r);
            using var br   = new SolidBrush(BackColor);
            e.Graphics.FillPath(br, path);
            using var pen  = new Pen(Color.FromArgb(45, 55, 72), 1f);
            e.Graphics.DrawPath(pen, path);
            // force children to repaint (transparency fix)
            foreach (Control c in Controls) c.Invalidate();
        }
        public static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X,            r.Y,            rad*2, rad*2, 180,  90);
            p.AddArc(r.Right-rad*2,  r.Y,            rad*2, rad*2, 270,  90);
            p.AddArc(r.Right-rad*2,  r.Bottom-rad*2, rad*2, rad*2, 0,    90);
            p.AddArc(r.X,            r.Bottom-rad*2, rad*2, rad*2, 90,   90);
            p.CloseFigure();
            return p;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  StyledTextBox  (dark, borderless, focus underline)
    // ─────────────────────────────────────────────────────────────
    class StyledTextBox : TextBox
    {
        static readonly Color BG     = Color.FromArgb(18, 24, 38);
        static readonly Color BORDER = Color.FromArgb(45, 55, 72);
        static readonly Color FOCUS  = Color.FromArgb(99, 179, 237);
        bool _focused;

        public StyledTextBox()
        {
            BackColor   = BG;
            ForeColor   = Color.FromArgb(226, 232, 240);
            BorderStyle = BorderStyle.None;
            Font        = new Font("Segoe UI", 11f);
        }
        protected override void OnGotFocus(EventArgs e)  { _focused = true;  Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { _focused = false; Invalidate(); base.OnLostFocus(e); }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0xF) // WM_PAINT
            {
                using var g   = Graphics.FromHwnd(Handle);
                using var pen = new Pen(_focused ? FOCUS : BORDER, _focused ? 2f : 1f);
                g.DrawLine(pen, 0, Height - (_focused ? 2 : 1), Width, Height - (_focused ? 2 : 1));
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  GlowButton  (gradient accent button, supports secondary style)
    // ─────────────────────────────────────────────────────────────
    class GlowButton : Control
    {
        static readonly Color PRI_A   = Color.FromArgb(99, 179, 237);
        static readonly Color PRI_B   = Color.FromArgb(72, 149, 239);
        static readonly Color SEC_A   = Color.FromArgb(40, 50, 72);
        static readonly Color SEC_B   = Color.FromArgb(30, 40, 60);
        static readonly Color SUB_A   = Color.FromArgb(55, 168, 100);
        static readonly Color SUB_B   = Color.FromArgb(38, 140, 78);
        static readonly Color HOV_PRI = Color.FromArgb(118, 75, 226);
        static readonly Color HOV_B2  = Color.FromArgb(90, 55, 180);

        string _label;
        bool   _hov, _pressed;
        bool   _secondary;
        public bool IsSubmit { get; set; }

        public GlowButton(string label, bool secondary = false)
        {
            _label     = label;
            _secondary = secondary;
            Cursor     = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint|
                     ControlStyles.OptimizedDoubleBuffer|
                     ControlStyles.UserPaint, true);
        }

        public void SetLabel(string label) { _label = label; Invalidate(); }

        protected override void OnMouseEnter(EventArgs e) { _hov     = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hov     = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true;  Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)   { _pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = ClientRectangle; r.Inflate(-1, -1);
            using var path = RoundedPanel.RoundRect(r, 10);

            Color ca, cb;
            if (_secondary)      { ca = SEC_A; cb = SEC_B; }
            else if (IsSubmit)   { ca = SUB_A; cb = SUB_B; }
            else                 { ca = PRI_A; cb = PRI_B; }

            if (_hov && !_secondary)
            {
                ca = HOV_PRI; cb = HOV_B2;
                if (IsSubmit) { ca = Color.FromArgb(38,140,78); cb=Color.FromArgb(28,110,58); }
            }
            if (_pressed) { ca = Color.FromArgb((int)(ca.R*.8f),(int)(ca.G*.8f),(int)(ca.B*.8f));
                            cb = Color.FromArgb((int)(cb.R*.8f),(int)(cb.G*.8f),(int)(cb.B*.8f)); }

            using var br = new LinearGradientBrush(r, ca, cb, LinearGradientMode.Horizontal);
            g.FillPath(br, path);

            if (_hov && !_secondary)
            {
                using var gp = new Pen(Color.FromArgb(60, ca), 5f);
                g.DrawPath(gp, path);
            }

            // border for secondary
            if (_secondary)
            {
                using var bp = new Pen(Color.FromArgb(60, 90, 120), 1f);
                g.DrawPath(bp, path);
            }

            using var tf = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            Color fc = _secondary ? Color.FromArgb(150, 180, 210) : Color.White;
            TextRenderer.DrawText(g, _label, tf, ClientRectangle, fc,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ProgressStrip  (thin gradient fill bar)
    // ─────────────────────────────────────────────────────────────
    class ProgressStrip : Control
    {
        static readonly Color C_BG = Color.FromArgb(30, 40, 62);
        static readonly Color C_A  = Color.FromArgb(99, 179, 237);
        static readonly Color C_B  = Color.FromArgb(118, 75, 226);
        int _total, _val;
        public ProgressStrip(int total)
        {
            _total = total;
            SetStyle(ControlStyles.AllPaintingInWmPaint|
                     ControlStyles.OptimizedDoubleBuffer|
                     ControlStyles.UserPaint, true);
        }
        public void SetProgress(int v) { _val = v; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(C_BG);
            if (_total > 0 && _val > 0)
            {
                int w = (int)((double)_val / _total * Width);
                using var br = new LinearGradientBrush(new Rectangle(0,0,Math.Max(Width,1),Height),C_A,C_B,LinearGradientMode.Horizontal);
                g.FillRectangle(br, new Rectangle(0, 0, w, Height));
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  OptionCard  (MCQ answer card, replaces RadioButton)
    // ─────────────────────────────────────────────────────────────
    class OptionCard : Control
    {
        public enum CardState { Normal, Hover, Selected }
        static readonly Color C_BG   = Color.FromArgb(22, 29, 48);
        static readonly Color C_HOV  = Color.FromArgb(26, 38, 62);
        static readonly Color C_SEL  = Color.FromArgb(17, 55, 100);
        static readonly Color C_ABG  = Color.FromArgb(99, 179, 237);
        static readonly Color C_ASEL = Color.FromArgb(30, 99, 179);
        static readonly Color C_TXT  = Color.FromArgb(226, 232, 240);
        static readonly Color C_BOR  = Color.FromArgb(45, 55, 72);
        static readonly Color C_BSEL = Color.FromArgb(99, 179, 237);

        CardState _state;
        string _letter, _text;

        public CardState State { get => _state; set { _state = value; Invalidate(); } }

        public OptionCard(int index)
        {
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint|
                     ControlStyles.OptimizedDoubleBuffer|
                     ControlStyles.UserPaint, true);
        }
        public void SetContent(string letter, string text) { _letter = letter; _text = text; Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { if (_state != CardState.Selected) _state = CardState.Hover; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { if (_state != CardState.Selected) _state = CardState.Normal; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseClick(MouseEventArgs e) { OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width-1, Height-1);
            using var path = RoundedPanel.RoundRect(r, 10);
            bool sel = _state == CardState.Selected;
            Color bg  = sel ? C_SEL : (_state == CardState.Hover ? C_HOV : C_BG);
            Color bor = sel ? C_BSEL : C_BOR;
            using var br  = new SolidBrush(bg);  g.FillPath(br, path);
            using var pen = new Pen(bor, sel ? 2f : 1f); g.DrawPath(pen, path);

            // letter badge
            int cx = 28, cy = Height/2, cr = 16;
            using var cbr = new SolidBrush(sel ? C_ABG : C_ASEL);
            g.FillEllipse(cbr, cx-cr, cy-cr, cr*2, cr*2);
            using var lf = new Font("Segoe UI", 10f, FontStyle.Bold);
            TextRenderer.DrawText(g, _letter ?? "-", lf, new Rectangle(cx-cr, cy-cr, cr*2, cr*2),
                sel ? Color.FromArgb(13,17,28) : Color.FromArgb(160,200,230),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            // text
            using var tf = new Font("Segoe UI", 10.5f);
            TextRenderer.DrawText(g, _text ?? "", tf, new Rectangle(56, 0, Width-70, Height),
                C_TXT, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);

            // chevron when selected
            if (sel)
            {
                using var af = new Font("Segoe UI", 14f);
                TextRenderer.DrawText(g, "›", af, new Rectangle(Width-30, 0, 24, Height),
                    C_BSEL, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  NavDots  (clickable question navigation dots)
    // ─────────────────────────────────────────────────────────────
    class NavDots : Control
    {
        public event Action<int> DotClicked;
        static readonly Color C_DONE  = Color.FromArgb(99, 179, 237);
        static readonly Color C_CURR  = Color.FromArgb(226, 232, 240);
        static readonly Color C_EMPTY = Color.FromArgb(45, 55, 72);

        int   _total, _current, _hovIdx = -1;
        int[] _answers;
        const int DOT = 10, GAP = 8;

        public NavDots(int total)
        {
            _total   = total;
            _answers = new int[total]; for(int i=0;i<total;i++) _answers[i]=-1;
            Cursor   = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint|
                     ControlStyles.OptimizedDoubleBuffer|
                     ControlStyles.UserPaint, true);
        }
        public void SetCurrent(int cur, int[] answers) { _current = cur; _answers = answers; Invalidate(); }

        protected override void OnMouseMove(MouseEventArgs e)  { _hovIdx = HitTest(e.X, e.Y); Invalidate(); base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e)      { _hovIdx = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseClick(MouseEventArgs e) { int i = HitTest(e.X, e.Y); if(i>=0) DotClicked?.Invoke(i); }

        int HitTest(int mx, int my)
        {
            int totalW = _total*(DOT+GAP)-GAP, sx = (Width-totalW)/2, cy = Height/2;
            for(int i=0;i<_total;i++)
            {
                int cx = sx + i*(DOT+GAP) + DOT/2, dx = mx-cx, dy = my-cy;
                if(dx*dx+dy*dy <= (DOT+4)*(DOT+4)/4) return i;
            }
            return -1;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(13,17,28));
            int totalW = _total*(DOT+GAP)-GAP, sx=(Width-totalW)/2, cy=Height/2;
            for(int i=0;i<_total;i++)
            {
                int cx = sx+i*(DOT+GAP)+DOT/2;
                bool cur = i==_current, done = _answers[i]>=0, hov = i==_hovIdx;
                int  r2  = cur ? DOT : (hov ? DOT-1 : DOT-2);
                Color c  = cur ? C_CURR : (done ? C_DONE : C_EMPTY);
                if(hov&&!cur) c = Color.FromArgb(150,180,220);
                using var br = new SolidBrush(c);
                g.FillEllipse(br, cx-r2/2, cy-r2/2, r2, r2);
                if(cur) { using var p=new Pen(Color.FromArgb(99,179,237),1.5f); g.DrawEllipse(p,cx-r2/2-2,cy-r2/2-2,r2+4,r2+4); }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ScoreHero  (circular score arc + stats)
    // ─────────────────────────────────────────────────────────────
    class ScoreHero : Control
    {
        static readonly Color C_TEXT = Color.FromArgb(226, 232, 240);
        static readonly Color C_MUT  = Color.FromArgb(113, 128, 150);
        readonly double _pct; readonly int _score,_total; readonly string _name; readonly Color _accent;
        public ScoreHero(double pct, int score, int total, string name, Color accent)
        {
            _pct=pct; _score=score; _total=total; _name=name; _accent=accent;
            SetStyle(ControlStyles.AllPaintingInWmPaint|
                     ControlStyles.OptimizedDoubleBuffer|
                     ControlStyles.UserPaint, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(13,17,28));
            using var gbr = new LinearGradientBrush(new Rectangle(0,0,Width,Height),
                Color.FromArgb(40,_accent.R,_accent.G,_accent.B), Color.FromArgb(13,17,28), LinearGradientMode.Vertical);
            g.FillRectangle(gbr, ClientRectangle);

            int cr=58, cx=104, cy=Height/2;
            using var arcBg = new Pen(Color.FromArgb(40,_accent.R,_accent.G,_accent.B), 14f);
            g.DrawEllipse(arcBg, cx-cr, cy-cr, cr*2, cr*2);
            float sweep = (float)(_pct/100*360);
            if(sweep>0)
            {
                using var arcFill = new Pen(_accent, 14f){ StartCap=LineCap.Round, EndCap=LineCap.Round };
                g.DrawArc(arcFill, cx-cr, cy-cr, cr*2, cr*2, -90, sweep);
            }
            using var sf = new Font("Segoe UI", 22f, FontStyle.Bold);
            TextRenderer.DrawText(g,$"{_score}/{_total}",sf,new Rectangle(cx-cr,cy-cr,cr*2,cr*2),
                Color.White, TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            using var pf = new Font("Segoe UI",8.5f);
            TextRenderer.DrawText(g,$"{_pct:F1}%",pf,new Rectangle(cx-cr,cy+34,cr*2,18),
                _accent, TextFormatFlags.HorizontalCenter);

            int tx = 210;
            using var titleF = new Font("Segoe UI",18f,FontStyle.Bold);
            g.DrawString("Quiz Complete!", titleF, new SolidBrush(Color.White), new PointF(tx,26));
            using var nf = new Font("Segoe UI",10f);
            g.DrawString($"👤  {_name}", nf, new SolidBrush(C_MUT), new PointF(tx,68));
            string grade = _pct>=80?"Excellent  🏆":_pct>=60?"Good  👍":_pct>=40?"Average":"Needs Improvement";
            using var grf = new Font("Segoe UI",13f,FontStyle.Bold);
            g.DrawString(grade, grf, new SolidBrush(_accent), new PointF(tx,98));

            DrawStat(g, tx,    140, "Correct",   $"{_score}",        Color.FromArgb(72,199,142));
            DrawStat(g, tx+130,140, "Incorrect",  $"{_total-_score}", Color.FromArgb(252,129,129));
            DrawStat(g, tx+270,140, "Total",      $"{_total}",        Color.FromArgb(99,179,237));
        }
        void DrawStat(Graphics g, int x, int y, string lbl, string val, Color c)
        {
            using var vf = new Font("Segoe UI",16f,FontStyle.Bold);
            using var lf = new Font("Segoe UI",8f);
            g.DrawString(val, vf, new SolidBrush(c), new PointF(x,y));
            g.DrawString(lbl, lf, new SolidBrush(Color.FromArgb(113,128,150)), new PointF(x,y+26));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ReviewRow  (one question's result in the results list)
    // ─────────────────────────────────────────────────────────────
    class ReviewRow : Control
    {
        static readonly Color C_TEXT  = Color.FromArgb(226, 232, 240);
        static readonly Color C_MUTED = Color.FromArgb(113, 128, 150);
        static readonly Color C_GREEN = Color.FromArgb(72,  199, 142);
        static readonly Color C_RED   = Color.FromArgb(252, 129, 129);

        readonly int      _qNum, _userIdx, _correctIdx;
        readonly bool     _correct;
        readonly string   _qText;
        readonly string[] _opts;

        public ReviewRow(int qnum, string qtext, string[] opts, int userIdx, int correctIdx, bool correct)
        {
            _qNum=qnum; _qText=qtext; _opts=opts; _userIdx=userIdx; _correctIdx=correctIdx; _correct=correct;
            Height = correct ? 54 : 98;
            SetStyle(ControlStyles.AllPaintingInWmPaint|
                     ControlStyles.OptimizedDoubleBuffer|
                     ControlStyles.UserPaint, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, Width-1, Height-1);
            using var path = RoundedPanel.RoundRect(r, 8);
            using var br   = new SolidBrush(_correct ? Color.FromArgb(16,72,199,142) : Color.FromArgb(20,252,129,129));
            g.FillPath(br, path);
            using var pen  = new Pen(_correct ? Color.FromArgb(55,72,199,142) : Color.FromArgb(55,252,129,129), 1f);
            g.DrawPath(pen, path);

            string icon = _correct ? "✔" : "✖";
            using var icF = new Font("Segoe UI", 13f, FontStyle.Bold);
            TextRenderer.DrawText(g, icon, icF, new Rectangle(10, 0, 28, Height),
                _correct ? C_GREEN : C_RED, TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);

            using var qF = new Font("Segoe UI", 8f, FontStyle.Bold);
            TextRenderer.DrawText(g, $"Q{_qNum}", qF, new Rectangle(44, 6, 40, 16),
                _correct ? C_GREEN : C_RED, TextFormatFlags.Left);

            using var tF = new Font("Segoe UI", 9.5f);
            TextRenderer.DrawText(g, _qText, tF, new Rectangle(44, 22, Width-58, _correct ? 28 : 22),
                C_TEXT, TextFormatFlags.Left|TextFormatFlags.WordBreak);

            if (!_correct)
            {
                string[] L = {"A","B","C","D"};
                string ua = _userIdx>=0 ? $"{L[_userIdx]})  {_opts[_userIdx]}" : "No answer";
                string ca = $"{L[_correctIdx]})  {_opts[_correctIdx]}";
                using var lF = new Font("Segoe UI", 8.5f);
                TextRenderer.DrawText(g, "Your answer:",   lF, new Rectangle(44, 50, 110, 18), C_MUTED, TextFormatFlags.Left);
                TextRenderer.DrawText(g, ua,               lF, new Rectangle(156,50, Width-168,18), C_RED,   TextFormatFlags.Left);
                TextRenderer.DrawText(g, "Correct answer:",lF, new Rectangle(44, 72, 110, 18), C_MUTED, TextFormatFlags.Left);
                TextRenderer.DrawText(g, ca,               lF, new Rectangle(156,72, Width-168,18), C_GREEN, TextFormatFlags.Left|TextFormatFlags.WordBreak);
            }
        }
    }
}
