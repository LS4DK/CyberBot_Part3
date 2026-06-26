using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CyberBot_Part3.Data;
using CyberBot_Part3.Logic;

namespace CyberBot_Part3.Forms
{
    public class TasksForm : UserControl
    {
        // ── Colours ────────────────────────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(15, 15, 25);
        private static readonly Color BgPanel = Color.FromArgb(22, 22, 38);
        private static readonly Color AccentCyan = Color.FromArgb(0, 220, 220);
        private static readonly Color AccentMag = Color.FromArgb(180, 0, 220);
        private static readonly Color TextWhite = Color.FromArgb(230, 230, 240);
        private static readonly Color TextGray = Color.FromArgb(140, 140, 160);
        private static readonly Color InputBg = Color.FromArgb(28, 28, 45);
        private static readonly Color GreenDone = Color.FromArgb(0, 180, 80);

        // ── Controls ───────────────────────────────────────────────────────────
        private ListView _taskList;
        private TextBox _titleBox;
        private TextBox _descBox;
        private TextBox _reminderBox;
        private Button _addBtn;
        private Button _doneBtn;
        private Button _deleteBtn;
        private Button _refreshBtn;
        private Label _statusLabel;

        public TasksForm()
        {
            BackColor = BgDark;
            Dock = DockStyle.Fill;
            BuildUI();
            LoadTasks();
        }

        private void BuildUI()
        {
            // ── Title bar ──────────────────────────────────────────────────────
            var titleBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = BgPanel };
            var titleLbl = new Label
            {
                Text = "📋  Task Assistant — Cybersecurity Tasks",
                Dock = DockStyle.Fill,
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            titleBar.Controls.Add(titleLbl);
            Controls.Add(titleBar);

            // ── Add-task panel (right) ─────────────────────────────────────────
            var addPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 280,
                BackColor = BgPanel,
                Padding = new Padding(10)
            };

            int y = 10;
            addPanel.Controls.Add(MakeLabel("TASK TITLE *", ref y));
            _titleBox = MakeTextBox(ref y, addPanel, 26); addPanel.Controls.Add(_titleBox);

            addPanel.Controls.Add(MakeLabel("DESCRIPTION", ref y));
            _descBox = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(260, 60),
                BackColor = InputBg,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 10f),
                Multiline = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            addPanel.Controls.Add(_descBox);
            y += 70;

            addPanel.Controls.Add(MakeLabel("REMINDER (e.g. 'in 3 days')", ref y));
            _reminderBox = MakeTextBox(ref y, addPanel, 26); addPanel.Controls.Add(_reminderBox);

            _addBtn = new Button
            {
                Text = "➕  Add Task",
                Location = new Point(10, y + 5),
                Size = new Size(260, 36),
                BackColor = AccentMag,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _addBtn.FlatAppearance.BorderSize = 0;
            _addBtn.Click += OnAddTask;
            addPanel.Controls.Add(_addBtn);
            y += 50;

            // Separator
            addPanel.Controls.Add(new Panel
            {
                Location = new Point(10, y + 10),
                Size = new Size(260, 1),
                BackColor = Color.FromArgb(40, 40, 70)
            });
            y += 20;

            _doneBtn = MakeActionButton("✅  Mark as Done", GreenDone, ref y);
            _doneBtn.Click += OnMarkDone;
            addPanel.Controls.Add(_doneBtn);

            _deleteBtn = MakeActionButton("🗑️  Delete Task", Color.FromArgb(180, 40, 40), ref y);
            _deleteBtn.Click += OnDeleteTask;
            addPanel.Controls.Add(_deleteBtn);

            _refreshBtn = MakeActionButton("🔄  Refresh", Color.FromArgb(40, 40, 90), ref y);
            _refreshBtn.Click += (s, e) => LoadTasks();
            addPanel.Controls.Add(_refreshBtn);

            _statusLabel = new Label
            {
                Location = new Point(10, y + 10),
                Size = new Size(260, 40),
                ForeColor = AccentCyan,
                Font = new Font("Segoe UI", 8.5f),
                TextAlign = ContentAlignment.MiddleCenter
            };
            addPanel.Controls.Add(_statusLabel);

            Controls.Add(addPanel);

            // ── Task list ──────────────────────────────────────────────────────
            _taskList = new ListView
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

            _taskList.Columns.Add("ID", 45);
            _taskList.Columns.Add("Title", 200);
            _taskList.Columns.Add("Description", 280);
            _taskList.Columns.Add("Reminder", 120);
            _taskList.Columns.Add("Status", 80);
            _taskList.Columns.Add("Created", 130);

            Controls.Add(_taskList);
        }

        // ── Data ───────────────────────────────────────────────────────────────
        private void LoadTasks()
        {
            _taskList.Items.Clear();
            try
            {
                List<CyberTask> tasks = DatabaseManager.GetAllTasks();
                foreach (var t in tasks)
                {
                    var item = new ListViewItem(t.Id.ToString());
                    item.SubItems.Add(t.Title);
                    item.SubItems.Add(t.Description);
                    item.SubItems.Add(string.IsNullOrEmpty(t.ReminderDate) ? "—" : t.ReminderDate);
                    item.SubItems.Add(t.IsCompleted ? "✅ Done" : "⏳ Pending");
                    item.SubItems.Add(t.CreatedAt.ToString("dd MMM yyyy HH:mm"));
                    item.Tag = t.Id;
                    item.ForeColor = t.IsCompleted ? TextGray : TextWhite;
                    _taskList.Items.Add(item);
                }

                SetStatus($"{tasks.Count} task(s) loaded.", AccentCyan);
            }
            catch (Exception ex)
            {
                SetStatus($"DB Error: {ex.Message}", Color.Tomato);
            }
        }

        // ── Handlers ───────────────────────────────────────────────────────────
        private void OnAddTask(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_titleBox.Text))
            {
                SetStatus("Please enter a task title.", Color.Orange);
                return;
            }

            var task = new CyberTask
            {
                Title = _titleBox.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(_descBox.Text)
                                   ? $"Cybersecurity task: {_titleBox.Text.Trim()}"
                                   : _descBox.Text.Trim(),
                ReminderDate = _reminderBox.Text.Trim()
            };

            try
            {
                DatabaseManager.AddTask(task);
                ActivityLog.Add("Task", $"Task added via GUI: '{task.Title}'" +
                    (string.IsNullOrEmpty(task.ReminderDate) ? "" : $" | Reminder: {task.ReminderDate}"));

                _titleBox.Clear();
                _descBox.Clear();
                _reminderBox.Clear();
                LoadTasks();
                SetStatus("Task added successfully! ✅", GreenDone);
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", Color.Tomato);
            }
        }

        private void OnMarkDone(object sender, EventArgs e)
        {
            if (_taskList.SelectedItems.Count == 0)
            { SetStatus("Select a task first.", Color.Orange); return; }

            int id = (int)_taskList.SelectedItems[0].Tag;
            try
            {
                DatabaseManager.MarkCompleted(id);
                ActivityLog.Add("Task", $"Task #{id} marked as completed");
                LoadTasks();
                SetStatus("Task marked as done! ✅", GreenDone);
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", Color.Tomato);
            }
        }

        private void OnDeleteTask(object sender, EventArgs e)
        {
            if (_taskList.SelectedItems.Count == 0)
            { SetStatus("Select a task first.", Color.Orange); return; }

            string title = _taskList.SelectedItems[0].SubItems[1].Text;
            var confirm = MessageBox.Show($"Delete task '{title}'?", "Confirm Delete",
                                           MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            int id = (int)_taskList.SelectedItems[0].Tag;
            try
            {
                DatabaseManager.DeleteTask(id);
                ActivityLog.Add("Task", $"Task #{id} '{title}' deleted");
                LoadTasks();
                SetStatus("Task deleted.", Color.Orange);
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", Color.Tomato);
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private void SetStatus(string msg, Color colour)
        {
            _statusLabel.Text = msg;
            _statusLabel.ForeColor = colour;
        }

        private Label MakeLabel(string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = TextGray,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
            };
            y += 18;
            return lbl;
        }

        private TextBox MakeTextBox(ref int y, Panel parent, int height)
        {
            var tb = new TextBox
            {
                Location = new Point(10, y),
                Size = new Size(260, height),
                BackColor = InputBg,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            y += height + 12;
            return tb;
        }

        private Button MakeActionButton(string text, Color colour, ref int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(10, y),
                Size = new Size(260, 34),
                BackColor = colour,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            y += 42;
            return btn;
        }
    }
}
