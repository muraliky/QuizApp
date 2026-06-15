using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace QuizApp
{
    public class NameEntryForm : Form
    {
        // ── palette ────────────────────────────────────────────────
        static readonly Color C_BG      = Color.FromArgb(13,  17,  28);
        static readonly Color C_CARD    = Color.FromArgb(22,  29,  48);
        static readonly Color C_ACCENT  = Color.FromArgb(99, 179, 237);   // sky-blue
        static readonly Color C_ACCENT2 = Color.FromArgb(72, 149, 239);
        static readonly Color C_TEXT    = Color.FromArgb(226, 232, 240);
        static readonly Color C_MUTED   = Color.FromArgb(113, 128, 150);
        static readonly Color C_BORDER  = Color.FromArgb(45,  55,  72);
        static readonly Color C_INPUT   = Color.FromArgb(18,  24,  38);

        private List<Question> _questions;
        private string _quizTitle, _scoreFilePath, _questionsFilePath;

        // controls we need to reference
        private StyledTextBox txtFirst, txtLast;
        private GlowButton    btnStart;
        private Label         lblError;

        public NameEntryForm()
        {
            _quizTitle        = ConfigurationManager.AppSettings["QuizTitle"]        ?? "Monthly Quiz";
            _scoreFilePath    = ConfigurationManager.AppSettings["ScoreFilePath"]    ?? "QuizScores.xlsx";
            _questionsFilePath= ConfigurationManager.AppSettings["QuestionsFilePath"]?? "questions.json";

            SetupForm();
        }

        private void SetupForm()
        {
            Text             = _quizTitle;
            Size             = new Size(560, 620);
            MinimumSize      = Size;
            MaximumSize      = Size;
            StartPosition    = FormStartPosition.CenterScreen;
            FormBorderStyle  = FormBorderStyle.None;   // custom chrome
            BackColor        = C_BG;
            Font             = new Font("Segoe UI", 9.5f);

            // ── drag support (borderless) ────────────────────────
            bool dragging = false; Point dragStart = Point.Empty;
            MouseDown += (s,e)=>{ if(e.Button==MouseButtons.Left){dragging=true;dragStart=e.Location;} };
            MouseMove += (s,e)=>{ if(dragging) Location=new Point(Location.X+e.X-dragStart.X,Location.Y+e.Y-dragStart.Y); };
            MouseUp   += (s,e)=>{ dragging=false; };

            // ── close btn ───────────────────────────────────────
            var btnClose = new Label
            {
                Text = "✕", Size = new Size(32,32),
                Location = new Point(Width-42, 10),
                ForeColor = C_MUTED, BackColor = Color.Transparent,
                Font = new Font("Segoe UI",11f), TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnClose.MouseEnter += (_,__)=>btnClose.ForeColor=Color.White;
            btnClose.MouseLeave += (_,__)=>btnClose.ForeColor=C_MUTED;
            btnClose.Click      += (_,__)=>Application.Exit();
            Controls.Add(btnClose);

            // ── top glow strip ─────────────────────────────────
            var strip = new GradientStrip { Dock = DockStyle.Top, Height = 4 };
            Controls.Add(strip);

            // ── logo area ──────────────────────────────────────
            var pnlTop = new Panel
            {
                Location = new Point(0,4), Size = new Size(560, 220),
                BackColor = Color.Transparent
            };
            pnlTop.Paint += (s,e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                // large icon circle
                int cx=280, cy=95, r=52;
                using var bg = new SolidBrush(Color.FromArgb(30,99,179,237));
                g.FillEllipse(bg, cx-r, cy-r, r*2, r*2);
                using var pen = new Pen(C_ACCENT, 2f);
                g.DrawEllipse(pen, cx-r, cy-r, r*2, r*2);
                // pencil icon (simple lines)
                using var wb = new SolidBrush(C_ACCENT);
                g.FillRectangle(wb, cx-6, cy-22, 12, 32);
                Point[] tip = { new(cx-6,cy+10), new(cx+6,cy+10), new(cx,cy+22) };
                g.FillPolygon(wb, tip);
                // title
                using var tf = new Font("Segoe UI", 20f, FontStyle.Bold);
                var titleRect = new RectangleF(30, 160, 500, 40);
                using var tBrush = new SolidBrush(C_TEXT);
                g.DrawString(_quizTitle, tf, tBrush, titleRect,
                    new StringFormat { Alignment=StringAlignment.Center });
                // subtitle
                using var sf = new Font("Segoe UI", 10f);
                using var sb = new SolidBrush(C_MUTED);
                g.DrawString("Enter your details to get started", sf, sb,
                    new RectangleF(30,200,500,24),
                    new StringFormat{Alignment=StringAlignment.Center});
            };
            Controls.Add(pnlTop);

            // ── card panel ─────────────────────────────────────
            var card = new RoundedPanel(16)
            {
                Location = new Point(50, 235),
                Size     = new Size(460, 310),
                BackColor = C_CARD
            };
            Controls.Add(card);

            int lx=30, fw=400;
            var lblF = MakeLabel("First Name", lx, 28, fw); card.Controls.Add(lblF);
            txtFirst = new StyledTextBox { Location=new Point(lx,52), Size=new Size(fw,44),
                PlaceholderText="e.g. Murali" };
            card.Controls.Add(txtFirst);

            var lblL = MakeLabel("Last Name", lx, 116, fw); card.Controls.Add(lblL);
            txtLast = new StyledTextBox { Location=new Point(lx,140), Size=new Size(fw,44),
                PlaceholderText="e.g. Kumar" };
            card.Controls.Add(txtLast);

            lblError = new Label
            {
                Location=new Point(lx,200), Size=new Size(fw,22),
                ForeColor=Color.FromArgb(252,129,129), BackColor=Color.Transparent,
                Font=new Font("Segoe UI",8.5f), Text="", Visible=false
            };
            card.Controls.Add(lblError);

            btnStart = new GlowButton("Begin Quiz  →")
            {
                Location = new Point(lx, 235), Size = new Size(fw, 50)
            };
            btnStart.Click += BtnStart_Click;
            card.Controls.Add(btnStart);

            // keyboard nav
            txtFirst.KeyDown += (s,e)=>{ if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Tab){txtLast.Focus();e.SuppressKeyPress=true;} };
            txtLast .KeyDown += (s,e)=>{ if(e.KeyCode==Keys.Enter){BtnStart_Click(s,e);e.SuppressKeyPress=true;} };

            // ── version label ──────────────────────────────────
            var lblVer = new Label
            {
                Text="v2.0  •  CATS Team", Location=new Point(0,580),
                Size=new Size(560,20), TextAlign=ContentAlignment.MiddleCenter,
                ForeColor=C_BORDER, BackColor=Color.Transparent,
                Font=new Font("Segoe UI",7.5f)
            };
            Controls.Add(lblVer);
        }

        private Label MakeLabel(string text, int x, int y, int w)
        {
            return new Label
            {
                Text=text, Location=new Point(x,y), Size=new Size(w,18),
                ForeColor=C_MUTED, BackColor=Color.Transparent,
                Font=new Font("Segoe UI",8.5f,FontStyle.Bold)
            };
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            string fn = txtFirst.Text.Trim();
            string ln = txtLast .Text.Trim();

            if(string.IsNullOrEmpty(fn)||string.IsNullOrEmpty(ln))
            {
                ShowError("Please enter both your first and last name.");
                return;
            }
            if(fn.Length<2||ln.Length<2)
            {
                ShowError("Name must be at least 2 characters each.");
                return;
            }

            // load questions
            try
            {
                string path = Path.IsPathRooted(_questionsFilePath)
                    ? _questionsFilePath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _questionsFilePath);
                if(!File.Exists(path))
                {
                    ShowError($"questions.json not found at: {path}"); return;
                }
                _questions = JsonConvert.DeserializeObject<List<Question>>(File.ReadAllText(path));
                if(_questions==null||_questions.Count==0)
                {
                    ShowError("No questions found in the file."); return;
                }
            }
            catch(Exception ex){ ShowError("Load error: "+ex.Message); return; }

            HideError();
            string full = $"{fn} {ln}";
            var qf = new QuizForm(_questions, full, _quizTitle, _scoreFilePath);
            qf.Show(); Hide();
            qf.FormClosed += (_,__)=>Close();
        }

        private void ShowError(string msg){ lblError.Text="⚠  "+msg; lblError.Visible=true; }
        private void HideError(){ lblError.Visible=false; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // border
            using var p = new Pen(C_BORDER, 1f);
            e.Graphics.DrawRectangle(p, 0,0,Width-1,Height-1);
        }
    }
}
