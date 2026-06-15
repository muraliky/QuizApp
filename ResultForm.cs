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
        private readonly int            _timeTaken;

        public ResultForm(List<Question> questions, int[] userAnswers, string fullName,
                          int score, string quizTitle, bool excelOk, string excelErr, int timeTaken)
        {
            _questions   = questions;
            _userAnswers = userAnswers;
            _fullName    = fullName;
            _score       = score;
            _quizTitle   = quizTitle;
            _excelOk     = excelOk;
            _excelErr    = excelErr;
            _timeTaken   = timeTaken;
            BuildUI();
        }

        private void BuildUI()
        {
            int    total = _questions.Count;
            double pct   = total > 0 ? (double)_score / total * 100 : 0;

            // ── Form ───────────────────────────────────────────────────
            Text            = "Results — " + _quizTitle;
            Size            = new Size(700, 700);
            MinimumSize     = new Size(700, 700);
            MaximumSize     = new Size(700, 700);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(245, 247, 250);
            Font            = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;

            Color headerColor = pct >= 80 ? Color.FromArgb(30, 120, 70)
                              : pct >= 60 ? Color.FromArgb(160, 100, 20)
                                          : Color.FromArgb(180, 40, 40);

            string grade = pct >= 80 ? "Excellent  🏆"
                         : pct >= 60 ? "Good  👍"
                         : pct >= 40 ? "Average"
                                     : "Needs Improvement";

            // ══════════════════════════════════════════════════════════
            // 1. HEADER  (Dock = Top, 160 px)
            // ══════════════════════════════════════════════════════════
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 160,
                BackColor = headerColor
            };

            // Left column — title, name, grade, excel status
            var lblDone = new Label
            {
                Text      = "Quiz Complete!",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                Location  = new Point(20, 18),
                Size      = new Size(390, 34),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblName = new Label
            {
                Text      = "👤  " + _fullName,
                ForeColor = Color.FromArgb(210, 240, 210),
                Font      = new Font("Segoe UI", 9.5f),
                Location  = new Point(22, 56),
                Size      = new Size(390, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            string timeDisplay = ExcelScoreWriter.FormatTime(_timeTaken);
            var lblGrade = new Label
            {
                Text      = "⏱  " + timeDisplay + " to complete",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location  = new Point(22, 82),
                Size      = new Size(390, 24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblExcel = new Label
            {
                Text      = _excelOk
                            ? "✔  Score saved to shared drive."
                            : "⚠  Could not save score: " + _excelErr,
                ForeColor = _excelOk ? Color.FromArgb(180, 255, 180) : Color.Yellow,
                Font      = new Font("Segoe UI", 8.5f),
                Location  = new Point(22, 122),
                Size      = new Size(490, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Right column — score box
            var pnlScoreBox = new Panel
            {
                Location  = new Point(516, 16),
                Size      = new Size(152, 126),
                BackColor = Color.FromArgb(0, 0, 0, 40)
            };
            var lblScoreNum = new Label
            {
                Text      = $"{_score} / {total}",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 26f, FontStyle.Bold),
                Location  = new Point(0, 14),
                Size      = new Size(152, 54),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblPct = new Label
            {
                Text      = $"{pct:F1}%",
                ForeColor = Color.FromArgb(210, 255, 210),
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                Location  = new Point(0, 70),
                Size      = new Size(152, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlScoreBox.Controls.AddRange(new Control[] { lblScoreNum, lblPct });

            pnlHeader.Controls.AddRange(new Control[]
                { lblDone, lblName, lblGrade, lblExcel, pnlScoreBox });

            // ══════════════════════════════════════════════════════════
            // 2. FOOTER  (Dock = Bottom, 64 px)  — added BEFORE scroll
            //    so DockStyle layering works correctly
            // ══════════════════════════════════════════════════════════
            var pnlFooter = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 64,
                BackColor = Color.White
            };
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new System.Drawing.Pen(Color.FromArgb(200, 212, 228), 1);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            var btnClose = new Button
            {
                Text      = "Close",
                Size      = new Size(150, 40),
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(41, 98, 172),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize            = 0;
            btnClose.FlatAppearance.MouseOverBackColor    = Color.FromArgb(30, 78, 140);
            btnClose.Click += (s, e) => Application.Exit();

            // Centre the button inside the footer
            pnlFooter.Controls.Add(btnClose);
            pnlFooter.Layout += (s, e) =>
            {
                btnClose.Location = new Point(
                    (pnlFooter.ClientSize.Width  - btnClose.Width)  / 2,
                    (pnlFooter.ClientSize.Height - btnClose.Height) / 2);
            };

            // ══════════════════════════════════════════════════════════
            // 3. SECTION HEADER  (Dock = Top, 36 px)
            // ══════════════════════════════════════════════════════════
            int wrongCount = 0;
            for (int i = 0; i < _questions.Count; i++)
                if (_userAnswers[i] != _questions[i].CorrectAnswerIndex) wrongCount++;

            var pnlRevHdr = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 36,
                BackColor = Color.FromArgb(232, 240, 252)
            };
            pnlRevHdr.Paint += (s, e) =>
            {
                using var p = new System.Drawing.Pen(Color.FromArgb(200, 212, 228), 1);
                e.Graphics.DrawLine(p, 0, pnlRevHdr.Height - 1, pnlRevHdr.Width, pnlRevHdr.Height - 1);
            };

            string revTitle = wrongCount == 0
                ? "  ✔  Perfect score — all answers correct!"
                : $"  📋  Answer Review  —  {wrongCount} incorrect answer{(wrongCount > 1 ? "s" : "")}";

            var lblRevTitle = new Label
            {
                Text      = revTitle,
                ForeColor = Color.FromArgb(41, 98, 172),
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlRevHdr.Controls.Add(lblRevTitle);

            // ══════════════════════════════════════════════════════════
            // 4. SCROLL AREA  (Dock = Fill — takes remaining space)
            // ══════════════════════════════════════════════════════════
            var scroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Color.FromArgb(245, 247, 250),
                Padding    = new Padding(14, 8, 14, 8)
            };

            int y = 8;
            for (int i = 0; i < _questions.Count; i++)
            {
                bool   correct = _userAnswers[i] == _questions[i].CorrectAnswerIndex;
                string[] L     = { "A", "B", "C", "D" };

                // Build row content
                string questionText = _questions[i].QuestionText;
                string yourAns      = !correct && _userAnswers[i] >= 0
                    ? $"{L[_userAnswers[i]]})  {_questions[i].Options[_userAnswers[i]]}"
                    : "No answer selected";
                string correctAns   = $"{L[_questions[i].CorrectAnswerIndex]})  {_questions[i].Options[_questions[i].CorrectAnswerIndex]}";

                // Row height: correct = 54, wrong = 96
                int rowH = correct ? 54 : 96;

                var row = new Panel
                {
                    Location  = new Point(0, y),
                    Size      = new Size(646, rowH),
                    BackColor = correct
                                ? Color.FromArgb(232, 248, 238)
                                : Color.FromArgb(255, 238, 238)
                };

                // Capture loop variable for Paint closure
                bool rowCorrect = correct;
                row.Paint += (s, e) =>
                {
                    using var pen = new System.Drawing.Pen(
                        rowCorrect ? Color.FromArgb(160, 215, 175) : Color.FromArgb(215, 175, 175), 1);
                    e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
                };

                // ── Icon ────────────────────────────────────────────────
                var lblIcon = new Label
                {
                    Text      = correct ? "✔" : "✖",
                    ForeColor = correct ? Color.FromArgb(25, 130, 65) : Color.FromArgb(185, 38, 38),
                    Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                    Location  = new Point(8, 0),
                    Size      = new Size(30, rowH),
                    TextAlign = ContentAlignment.MiddleCenter
                };

                // ── Q number ────────────────────────────────────────────
                var lblQN = new Label
                {
                    Text      = $"Q{i + 1}",
                    ForeColor = correct ? Color.FromArgb(25, 130, 65) : Color.FromArgb(185, 38, 38),
                    Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Location  = new Point(44, 8),
                    Size      = new Size(38, 16),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                // ── Question text ────────────────────────────────────────
                var lblQ = new Label
                {
                    Text      = questionText,
                    ForeColor = Color.FromArgb(25, 25, 25),
                    Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Location  = new Point(44, 26),
                    Size      = new Size(592, correct ? 22 : 20),
                    TextAlign = ContentAlignment.MiddleLeft
                };

                row.Controls.AddRange(new Control[] { lblIcon, lblQN, lblQ });

                if (!correct)
                {
                    var lblYourLbl = new Label
                    {
                        Text      = "Your answer:",
                        ForeColor = Color.FromArgb(140, 60, 60),
                        Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                        Location  = new Point(44, 50),
                        Size      = new Size(110, 18),
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    var lblYA = new Label
                    {
                        Text      = yourAns,
                        ForeColor = Color.FromArgb(180, 45, 45),
                        Font      = new Font("Segoe UI", 8.5f),
                        Location  = new Point(158, 50),
                        Size      = new Size(478, 18),
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    var lblCorrLbl = new Label
                    {
                        Text      = "Correct answer:",
                        ForeColor = Color.FromArgb(20, 100, 50),
                        Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                        Location  = new Point(44, 72),
                        Size      = new Size(110, 18),
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    var lblCA = new Label
                    {
                        Text      = correctAns,
                        ForeColor = Color.FromArgb(20, 110, 55),
                        Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                        Location  = new Point(158, 72),
                        Size      = new Size(478, 18),
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    row.Controls.AddRange(new Control[] { lblYourLbl, lblYA, lblCorrLbl, lblCA });
                }

                scroll.Controls.Add(row);
                y += rowH + 6;
            }

            // ══════════════════════════════════════════════════════════
            // Add controls in correct dock order:
            //   Top elements first (header, revHdr), then Bottom, then Fill
            // ══════════════════════════════════════════════════════════
            Controls.Add(scroll);       // Fill  — added first so it goes behind
            Controls.Add(pnlFooter);    // Bottom
            Controls.Add(pnlRevHdr);    // Top (second, so below header)
            Controls.Add(pnlHeader);    // Top (added last = topmost)
        }
    }
}
