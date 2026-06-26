using System;
using System.Drawing;
using System.Windows.Forms;
using CyberBot_Part3.Logic;

namespace CyberBot_Part3.Forms
{
    public class ActivityLogForm : UserControl
    {
        private static readonly Color BgDark = Color.FromArgb(15, 15, 25);
        private static readonly Color BgPanel = Color.FromArgb(22, 22, 38);
        private static readonly Color AccentCyan = Color.FromArgb(0, 220, 220);
        private static readonly Color AccentMag = Color.FromArgb(180, 0, 220);
        private static readonly Color TextWhite = Color.FromArgb(230, 230, 240);
        private static readonly Color TextGray = Color.FromArgb(140, 140, 160);

        private ListView _logList;
        private Button _refreshBtn;
        private Button _showAllBtn;
        private Label _countLabel;
        private bool _showingAll = false;

        public ActivityLogForm()
        {
            BackColor = BgDark;
            Dock = DockStyle.Fill;
            BuildUI();
        }

        private void BuildUI()
        {
            // Title
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = BgPanel };
            titleBar.Controls.Add(new Label
            {
                Text = "📊  Activity Log — Recent Chatbot Actions",
                Dock = DockStyle.Fill,
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            });
            Controls.Add(titleBar);

            // Button bar
            var btnBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = BgPanel, Padding = new Padding(8) };

            _refreshBtn = MakeBtn("🔄  Refresh", AccentMag);
            _refreshBtn.Location = new Point(8, 6);
            _refreshBtn.Click += (s, e) => LoadLog();
            btnBar.Controls.Add(_refreshBtn);

            _showAllBtn = MakeBtn("📜  Show All", Color.FromArgb(40, 40, 90));
            _showAllBtn.Location = new Point(118, 6);
            _showAllBtn.Click += OnToggleAll;
            btnBar.Controls.Add(_showAllBtn);

            _countLabel = new Label
            {
                Location = new Point(230, 13),
                AutoSize = true,
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnBar.Controls.Add(_countLabel);
            Controls.Add(btnBar);

            // List
            _logList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = BgDark,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };

            _logList.Columns.Add("#", 40);
            _logList.Columns.Add("Time", 70);
            _logList.Columns.Add("Category", 90);
            _logList.Columns.Add("Description", 500);

            Controls.Add(_logList);

            LoadLog();
        }

        public void LoadLog()
        {
            _logList.Items.Clear();
            var entries = _showingAll ? ActivityLog.GetAll() : ActivityLog.GetRecent(10);

            int num = 1;
            foreach (var entry in entries)
            {
                var item = new ListViewItem(num.ToString());
                item.SubItems.Add(entry.Timestamp.ToString("HH:mm:ss"));
                item.SubItems.Add(entry.Category);
                item.SubItems.Add(entry.Description);

                // Colour-code by category
                item.ForeColor = entry.Category switch
                {
                    "Task" => Color.FromArgb(0, 220, 220),
                    "Reminder" => Color.FromArgb(255, 200, 50),
                    "Quiz" => Color.FromArgb(100, 220, 100),
                    "NLP" => Color.FromArgb(180, 0, 220),
                    _ => Color.FromArgb(200, 200, 220)
                };

                _logList.Items.Add(item);
                num++;
            }

            _countLabel.Text = _showingAll
                ? $"Showing all {ActivityLog.TotalCount} entries"
                : $"Showing last {entries.Count} of {ActivityLog.TotalCount} entries";
        }

        private void OnToggleAll(object sender, EventArgs e)
        {
            _showingAll = !_showingAll;
            _showAllBtn.Text = _showingAll ? "📄  Show Recent" : "📜  Show All";
            LoadLog();
        }

        private static Button MakeBtn(string text, Color colour)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(104, 32),
                BackColor = colour,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}