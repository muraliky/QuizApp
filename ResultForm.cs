using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuizApp
{
    public class ResultForm : Form
    {
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

            BuildUI();
        }

        private void BuildUI()
        {
            int    total = _questions.Count;
            double pct   = total > 0 ? (double)_score / total * 100 : 0;

            Text            = "Results — " + _quizTitle;
            Size            = new Size(700, 680);
            MinimumSize     = new Size(700, 680);
            MaximumSize     = new Size(700, 680);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(245, 247, 250);
            Font            = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;

            // ── SCORE HEADER ───────────────────────────────────────────
            Color headerColor = pct >= 80
                ? Color.FromArgb(30, 120, 70)
                : pct >= 60
                    ? Color.FromArgb(160, 100, 20)
                    : Color.FromArgb(180, 40, 40);

            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 160,
                BackColor = headerColor
            };

            var lblDone = new Label
            {
                Text      = "Quiz Complete!",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(380, 38),
                Location  = new Point(24, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblName = new Label
            {
                Text      = "👤  " + _fullName,
                ForeColor = Color.FromArgb(210, 240, 210),
                Font      = new Font("Segoe UI", 9.5f),
                AutoSize  = false,
                Size      = new Size(380, 24),
                Location  = new Point(26, 62),
                TextAlign = ContentAlignment.MiddleLeft
            };

            string grade = pct >= 80 ? "Excellent 🏆"
                         : pct >= 60 ? "Good 👍"
                         : pct >= 40 ? "Average"
                         : "Needs Improvement";

            var lblGrade = new Label
            {
                Text      = grade,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(380, 26),
                Location  = new Point(26, 90),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblExcel = new Label
            {
                Text      = _excelOk ? "✔  Score saved to shared drive." : "⚠  Could not save: " + _excelErr,
                ForeColor = _excelOk ? Color.FromArgb(180, 255, 180) : Color.Yellow,
                Font      = new Font("Segoe UI", 8.5f),
                AutoSize  = false,
                Size      = new Size(500, 22),
                Location  = new Point(26, 128),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Score box on the right
            var pnlScore = new Panel
            {
                Location  = new Point(510, 20),
                Size      = new Size(160, 118),
                BackColor = Color.FromArgb(0, 0, 0, 50)
            };

            var lblScoreNum = new Label
            {
                Text      = $"{_score}/{total}",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 30f, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(160, 60),
                Location  = new Point(0, 10),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblPct = new Label
            {
                Text      = $"{pct:F1}%",
                ForeColor = Color.FromArgb(220, 255, 220),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(160, 32),
                Location  = new Point(0, 68),
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnlScore.Controls.AddRange(new Control[] { lblScoreNum, lblPct });
            pnlHeader.Controls.AddRange(new Control[]
                { lblDone, lblName, lblGrade, lblExcel, pnlScore });

            // ── REVIEW HEADER ──────────────────────────────────────────
            var pnlRevHdr = new Panel
            {
                Location  = new Point(0, 160),
                Size      = new Size(700, 36),
                BackColor = Color.FromArgb(235, 240, 248)
            };
            pnlRevHdr.Paint += (s, e) =>
            {
                using var p = new System.Drawing.Pen(Color.FromArgb(200, 210, 225), 1);
                e.Graphics.DrawLine(p, 0, pnlRevHdr.Height - 1, pnlRevHdr.Width, pnlRevHdr.Height - 1);
            };

            int wrongCount = 0;
            for (int i = 0; i < _questions.Count; i++)
                if (_userAnswers[i] != _questions[i].CorrectAnswerIndex) wrongCount++;

            var lblRevTitle = new Label
            {
                Text      = wrongCount == 0
                            ? "  ✔  All answers correct!"
                            : $"  📋  Answer Review  —  {wrongCount} incorrect answer{(wrongCount > 1 ? "s" : "")}",
                ForeColor = Color.FromArgb(41, 98, 172),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlRevHdr.Controls.Add(lblRevTitle);

            // ── SCROLLABLE REVIEW LIST ─────────────────────────────────
            var scroll = new Panel
            {
                Location   = new Point(0, 196),
                Size       = new Size(700, 430),
                AutoScroll = true,
                BackColor  = Color.FromArgb(245, 247, 250)
            };

            int y = 8;
            for (int i = 0; i < _questions.Count; i++)
            {
                bool correct = _userAnswers[i] == _questions[i].CorrectAnswerIndex;
                int  rowH    = correct ? 56 : 100;

                var row = new Panel
                {
                    Location  = new Point(14, y),
                    Size      = new Size(656, rowH),
                    BackColor = correct
                                ? Color.FromArgb(235, 250, 240)
                                : Color.FromArgb(255, 240, 240)
                };
                row.Paint += (s, e) =>
                {
                    using var pen = new System.Drawing.Pen(
                        correct ? Color.FromArgb(180, 220, 190) : Color.FromArgb(220, 180, 180), 1);
                    e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
                };

                // Status icon
                var lblIcon = new Label
                {
                    Text      = correct ? "✔" : "✖",
                    ForeColor = correct ? Color.FromArgb(30, 140, 70) : Color.FromArgb(190, 40, 40),
                    Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                    Location  = new Point(10, 0),
                    Size      = new Size(32, rowH),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // Q number
                var lblQN = new Label
                {
                    Text      = $"Q{i + 1}",
                    ForeColor = correct ? Color.FromArgb(30, 140, 70) : Color.FromArgb(190, 40, 40),
                    Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Location  = new Point(46, 8),
                    Size      = new Size(36, 16)
                };

                // Question text
                var lblQ = new Label
                {
                    Text      = _questions[i].QuestionText,
                    ForeColor = Color.FromArgb(30, 30, 30),
                    Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Location  = new Point(46, 24),
                    Size      = new Size(596, correct ? 26 : 22)
                };

                row.Controls.AddRange(new Control[] { lblIcon, lblQN, lblQ });

                if (!correct)
                {
                    string[] L = { "A", "B", "C", "D" };
                    string ua  = _userAnswers[i] >= 0
                        ? $"{L[_userAnswers[i]]})  {_questions[i].Options[_userAnswers[i]]}"
                        : "No answer selected";
                    string ca  = $"{L[_questions[i].CorrectAnswerIndex]})  {_questions[i].Options[_questions[i].CorrectAnswerIndex]}";

                    var lblYA = new Label
                    {
                        Text      = "Your answer:      " + ua,
                        ForeColor = Color.FromArgb(180, 50, 50),
                        Font      = new Font("Segoe UI", 9f),
                        Location  = new Point(46, 50),
                        Size      = new Size(600, 20)
                    };

                    var lblCA = new Label
                    {
                        Text      = "Correct answer:  " + ca,
                        ForeColor = Color.FromArgb(25, 120, 60),
                        Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                        Location  = new Point(46, 72),
                        Size      = new Size(600, 20)
                    };

                    row.Controls.AddRange(new Control[] { lblYA, lblCA });
                }

                scroll.Controls.Add(row);
                y += rowH + 6;
            }

            // ── CLOSE BUTTON ───────────────────────────────────────────
            var btnClose = new Button
            {
                Text      = "Close",
                Size      = new Size(130, 40),
                Location  = new Point(284, 628),
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(41, 98, 172),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 78, 140);
            btnClose.Click += (s, e) => Application.Exit();

            Controls.Add(pnlHeader);
            Controls.Add(pnlRevHdr);
            Controls.Add(scroll);
            Controls.Add(btnClose);
        }
    }
}
