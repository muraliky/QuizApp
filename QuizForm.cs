using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuizApp
{
    public class QuizForm : Form
    {
        // ── palette ────────────────────────────────────────────────────
        private static readonly Color C_BLUE       = Color.FromArgb(41,  98, 172);
        private static readonly Color C_BLUE_LIGHT = Color.FromArgb(235, 243, 255);
        private static readonly Color C_BLUE_SEL   = Color.FromArgb(210, 230, 255);
        private static readonly Color C_BG         = Color.FromArgb(245, 247, 250);
        private static readonly Color C_WHITE      = Color.White;
        private static readonly Color C_TEXT       = Color.FromArgb(30,  30,  30);
        private static readonly Color C_MUTED      = Color.FromArgb(110, 110, 110);
        private static readonly Color C_BORDER     = Color.FromArgb(200, 210, 225);

        // ── data ───────────────────────────────────────────────────────
        private readonly List<Question> _questions;
        private readonly string         _fullName, _quizTitle, _scoreFilePath;
        private readonly int[]          _userAnswers;
        private int _current = 0;

        // ── controls ───────────────────────────────────────────────────
        private Panel       pnlHeader;
        private Label       lblTitle, lblUser, lblProgress;
        private ProgressBar pbProgress;

        private Panel       pnlQuestion;
        private Label       lblQNum, lblQText;

        private Panel       pnlOptions;
        private RadioButton[] radOptions = new RadioButton[4];
        private Panel[]     pnlOptCards  = new Panel[4];

        private Panel       pnlNav;
        private FlowLayoutPanel flpDots;
        private Label[]     dotLabels;

        private Panel       pnlFooter;
        private Button      btnPrev, btnNext;
        private Label       lblWarn;

        public QuizForm(List<Question> questions, string fullName,
                        string quizTitle, string scoreFilePath)
        {
            _questions     = questions;
            _fullName      = fullName;
            _quizTitle     = quizTitle;
            _scoreFilePath = scoreFilePath;
            _userAnswers   = new int[questions.Count];
            for (int i = 0; i < _userAnswers.Length; i++) _userAnswers[i] = -1;

            BuildUI();
            LoadQuestion(0);
        }

        private void BuildUI()
        {
            Text            = _quizTitle;
            Size            = new Size(700, 660);
            MinimumSize     = new Size(700, 660);
            MaximumSize     = new Size(700, 660);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = C_BG;
            Font            = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;

            // ── HEADER ─────────────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = C_BLUE
            };

            lblTitle = new Label
            {
                Text      = _quizTitle,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(400, 36),
                Location  = new Point(20, 14),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblUser = new Label
            {
                Text      = "👤  " + _fullName,
                ForeColor = Color.FromArgb(180, 210, 255),
                Font      = new Font("Segoe UI", 9f),
                AutoSize  = false,
                Size      = new Size(400, 22),
                Location  = new Point(22, 52),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblProgress = new Label
            {
                Text      = "",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize  = false,
                Size      = new Size(120, 22),
                Location  = new Point(558, 14),
                TextAlign = ContentAlignment.MiddleRight
            };

            pbProgress = new ProgressBar
            {
                Location = new Point(500, 50),
                Size     = new Size(178, 12),
                Minimum  = 0,
                Maximum  = _questions.Count,
                Style    = ProgressBarStyle.Continuous
            };

            pnlHeader.Controls.AddRange(new Control[]
                { lblTitle, lblUser, lblProgress, pbProgress });

            // ── QUESTION PANEL ─────────────────────────────────────────
            pnlQuestion = new Panel
            {
                Location  = new Point(18, 106),
                Size      = new Size(656, 120),
                BackColor = C_WHITE,
                Padding   = new Padding(16)
            };
            pnlQuestion.Paint += PaintBorder;

            lblQNum = new Label
            {
                Location  = new Point(16, 14),
                Size      = new Size(620, 18),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = C_BLUE
            };

            lblQText = new Label
            {
                Location  = new Point(16, 36),
                Size      = new Size(620, 76),
                Font      = new Font("Segoe UI", 11.5f),
                ForeColor = C_TEXT
            };

            pnlQuestion.Controls.AddRange(new Control[] { lblQNum, lblQText });

            // ── OPTIONS ────────────────────────────────────────────────
            pnlOptions = new Panel
            {
                Location  = new Point(18, 238),
                Size      = new Size(656, 272),
                BackColor = Color.Transparent
            };

            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;

                // Card panel
                var card = new Panel
                {
                    Location  = new Point(0, i * 66),
                    Size      = new Size(656, 58),
                    BackColor = C_WHITE,
                    Cursor    = Cursors.Hand,
                    Tag       = i
                };
                card.Paint += PaintBorder;

                // Letter badge label
                var badge = new Label
                {
                    Text      = letters[i],
                    Size      = new Size(32, 32),
                    Location  = new Point(14, 12),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                    BackColor = Color.FromArgb(220, 235, 255),
                    ForeColor = C_BLUE
                };

                // Radio button (hidden visually, used for state)
                var rad = new RadioButton
                {
                    Location  = new Point(56, 18),
                    Size      = new Size(580, 24),
                    Font      = new Font("Segoe UI", 10.5f),
                    ForeColor = C_TEXT,
                    BackColor = Color.Transparent,
                    Cursor    = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    AutoSize  = false
                };
                rad.CheckedChanged += (s, e) =>
                {
                    if (rad.Checked) SelectOption(idx);
                };

                // Click on card also selects
                card.Click    += (s, e) => { rad.Checked = true; };
                badge.Click   += (s, e) => { rad.Checked = true; };

                card.Controls.Add(badge);
                card.Controls.Add(rad);
                pnlOptions.Controls.Add(card);

                pnlOptCards[i] = card;
                radOptions[i]  = rad;
            }

            // ── NAV DOTS ───────────────────────────────────────────────
            pnlNav = new Panel
            {
                Location  = new Point(18, 520),
                Size      = new Size(656, 30),
                BackColor = C_BG
            };

            flpDots = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false
            };
            // centre the dots
            flpDots.Padding = new Padding(0, 5, 0, 0);

            dotLabels = new Label[_questions.Count];
            for (int i = 0; i < _questions.Count; i++)
            {
                int idx = i;
                var dot = new Label
                {
                    Size      = new Size(20, 20),
                    BackColor = C_BORDER,
                    Cursor    = Cursors.Hand,
                    Margin    = new Padding(4, 0, 4, 0),
                    Text      = "",
                    Tag       = i
                };
                dot.Click += (s, e) => DotClicked(idx);
                dotLabels[i] = dot;
                flpDots.Controls.Add(dot);
            }
            pnlNav.Controls.Add(flpDots);

            // ── FOOTER ─────────────────────────────────────────────────
            pnlFooter = new Panel
            {
                Location  = new Point(0, 558),
                Size      = new Size(700, 62),
                BackColor = C_WHITE
            };

            // top divider
            pnlFooter.Paint += (s, e) =>
            {
                using var p = new System.Drawing.Pen(C_BORDER, 1);
                e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0);
            };

            lblWarn = new Label
            {
                Text      = "⚠  Please select an answer to continue.",
                ForeColor = Color.FromArgb(200, 80, 40),
                Font      = new Font("Segoe UI", 9f),
                Location  = new Point(20, 20),
                Size      = new Size(360, 22),
                Visible   = false
            };

            btnPrev = new Button
            {
                Text      = "← Previous",
                Location  = new Point(420, 10),
                Size      = new Size(120, 40),
                Font      = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(230, 238, 255),
                ForeColor = C_BLUE,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Visible   = false
            };
            btnPrev.FlatAppearance.BorderColor = C_BORDER;
            btnPrev.Click += (s, e) => NavPrev();

            btnNext = new Button
            {
                Text      = "Next →",
                Location  = new Point(552, 10),
                Size      = new Size(130, 40),
                Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = C_BLUE,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 78, 140);
            btnNext.Click += (s, e) => NavNext();

            pnlFooter.Controls.AddRange(new Control[] { lblWarn, btnPrev, btnNext });

            // ── Add all to form ────────────────────────────────────────
            Controls.Add(pnlHeader);
            Controls.Add(pnlQuestion);
            Controls.Add(pnlOptions);
            Controls.Add(pnlNav);
            Controls.Add(pnlFooter);
        }

        // ── Load a question ────────────────────────────────────────────
        private void LoadQuestion(int idx)
        {
            _current = idx;
            var q    = _questions[idx];
            bool isLast  = (idx == _questions.Count - 1);
            bool isFirst = (idx == 0);

            // Header
            lblProgress.Text = $"{idx + 1} / {_questions.Count}";
            pbProgress.Value = idx + 1;

            // Question
            lblQNum.Text  = $"QUESTION  {idx + 1}  OF  {_questions.Count}";
            lblQText.Text = q.QuestionText;

            // Options
            for (int i = 0; i < 4; i++)
            {
                radOptions[i].Text    = q.Options[i];
                radOptions[i].Checked = false;
                SetCardStyle(i, false);
            }

            // Restore saved answer
            if (_userAnswers[idx] >= 0)
            {
                radOptions[_userAnswers[idx]].Checked = true;
                SetCardStyle(_userAnswers[idx], true);
            }

            // Nav dots
            UpdateDots(idx);

            // Buttons
            btnPrev.Visible = !isFirst;
            lblWarn.Visible = false;

            if (isLast)
            {
                btnNext.Text      = "Submit ✓";
                btnNext.BackColor = Color.FromArgb(34, 139, 80);
                btnNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(26, 110, 62);
            }
            else
            {
                btnNext.Text      = "Next →";
                btnNext.BackColor = Color.FromArgb(41, 98, 172);
                btnNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 78, 140);
            }
        }

        private void SelectOption(int idx)
        {
            for (int i = 0; i < 4; i++) SetCardStyle(i, i == idx);
            _userAnswers[_current] = idx;
            UpdateDots(_current);
            lblWarn.Visible = false;
        }

        private void SetCardStyle(int idx, bool selected)
        {
            pnlOptCards[idx].BackColor = selected
                ? Color.FromArgb(210, 230, 255)
                : Color.White;

            // badge
            var badge = pnlOptCards[idx].Controls[0] as Label;
            if (badge != null)
            {
                badge.BackColor = selected
                    ? Color.FromArgb(41, 98, 172)
                    : Color.FromArgb(220, 235, 255);
                badge.ForeColor = selected ? Color.White : Color.FromArgb(41, 98, 172);
            }
        }

        private void UpdateDots(int current)
        {
            for (int i = 0; i < _questions.Count; i++)
            {
                bool isCur  = (i == current);
                bool isDone = (_userAnswers[i] >= 0);
                dotLabels[i].BackColor = isCur
                    ? Color.FromArgb(41, 98, 172)
                    : isDone
                        ? Color.FromArgb(100, 160, 230)
                        : Color.FromArgb(200, 210, 225);
            }
        }

        private void NavNext()
        {
            if (_userAnswers[_current] < 0)
            {
                lblWarn.Visible = true;
                return;
            }
            if (_current == _questions.Count - 1)
            {
                ShowResults();
                return;
            }
            LoadQuestion(_current + 1);
        }

        private void NavPrev()
        {
            if (_current > 0) LoadQuestion(_current - 1);
        }

        private void DotClicked(int idx)
        {
            // Allow jumping back freely, or forward only if answered
            if (idx < _current)
            {
                LoadQuestion(idx);
            }
            else if (idx == _current + 1 && _userAnswers[_current] >= 0)
            {
                LoadQuestion(idx);
            }
            else if (idx <= _current)
            {
                LoadQuestion(idx);
            }
            else
            {
                // Must answer in order
                lblWarn.Visible = (_userAnswers[_current] < 0);
            }
        }

        private void ShowResults()
        {
            int score = 0;
            for (int i = 0; i < _questions.Count; i++)
                if (_userAnswers[i] == _questions[i].CorrectAnswerIndex) score++;

            bool   ok  = false;
            string err = "";
            try
            {
                ExcelScoreWriter.WriteScore(_scoreFilePath, _fullName, score,
                                            _questions.Count, DateTime.Now);
                ok = true;
            }
            catch (Exception ex) { err = ex.Message; }

            var rf = new ResultForm(_questions, _userAnswers, _fullName, score,
                                    _quizTitle, ok, err);
            rf.FormClosed += (s, e) => Application.Exit();
            rf.Show();
            Hide();
        }

        // Paint a light border around panels
        private void PaintBorder(object sender, PaintEventArgs e)
        {
            var ctrl = (Control)sender;
            using var pen = new System.Drawing.Pen(Color.FromArgb(200, 210, 225), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
        }
    }
}
