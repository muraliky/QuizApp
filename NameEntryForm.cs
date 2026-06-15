using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace QuizApp
{
    public class NameEntryForm : Form
    {
        // ── config ─────────────────────────────────────────────────────
        private string _quizTitle;
        private string _scoreFilePath;
        private string _questionsFilePath;

        // ── controls ───────────────────────────────────────────────────
        private Panel    pnlHeader;
        private Label    lblAppTitle;
        private Label    lblSubtitle;
        private Panel    pnlBody;
        private Label    lblFirstName;
        private TextBox  txtFirstName;
        private Label    lblLastName;
        private TextBox  txtLastName;
        private Label    lblError;
        private Button   btnStart;
        private Label    lblFooter;

        public NameEntryForm()
        {
            _quizTitle         = ConfigurationManager.AppSettings["QuizTitle"]         ?? "Monthly Quiz";
            _scoreFilePath     = ConfigurationManager.AppSettings["ScoreFilePath"]     ?? "QuizScores.xlsx";
            _questionsFilePath = ConfigurationManager.AppSettings["QuestionsFilePath"] ?? "questions.json";

            BuildUI();
        }

        private void BuildUI()
        {
            // ── Form ───────────────────────────────────────────────────
            Text            = _quizTitle;
            Size            = new Size(500, 520);
            MinimumSize     = new Size(500, 520);
            MaximumSize     = new Size(500, 520);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = Color.FromArgb(245, 247, 250);
            Font            = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;

            // ── Header panel ───────────────────────────────────────────
            pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 120,
                BackColor = Color.FromArgb(41, 98, 172)
            };

            lblAppTitle = new Label
            {
                Text      = _quizTitle,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 17f, FontStyle.Bold),
                AutoSize  = false,
                Width     = 460,
                Height    = 50,
                Location  = new Point(20, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblSubtitle = new Label
            {
                Text      = "Enter your details to begin the quiz",
                ForeColor = Color.FromArgb(180, 210, 255),
                Font      = new Font("Segoe UI", 10f),
                AutoSize  = false,
                Width     = 460,
                Height    = 28,
                Location  = new Point(20, 74),
                TextAlign = ContentAlignment.MiddleLeft
            };

            pnlHeader.Controls.Add(lblAppTitle);
            pnlHeader.Controls.Add(lblSubtitle);

            // ── Body panel ─────────────────────────────────────────────
            pnlBody = new Panel
            {
                Location  = new Point(0, 120),
                Size      = new Size(500, 340),
                BackColor = Color.White
            };

            // First Name
            lblFirstName = new Label
            {
                Text      = "First Name",
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location  = new Point(50, 40),
                AutoSize  = true
            };

            txtFirstName = new TextBox
            {
                Location    = new Point(50, 65),
                Size        = new Size(390, 30),
                Font        = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Color.FromArgb(248, 250, 253)
            };

            // Last Name
            lblLastName = new Label
            {
                Text      = "Last Name",
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                Location  = new Point(50, 115),
                AutoSize  = true
            };

            txtLastName = new TextBox
            {
                Location    = new Point(50, 140),
                Size        = new Size(390, 30),
                Font        = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor   = Color.FromArgb(248, 250, 253)
            };

            // Error label
            lblError = new Label
            {
                Text      = "",
                ForeColor = Color.FromArgb(200, 50, 50),
                Font      = new Font("Segoe UI", 9f),
                Location  = new Point(50, 190),
                Size      = new Size(390, 22),
                Visible   = false
            };

            // Start button
            btnStart = new Button
            {
                Text      = "Start Quiz  →",
                Location  = new Point(50, 225),
                Size      = new Size(390, 46),
                Font      = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = Color.FromArgb(41, 98, 172),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnStart.FlatAppearance.BorderSize  = 0;
            btnStart.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 78, 140);
            btnStart.Click += BtnStart_Click;

            pnlBody.Controls.AddRange(new Control[]
            {
                lblFirstName, txtFirstName,
                lblLastName,  txtLastName,
                lblError,     btnStart
            });

            // ── Footer ─────────────────────────────────────────────────
            lblFooter = new Label
            {
                Text      = "CATS Team  •  UAT Center of Excellence",
                ForeColor = Color.FromArgb(160, 160, 160),
                Font      = new Font("Segoe UI", 8.5f),
                Dock      = DockStyle.Bottom,
                Height    = 36,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(245, 247, 250)
            };

            Controls.Add(pnlHeader);
            Controls.Add(pnlBody);
            Controls.Add(lblFooter);

            // ── Keyboard nav ───────────────────────────────────────────
            txtFirstName.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                { txtLastName.Focus(); e.SuppressKeyPress = true; }
            };
            txtLastName.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                { BtnStart_Click(s, e); e.SuppressKeyPress = true; }
            };
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            string fn = txtFirstName.Text.Trim();
            string ln = txtLastName.Text.Trim();

            if (string.IsNullOrEmpty(fn) || string.IsNullOrEmpty(ln))
            {
                ShowError("Please enter both your first and last name.");
                return;
            }

            List<Question> questions;
            try
            {
                string path = Path.IsPathRooted(_questionsFilePath)
                    ? _questionsFilePath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _questionsFilePath);

                if (!File.Exists(path))
                {
                    ShowError($"questions.json not found at: {path}");
                    return;
                }

                questions = JsonConvert.DeserializeObject<List<Question>>(File.ReadAllText(path));

                if (questions == null || questions.Count == 0)
                {
                    ShowError("No questions found in questions.json.");
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowError("Error loading questions: " + ex.Message);
                return;
            }

            string fullName = $"{fn} {ln}";

            // ── Duplicate check ────────────────────────────────────────
            try
            {
                if (ExcelScoreWriter.NameExists(_scoreFilePath, fullName))
                {
                    ShowError($"{fullName} has already submitted. Contact the organiser.");
                    return;
                }
            }
            catch
            {
                // If the file doesn't exist yet or can't be read, allow through
            }

            lblError.Visible = false;
            var quizForm     = new QuizForm(questions, fullName, _quizTitle, _scoreFilePath);
            quizForm.Show();
            Hide();
            // Do NOT close NameEntryForm when QuizForm closes - QuizForm hides itself
            // and ResultForm handles Application.Exit() when user is done.
        }

        private void ShowError(string msg)
        {
            lblError.Text    = "⚠  " + msg;
            lblError.Visible = true;
        }
    }
}
