using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Windows.Forms;
using CyberBot_Part3.Data;
using CyberBot_Part3.Logic;

namespace CyberBot_Part3.Forms
{
    public class MainForm : Form
    {
        // ── Engine ─────────────────────────────────────────────────────────────
        private readonly ChatbotEngine _engine = new ChatbotEngine();

        // ── Colours ────────────────────────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(15, 15, 25);
        private static readonly Color BgPanel = Color.FromArgb(22, 22, 38);
        private static readonly Color AccentCyan = Color.FromArgb(0, 220, 220);
        private static readonly Color AccentMag = Color.FromArgb(180, 0, 220);
        private static readonly Color TextWhite = Color.FromArgb(230, 230, 240);
        private static readonly Color TextGray = Color.FromArgb(140, 140, 160);
        private static readonly Color InputBg = Color.FromArgb(28, 28, 45);

        // ── Chat controls ──────────────────────────────────────────────────────
        private RichTextBox _chatBox;
        private TextBox _inputBox;
        private Button _sendBtn;
        private Label _memoryLabel;
        private ComboBox _personalityBox;
        private Label _sentimentLabel;

        // ── Tab sub-forms ──────────────────────────────────────────────────────
        private TasksForm _tasksForm;
        private ActivityLogForm _logForm;

        // ── State ──────────────────────────────────────────────────────────────
        private string _userName = "";
        private string _personality = "friendly";

        // ══════════════════════════════════════════════════════════════════════
        public MainForm()
        {
            // Try to connect to MySQL; warn but don't crash
            if (!DatabaseManager.TestConnection(out string dbError))
            {
                MessageBox.Show(
                    $"⚠️ Could not connect to MySQL:\n{dbError}\n\n" +
                    "Task storage will be unavailable until you fix the connection.\n" +
                    "Check your MySQL settings in DatabaseManager.cs.",
                    "Database Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            InitializeUI();
            ShowWelcomeDialog();
            PlayGreeting();
            ShowWelcomeMessages();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  UI CONSTRUCTION
        // ══════════════════════════════════════════════════════════════════════
        private void InitializeUI()
        {
            Text = "CyberBot v3 — Cybersecurity Awareness Assistant";
            Size = new Size(1200, 760);
            MinimumSize = new Size(1000, 640);
            BackColor = BgDark;
            ForeColor = TextWhite;
            Font = new Font("Segoe UI", 10f);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            var helpStrip = new Panel { Dock = DockStyle.Bottom, Height = 22, BackColor = Color.FromArgb(10, 10, 20) };
            helpStrip.Controls.Add(new Label
            {
                Text = "  💡 Try: 'add a task to enable 2FA'  |  'start quiz'  |  'show activity log'  |  'what is phishing'",
                Dock = DockStyle.Fill,
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 9f),
                TextAlign = ContentAlignment.MiddleLeft
            });
            Controls.Add(helpStrip);

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = BgPanel
            };
            ApplyTabStyle(tabs);

            var chatTab = new TabPage("💬  Chat") { BackColor = BgDark, Padding = new Padding(0) };
            chatTab.Controls.Add(BuildChatPanel());
            tabs.TabPages.Add(chatTab);

            var tasksTab = new TabPage("📋  Tasks") { BackColor = BgDark, Padding = new Padding(0) };
            _tasksForm = new TasksForm { Dock = DockStyle.Fill };
            tasksTab.Controls.Add(_tasksForm);
            tabs.TabPages.Add(tasksTab);

            var quizTab = new TabPage("🎮  Quiz") { BackColor = BgDark, Padding = new Padding(0) };
            quizTab.Controls.Add(BuildQuizPanel());
            tabs.TabPages.Add(quizTab);

            var logTab = new TabPage("📊  Activity Log") { BackColor = BgDark, Padding = new Padding(0) };
            _logForm = new ActivityLogForm { Dock = DockStyle.Fill };
            logTab.Controls.Add(_logForm);
            tabs.TabPages.Add(logTab);

            tabs.SelectedIndexChanged += (s, e) =>
            {
                if (tabs.SelectedTab == logTab) _logForm.LoadLog();
                if (tabs.SelectedTab == tasksTab) _tasksForm.Refresh();
            };

            Controls.Add(tabs);

            Panel sidebar = BuildSidebar();
            Controls.Add(sidebar);
        }

        // ── Sidebar ────────────────────────────────────────────────────────────
        private Panel BuildSidebar()
        {
            var sidebar = new Panel
            {
                Dock = DockStyle.Right,
                Width = 160,
                BackColor = BgPanel,
                Padding = new Padding(8)
            };

            var logo = new Label
            {
                Text = "CYbER\nSECURITY BOT V3\n★ Stay Safe ★",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = AccentMag,
                AutoSize = false,
                Size = new Size(144, 60),
                Location = new Point(8, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };
            sidebar.Controls.Add(logo);

            AddHRule(8, 75, 144, sidebar);
            AddSideLabel("PERSONALITY", 85, sidebar);

            _personalityBox = new ComboBox
            {
                Location = new Point(8, 103),
                Size = new Size(144, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = InputBg,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };
            _personalityBox.Items.AddRange(new object[] { "Friendly", "Professional", "Futuristic AI", "Casual" });
            _personalityBox.SelectedIndex = 0;
            _personalityBox.SelectedIndexChanged += OnPersonalityChanged;
            sidebar.Controls.Add(_personalityBox);

            AddHRule(8, 138, 144, sidebar);
            AddSideLabel("MEMORY", 148, sidebar);

            _memoryLabel = new Label
            {
                Text = "Name: -\nFav Topic: -",
                Location = new Point(8, 165),
                Size = new Size(144, 45),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 8.5f)
            };
            sidebar.Controls.Add(_memoryLabel);

            AddHRule(8, 218, 144, sidebar);
            AddSideLabel("MOOD DETECTED", 228, sidebar);

            _sentimentLabel = new Label
            {
                Text = "Neutral",
                Location = new Point(8, 246),
                Size = new Size(144, 25),
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            sidebar.Controls.Add(_sentimentLabel);

            AddHRule(8, 278, 144, sidebar);
            AddSideLabel("QUICK TOPICS", 288, sidebar);

            string[] topics = { "Passwords", "Phishing", "Malware", "VPN", "2FA", "Firewall", "Encryption", "Privacy", "Scams" };
            for (int i = 0; i < topics.Length; i++)
            {
                string t = topics[i];
                var btn = new Button
                {
                    Text = t,
                    Location = new Point(8, 306 + i * 24),
                    Size = new Size(144, 22),
                    BackColor = BgPanel,
                    ForeColor = AccentCyan,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.5f),
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(40, 40, 70);
                btn.Click += (s, e) => { _inputBox.Text = $"Tell me about {t.ToLower()}"; ProcessInput(); };
                btn.MouseEnter += (s, e) => btn.ForeColor = Color.White;
                btn.MouseLeave += (s, e) => btn.ForeColor = AccentCyan;
                sidebar.Controls.Add(btn);
            }

            return sidebar;
        }

        // ── Chat panel ─────────────────────────────────────────────────────────
        private Panel BuildChatPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };

            var titleBar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = BgPanel };
            titleBar.Controls.Add(new Label
            {
                Text = "CYBERBOT v3 — Chat Assistant",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AccentCyan,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
            panel.Controls.Add(titleBar);

            var inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = BgPanel, Padding = new Padding(8) };

            _sendBtn = new Button
            {
                Text = "Send",
                Dock = DockStyle.Right,
                Width = 100,
                BackColor = AccentMag,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _sendBtn.FlatAppearance.BorderSize = 0;
            _sendBtn.Click += OnSendClick;
            _sendBtn.MouseEnter += (s, e) => _sendBtn.BackColor = Color.FromArgb(0, 180, 180);
            _sendBtn.MouseLeave += (s, e) => _sendBtn.BackColor = AccentMag;
            inputPanel.Controls.Add(_sendBtn);

            _inputBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BackColor = InputBg,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 11f),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Type a message, task, or command..."
            };
            _inputBox.KeyDown += OnInputKeyDown;
            inputPanel.Controls.Add(_inputBox);
            panel.Controls.Add(inputPanel);

            _chatBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = BgDark,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.None,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false,
                DetectUrls = true,
                HideSelection = false,
            };
            panel.Controls.Add(_chatBox);

            return panel;
        }

        // ── Quiz panel ─────────────────────────────────────────────────────────
        private Panel BuildQuizPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark };

            var titleBar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = BgPanel };
            titleBar.Controls.Add(new Label
            {
                Text = "🎮  Cybersecurity Mini-Game — Quiz",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = AccentCyan,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
            panel.Controls.Add(titleBar);

            // Quiz content area
            var quizPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(30)
            };

            var infoLbl = new Label
            {
                Text = "💡 The quiz is also accessible directly from the chat!\n" +
                            "Type 'start quiz' in the Chat tab, or click the button below to begin here.",
                Dock = DockStyle.Top,
                Height = 50,
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            quizPanel.Controls.Add(infoLbl);

            var startBtn = new Button
            {
                Text = "🎮  Start Quiz in Chat",
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = AccentMag,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            startBtn.FlatAppearance.BorderSize = 0;
            startBtn.Click += (s, e) =>
            {
                // Switch to chat tab and fire quiz
                _inputBox.Text = "start quiz";
                ProcessInput();
                // Switch to chat tab (tab 0)
                ((TabControl)((TabPage)panel.Parent).Parent).SelectedIndex = 0;
            };
            quizPanel.Controls.Add(startBtn);

            // Quiz info
            var infoBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = BgPanel,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.None,
                Text =
                    "📋 QUIZ OVERVIEW\r\n" +
                    "══════════════════════════════════════\r\n\r\n" +
                    "✅ 12 questions covering key cybersecurity topics\r\n" +
                    "✅ Mix of multiple-choice (A/B/C/D) and True/False questions\r\n" +
                    "✅ Immediate feedback after each answer\r\n" +
                    "✅ Final score with personalised feedback\r\n" +
                    "✅ Activity logged automatically\r\n\r\n" +
                    "TOPICS COVERED:\r\n" +
                    "  • Phishing & email safety\r\n" +
                    "  • Password security\r\n" +
                    "  • Two-Factor Authentication (2FA)\r\n" +
                    "  • VPN & public Wi-Fi safety\r\n" +
                    "  • Social engineering\r\n" +
                    "  • Malware & ransomware\r\n" +
                    "  • HTTPS & web safety\r\n" +
                    "  • Firewalls & encryption\r\n\r\n" +
                    "HOW TO ANSWER:\r\n" +
                    "  • Multiple choice: type A, B, C, or D\r\n" +
                    "  • True/False: type True or False\r\n"
            };
            quizPanel.Controls.Add(infoBox);

            panel.Controls.Add(quizPanel);
            return panel;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  WELCOME
        // ══════════════════════════════════════════════════════════════════════
        private void ShowWelcomeDialog()
        {
            using (var dialog = new Form())
            {
                dialog.Text = "Welcome to CyberBot v3";
                dialog.Size = new Size(420, 280);
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.BackColor = BgPanel;
                dialog.ForeColor = TextWhite;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;

                dialog.Controls.Add(new Label
                {
                    Text = "Welcome to CyberBot v3",
                    Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = AccentCyan,
                    Location = new Point(20, 15),
                    Size = new Size(380, 30)
                });

                dialog.Controls.Add(new Label { Text = "Your Name:", Location = new Point(20, 60), AutoSize = true });

                var nameBox = new TextBox
                {
                    Location = new Point(20, 82),
                    Size = new Size(370, 26),
                    BackColor = InputBg,
                    ForeColor = TextWhite,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 11f)
                };
                dialog.Controls.Add(nameBox);

                dialog.Controls.Add(new Label { Text = "Choose Personality:", Location = new Point(20, 120), AutoSize = true });

                var pBox = new ComboBox
                {
                    Location = new Point(20, 142),
                    Size = new Size(370, 26),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = InputBg,
                    ForeColor = TextWhite
                };
                pBox.Items.AddRange(new object[] { "Friendly", "Professional", "Futuristic AI", "Casual" });
                pBox.SelectedIndex = 0;
                dialog.Controls.Add(pBox);

                var ok = new Button
                {
                    Text = "Start Chatting!",
                    Location = new Point(140, 195),
                    Size = new Size(140, 36),
                    BackColor = AccentMag,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold)
                };
                ok.FlatAppearance.BorderSize = 0;
                ok.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        MessageBox.Show("Please enter your name.", "CyberBot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                };
                dialog.Controls.Add(ok);
                dialog.AcceptButton = ok;
                dialog.ShowDialog();

                _userName = string.IsNullOrWhiteSpace(nameBox.Text) ? "User" : nameBox.Text.Trim();
                _engine.Memory.Name = _userName;
                _personality = ParsePersonality(pBox.SelectedItem?.ToString() ?? "");
                _personalityBox.SelectedIndex = pBox.SelectedIndex;
                UpdateMemoryDisplay();

                ActivityLog.Add("Chat", $"Session started for user: {_userName}");
            }
        }

        private void ShowWelcomeMessages()
        {
            AppendSystem("╔══════════════════════════════════════════════════╗");
            AppendSystem("║  🛡️  CYBERBOT v3.0  |  Part 3/POE  🛡️           ║");
            AppendSystem("║  🔐 Cybersecurity Awareness Assistant            ║");
            AppendSystem("║  📋 Tasks  |  🎮 Quiz  |  📊 Log  |  🧠 NLP    ║");
            AppendSystem("║  ⚠️  Stay Safe. Stay Informed. Stay Secure.     ║");
            AppendSystem("╚══════════════════════════════════════════════════╝");
            AppendBot($"Hello {_userName}! Welcome to CyberBot v3 — your advanced cybersecurity assistant.");
            AppendBot("NEW in v3: Task Manager 📋 | Cybersecurity Quiz 🎮 | Activity Log 📊 | Advanced NLP 🧠");
            AppendBot("Try: 'add a task to enable 2FA' | 'start quiz' | 'show activity log' | 'tell me about phishing'");
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INPUT HANDLING
        // ══════════════════════════════════════════════════════════════════════
        private void OnSendClick(object sender, EventArgs e) => ProcessInput();
        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ProcessInput(); }
        }

        private void ProcessInput()
        {
            string input = _inputBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;
            _inputBox.Clear();

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                AppendUser(input);
                AppendBot($"Goodbye, {_userName}! Stay safe online. 👋");
                System.Threading.Thread.Sleep(600);
                Application.Exit();
                return;
            }

            AppendUser(input);
            Sentiment s = SentimentDetector.Detect(input);
            UpdateSentimentLabel(s);
            string response = _engine.GetResponse(input, _personality);
            AppendBot(response);
            UpdateMemoryDisplay();
        }
        
        // ══════════════════════════════════════════════════════════════════════
        //  DISPLAY HELPERS
        // ══════════════════════════════════════════════════════════════════════
        private void AppendUser(string text)
        {
            _chatBox.SuspendLayout();

            _chatBox.SelectionStart = _chatBox.TextLength;
            _chatBox.SelectionColor = AccentCyan;
            _chatBox.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _chatBox.AppendText(Environment.NewLine + $"You ({_userName}):" + Environment.NewLine);

            _chatBox.SelectionColor = TextWhite;
            _chatBox.SelectionFont = new Font("Segoe UI", 10.5f);
            _chatBox.AppendText("  " + text + Environment.NewLine + Environment.NewLine);

            _chatBox.SelectionStart = _chatBox.TextLength;
            _chatBox.SelectionLength = 0;
            _chatBox.ScrollToCaret();

            _chatBox.ResumeLayout();
        }

        private void AppendBot(string text)
        {
            _chatBox.SuspendLayout();

            _chatBox.SelectionStart = _chatBox.TextLength;
            _chatBox.SelectionColor = AccentMag;
            _chatBox.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            _chatBox.AppendText(Environment.NewLine + "CyberBot:" + Environment.NewLine);

            _chatBox.SelectionColor = TextWhite;
            _chatBox.SelectionFont = new Font("Segoe UI", 10.5f);

            _chatBox.AppendText("  " + text + Environment.NewLine + Environment.NewLine);

            _chatBox.SelectionStart = _chatBox.TextLength;
            _chatBox.SelectionLength = 0;
            _chatBox.ScrollToCaret();

            _chatBox.ResumeLayout();
        }
        private void AppendSystem(string text)
        {
            _chatBox.SuspendLayout();

            _chatBox.SelectionStart = _chatBox.TextLength;
            _chatBox.SelectionAlignment = HorizontalAlignment.Center;
            _chatBox.SelectionColor = Color.DeepSkyBlue;
            _chatBox.SelectionFont = new Font("Consolas", 17f, FontStyle.Bold);

            _chatBox.AppendText(text + Environment.NewLine);

            _chatBox.SelectionAlignment = HorizontalAlignment.Left;

            _chatBox.SelectionStart = _chatBox.TextLength;
            _chatBox.SelectionLength = 0;
            _chatBox.ScrollToCaret();

            _chatBox.ResumeLayout();
        }

        private void UpdateMemoryDisplay()
        {
            string name = string.IsNullOrEmpty(_engine.Memory.Name) ? "-" : _engine.Memory.Name;
            string topic = string.IsNullOrEmpty(_engine.Memory.FavouriteTopic) ? "-" : _engine.Memory.FavouriteTopic;
            _memoryLabel.Text = $"Name: {name}\nFav Topic: {topic}";
        }

        private void UpdateSentimentLabel(Sentiment s)
        {
            switch (s)
            {
                case Sentiment.Worried: _sentimentLabel.Text = "Worried"; _sentimentLabel.ForeColor = Color.Orange; break;
                case Sentiment.Frustrated: _sentimentLabel.Text = "Frustrated"; _sentimentLabel.ForeColor = Color.Tomato; break;
                case Sentiment.Curious: _sentimentLabel.Text = "Curious"; _sentimentLabel.ForeColor = AccentCyan; break;
                case Sentiment.Happy: _sentimentLabel.Text = "Happy"; _sentimentLabel.ForeColor = Color.LightGreen; break;
                default: _sentimentLabel.Text = "Neutral"; _sentimentLabel.ForeColor = TextGray; break;
            }
        }

        private void OnPersonalityChanged(object sender, EventArgs e)
        {
            _personality = ParsePersonality(_personalityBox.SelectedItem?.ToString() ?? "");
            AppendSystem($"-- Personality changed to: {_personalityBox.SelectedItem} --");
        }

        private static string ParsePersonality(string label)
        {
            if (label.Contains("Professional")) return "professional";
            if (label.Contains("AI") || label.Contains("Futuristic")) return "ai";
            if (label.Contains("Casual")) return "casual";
            return "friendly";
        }

        private static void PlayGreeting()
        {
            try
            {
                string[] paths =
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "greeting.wav"),
                    Path.Combine(Environment.CurrentDirectory, "greeting.wav")
                };
                foreach (string p in paths)
                    if (File.Exists(p)) { new SoundPlayer(p).Play(); return; }
            }
            catch { }
        }

        // ── Tab styling ────────────────────────────────────────────────────────
        private static void ApplyTabStyle(TabControl tc)
        {
            tc.DrawMode = TabDrawMode.OwnerDrawFixed;
            tc.DrawItem += (s, e) =>
            {
                var page = tc.TabPages[e.Index];
                bool sel = (e.Index == tc.SelectedIndex);
                using (var bg = new SolidBrush(sel ? Color.FromArgb(30, 30, 50) : Color.FromArgb(18, 18, 32)))
                    e.Graphics.FillRectangle(bg, e.Bounds);
                Color fg = sel ? Color.FromArgb(0, 220, 220) : Color.FromArgb(140, 140, 160);
                TextRenderer.DrawText(e.Graphics, page.Text, new Font("Segoe UI", 9.5f, FontStyle.Bold),
                                      e.Bounds, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private static void AddHRule(int x, int y, int w, Panel parent)
            => parent.Controls.Add(new Panel { Location = new Point(x, y), Size = new Size(w, 1), BackColor = Color.FromArgb(40, 40, 70) });

        private static void AddSideLabel(string text, int y, Panel parent)
            => parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(8, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 100, 140),
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
            });
    }
}