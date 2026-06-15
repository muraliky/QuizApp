using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuizApp
{
    public class ResultForm : Form
    {
        static readonly Color C_BG      = Color.FromArgb(13,  17,  28);
        static readonly Color C_CARD    = Color.FromArgb(22,  29,  48);
        static readonly Color C_ACCENT  = Color.FromArgb(99, 179, 237);
        static readonly Color C_ACCENT2 = Color.FromArgb(118, 75, 226);
        static readonly Color C_TEXT    = Color.FromArgb(226, 232, 240);
        static readonly Color C_MUTED   = Color.FromArgb(113, 128, 150);
        static readonly Color C_BORDER  = Color.FromArgb(45,  55,  72);
        static readonly Color C_GREEN   = Color.FromArgb(72, 199, 142);
        static readonly Color C_RED     = Color.FromArgb(252, 129, 129);
        static readonly Color C_YELLOW  = Color.FromArgb(246, 194,  62);

        private readonly List<Question> _questions;
        private readonly int[]          _userAnswers;
        private readonly string         _fullName;
        private readonly int            _score;
        private readonly string         _quizTitle;
        private readonly bool           _excelOk;
        private readonly string         _excelErr;

        public ResultForm(List<Question> questions, int[] userAnswers, string fullName,
            int score, string quizTitle, bool excelOk, string excelErr)
        {
            _questions   = questions;
            _userAnswers = userAnswers;
            _fullName    = fullName;
            _score       = score;
            _quizTitle   = quizTitle;
            _excelOk     = excelOk;
            _excelErr    = excelErr;
            Build();
        }

        private void Build()
        {
            int total = _questions.Count;
            double pct = total>0 ? (double)_score/total*100 : 0;

            Text            = "Results — " + _quizTitle;
            Size            = new Size(720, 700);
            MinimumSize     = Size; MaximumSize = Size;
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor       = C_BG;

            // drag
            bool drag=false; Point ds=Point.Empty;
            MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Left){drag=true;ds=e.Location;}};
            MouseMove+=(s,e)=>{if(drag)Location=new Point(Location.X+e.X-ds.X,Location.Y+e.Y-ds.Y);};
            MouseUp  +=(s,e)=>drag=false;

            // close button
            var btnX=new Label
            {
                Text="✕",Size=new Size(32,32),Location=new Point(Width-42,10),
                ForeColor=C_MUTED,BackColor=Color.Transparent,
                Font=new Font("Segoe UI",11f),TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand
            };
            btnX.MouseEnter+=(_,__)=>btnX.ForeColor=Color.White;
            btnX.MouseLeave+=(_,__)=>btnX.ForeColor=C_MUTED;
            btnX.Click+=(_,__)=>Application.Exit();
            Controls.Add(btnX);

            Controls.Add(new GradientStrip{Dock=DockStyle.Top,Height=4});

            // ── score hero ─────────────────────────────────────────
            var hero = new ScoreHero(pct, _score, total, _fullName, pct>=80?C_GREEN:pct>=60?C_YELLOW:C_RED)
            {
                Location=new Point(0,4), Size=new Size(720,190)
            };
            Controls.Add(hero);

            // excel status banner
            var lblExcel = new Label
            {
                Location=new Point(26,196), Size=new Size(668,24),
                Text=_excelOk?"✔  Score saved to shared drive." : $"⚠  Could not save: {_excelErr}",
                ForeColor=_excelOk?C_GREEN:C_YELLOW,
                BackColor=Color.Transparent, Font=new Font("Segoe UI",8.5f)
            };
            Controls.Add(lblExcel);

            // ── section header ─────────────────────────────────────
            var hdrPanel = new Panel{Location=new Point(26,226),Size=new Size(668,30),BackColor=Color.Transparent};
            hdrPanel.Paint+=(s,e)=>{
                var g=e.Graphics;
                using var f=new Font("Segoe UI",9f,FontStyle.Bold);
                using var b=new SolidBrush(C_MUTED);
                g.DrawString("ANSWER REVIEW",f,b,new PointF(0,6));
                int wrongCount=0; for(int i=0;i<_questions.Count;i++) if(_userAnswers[i]!=_questions[i].CorrectAnswerIndex)wrongCount++;
                string stat=wrongCount==0?"All correct!":$"{wrongCount} incorrect";
                using var b2=new SolidBrush(wrongCount==0?C_GREEN:C_RED);
                g.DrawString(stat,f,b2,new PointF(668-80,6));
            };
            Controls.Add(hdrPanel);

            // separator line
            var sep=new Panel{Location=new Point(26,256),Size=new Size(668,1),BackColor=C_BORDER};
            Controls.Add(sep);

            // ── scroll area ────────────────────────────────────────
            var scroll = new Panel
            {
                Location=new Point(26,262), Size=new Size(668,370),
                AutoScroll=true, BackColor=Color.Transparent
            };

            int y=8;
            for(int i=0;i<_questions.Count;i++)
            {
                bool correct = _userAnswers[i]==_questions[i].CorrectAnswerIndex;
                var row = new ReviewRow(
                    i+1,
                    _questions[i].QuestionText,
                    _questions[i].Options,
                    _userAnswers[i],
                    _questions[i].CorrectAnswerIndex,
                    correct
                ){
                    Location=new Point(0,y),
                    Width=scroll.Width-20   // leave room for scrollbar
                };
                // height set inside ReviewRow
                scroll.Controls.Add(row);
                y+=row.Height+6;
            }

            Controls.Add(scroll);

            // ── close button ───────────────────────────────────────
            var btnClose = new GlowButton("Close")
            {
                Location=new Point(290,640), Size=new Size(140,46)
            };
            btnClose.Click+=(_,__)=>Application.Exit();
            Controls.Add(btnClose);

            Paint+=(s,e)=>{ using var p=new Pen(C_BORDER,1f); e.Graphics.DrawRectangle(p,0,0,Width-1,Height-1); };
        }
    }

}
