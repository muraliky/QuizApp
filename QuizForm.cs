using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuizApp
{
    public class QuizForm : Form
    {
        // ── palette ────────────────────────────────────────────────
        static readonly Color C_BG      = Color.FromArgb(13,  17,  28);
        static readonly Color C_CARD    = Color.FromArgb(22,  29,  48);
        static readonly Color C_ACCENT  = Color.FromArgb(99, 179, 237);
        static readonly Color C_ACCENT2 = Color.FromArgb(118, 75, 226);
        static readonly Color C_TEXT    = Color.FromArgb(226, 232, 240);
        static readonly Color C_MUTED   = Color.FromArgb(113, 128, 150);
        static readonly Color C_BORDER  = Color.FromArgb(45,  55,  72);
        static readonly Color C_SEL     = Color.FromArgb(30,  66, 110);
        static readonly Color C_HOVER   = Color.FromArgb(26,  38,  62);

        // ── data ───────────────────────────────────────────────────
        private readonly List<Question> _questions;
        private readonly string _fullName, _quizTitle, _scoreFilePath;
        private readonly int[] _userAnswers;
        private int _current = 0;

        // ── controls ───────────────────────────────────────────────
        private ProgressStrip  _progressStrip;
        private Label          _lblQNum, _lblQText;
        private OptionCard[]   _optCards;
        private Label          _lblWarn;
        private GlowButton     _btnPrev, _btnNext;
        private NavDots        _navDots;
        private Label          _lblUser;

        public QuizForm(List<Question> questions, string fullName, string quizTitle, string scoreFilePath)
        {
            _questions   = questions;
            _fullName    = fullName;
            _quizTitle   = quizTitle;
            _scoreFilePath = scoreFilePath;
            _userAnswers = new int[questions.Count];
            for(int i=0;i<_userAnswers.Length;i++) _userAnswers[i]=-1;

            Build();
            Load(_current);
        }

        private void Build()
        {
            Text            = _quizTitle;
            Size            = new Size(720, 680);
            MinimumSize     = Size; MaximumSize = Size;
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor       = C_BG;

            // drag
            bool drag=false; Point ds=Point.Empty;
            MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){drag=true;ds=e.Location;}};
            MouseMove+=(s,e)=>{if(drag)Location=new Point(Location.X+e.X-ds.X,Location.Y+e.Y-ds.Y);};
            MouseUp  +=(s,e)=>drag=false;

            // ── close ──────────────────────────────────────────────
            var btnX = new Label
            {
                Text="✕", Size=new Size(32,32), Location=new Point(Width-42,10),
                ForeColor=C_MUTED, BackColor=Color.Transparent,
                Font=new Font("Segoe UI",11f), TextAlign=ContentAlignment.MiddleCenter,
                Cursor=Cursors.Hand
            };
            btnX.MouseEnter+=(_,__)=>btnX.ForeColor=Color.White;
            btnX.MouseLeave+=(_,__)=>btnX.ForeColor=C_MUTED;
            btnX.Click+=(_,__)=>{
                if(MessageBox.Show("Exit the quiz? Progress will be lost.",
                    "Exit Quiz",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes)
                    Application.Exit();
            };
            Controls.Add(btnX);

            // gradient strip
            Controls.Add(new GradientStrip{Dock=DockStyle.Top,Height=4});

            // ── header ─────────────────────────────────────────────
            var pnlH = new Panel{Location=new Point(0,4),Size=new Size(720,70),BackColor=Color.Transparent};
            pnlH.Paint+=(s,e)=>{
                var g=e.Graphics;
                using var tf=new Font("Segoe UI",14f,FontStyle.Bold);
                using var tb=new SolidBrush(C_TEXT);
                g.DrawString(_quizTitle,tf,tb,new PointF(26,18));
            };
            _lblUser = new Label
            {
                Location=new Point(26,44), AutoSize=true, BackColor=Color.Transparent,
                ForeColor=C_MUTED, Font=new Font("Segoe UI",8.5f),
                Text=$"👤  {_fullName}"
            };
            pnlH.Controls.Add(_lblUser);

            // progress strip (custom drawn)
            _progressStrip = new ProgressStrip(_questions.Count)
            {
                Location=new Point(0,74), Size=new Size(720,6)
            };
            pnlH.Controls.Add(_progressStrip);
            Controls.Add(pnlH);

            // ── question card ──────────────────────────────────────
            var qCard = new RoundedPanel(14)
            {
                Location=new Point(26,90), Size=new Size(668,130), BackColor=C_CARD
            };
            _lblQNum = new Label
            {
                Location=new Point(22,18), Size=new Size(400,18),
                ForeColor=C_ACCENT, BackColor=Color.Transparent,
                Font=new Font("Segoe UI",8.5f,FontStyle.Bold)
            };
            _lblQText = new Label
            {
                Location=new Point(22,42), Size=new Size(624,78),
                ForeColor=C_TEXT, BackColor=Color.Transparent,
                Font=new Font("Segoe UI",12f)
            };
            qCard.Controls.AddRange(new Control[]{_lblQNum,_lblQText});
            Controls.Add(qCard);

            // ── option cards ───────────────────────────────────────
            _optCards = new OptionCard[4];
            for(int i=0;i<4;i++)
            {
                int idx=i;
                var oc = new OptionCard(i)
                {
                    Location = new Point(26, 234+i*72),
                    Size     = new Size(668, 62)
                };
                oc.Click += (_,__)=>SelectOption(idx);
                _optCards[i]=oc;
                Controls.Add(oc);
            }

            // ── warning label ──────────────────────────────────────
            _lblWarn = new Label
            {
                Location=new Point(26,526), Size=new Size(460,22),
                ForeColor=Color.FromArgb(252,129,129), BackColor=Color.Transparent,
                Font=new Font("Segoe UI",8.5f), Text="⚠  Please select an answer to continue.",
                Visible=false
            };
            Controls.Add(_lblWarn);

            // ── nav dots ───────────────────────────────────────────
            _navDots = new NavDots(_questions.Count)
            {
                Location = new Point(0, 552), Size = new Size(720, 30)
            };
            _navDots.DotClicked += GoToQuestion;
            Controls.Add(_navDots);

            // ── prev / next buttons ────────────────────────────────
            _btnPrev = new GlowButton("← Prev", secondary:true)
            {
                Location=new Point(26,590), Size=new Size(140,46)
            };
            _btnPrev.Click += (_,__)=>NavPrev();

            _btnNext = new GlowButton("Next →")
            {
                Location=new Point(554,590), Size=new Size(140,46)
            };
            _btnNext.Click += (_,__)=>NavNext();

            Controls.Add(_btnPrev);
            Controls.Add(_btnNext);

            // outer border
            Paint+=(s,e)=>{ using var p=new Pen(C_BORDER,1f); e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1); };
        }

        private void Load(int idx)
        {
            var q = _questions[idx];
            bool isFirst = idx==0;
            bool isLast  = idx==_questions.Count-1;

            _lblQNum.Text  = $"QUESTION  {idx+1}  OF  {_questions.Count}";
            _lblQText.Text = q.QuestionText;

            // option labels
            string[] letters = {"A","B","C","D"};
            for(int i=0;i<4;i++)
            {
                _optCards[i].SetContent(letters[i], q.Options[i]);
                _optCards[i].State = OptionCard.CardState.Normal;
            }

            // restore saved answer
            if(_userAnswers[idx]>=0)
                _optCards[_userAnswers[idx]].State = OptionCard.CardState.Selected;

            // progress
            _progressStrip.SetProgress(idx+1);
            _navDots.SetCurrent(idx, _userAnswers);

            // buttons
            _btnPrev.Visible = !isFirst;
            _btnNext.SetLabel(isLast ? "Submit ✓" : "Next →");
            _btnNext.IsSubmit = isLast;

            _lblWarn.Visible = false;

            // bring option cards to front (render above card)
            foreach(var oc in _optCards) oc.BringToFront();
            _lblWarn.BringToFront();
            _navDots.BringToFront();
            _btnPrev.BringToFront();
            _btnNext.BringToFront();
        }

        private void SelectOption(int idx)
        {
            for(int i=0;i<4;i++)
                _optCards[i].State = (i==idx) ? OptionCard.CardState.Selected : OptionCard.CardState.Normal;
            _userAnswers[_current]=idx;
            _navDots.SetCurrent(_current, _userAnswers);
            _lblWarn.Visible=false;
        }

        private void NavNext()
        {
            if(_userAnswers[_current]<0){ _lblWarn.Visible=true; return; }
            if(_current==_questions.Count-1){ ShowResults(); return; }
            _current++;
            Load(_current);
        }

        private void NavPrev()
        {
            if(_current>0){ _current--; Load(_current); }
        }

        private void GoToQuestion(int idx)
        {
            // allow jumping only to answered or current±1
            if(idx<=_current || _userAnswers[idx-1]>=0)
            {
                if(_current!=idx && _userAnswers[_current]<0 && idx>_current)
                {
                    _lblWarn.Visible=true; return;
                }
                _current=idx;
                Load(_current);
            }
        }

        private void ShowResults()
        {
            int score=0;
            for(int i=0;i<_questions.Count;i++)
                if(_userAnswers[i]==_questions[i].CorrectAnswerIndex) score++;

            bool ok=false; string err="";
            try{ ExcelScoreWriter.WriteScore(_scoreFilePath,_fullName,score,_questions.Count,DateTime.Now); ok=true; }
            catch(Exception ex){ err=ex.Message; }

            new ResultForm(_questions,_userAnswers,_fullName,score,_quizTitle,ok,err).Show();
            Close();
        }
    }

}
