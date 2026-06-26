# 🛡️ CyberBot v3 — Cybersecurity Awareness Chatbot

> **Module:** PROG6221 — Programming 2A
> **Part:** 3 of 3 (Final POE Submission)
> **Student:** Lonke
> **Institution:** The Independent Institute of Education

---

## 📌 Project Overview

CyberBot v3 is a **Windows Forms (.NET) GUI-based cybersecurity awareness chatbot** that helps users learn about online safety through interactive conversation, tasks, and quizzes.

This is the final part of the Portfolio of Evidence (POE), building on:
- **Part 1** — Console-based chatbot with keyword detection
- **Part 2** — GUI upgrade with sentiment detection, memory, and personality modes
- **Part 3** — Full feature expansion with MySQL, NLP, Quiz, and Activity Log

---

## 🎥 Video Presentation

> 📺 YouTube (Unlisted): _[Add your YouTube link here]_

---

## ✨ Features

### ✅ Task 1 — Task Assistant with MySQL Database
- Add cybersecurity tasks (e.g. "Enable two-factor authentication")
- Each task has a **title**, **description**, and optional **reminder**
- Tasks are stored in a **MySQL database** (full CRUD operations)
- View, complete, and delete tasks via the **Tasks tab** or chat
- NLP supports natural phrases like:
  - *"Add a task to update my password"*
  - *"Remind me in 3 days"*
  - *"Show my tasks"*

### ✅ Task 2 — Cybersecurity Mini-Game (Quiz)
- **12 questions** — 8 multiple choice + 4 true/false
- Topics: phishing, passwords, 2FA, VPN, malware, social engineering, HTTPS, firewalls, encryption
- Immediate feedback with explanation after each answer
- Final score with personalised feedback
- Accessible via **Quiz tab** or by typing *"start quiz"* in chat

### ✅ Task 3 — NLP Simulation
- Detects **10+ user intents** from varied phrases
- Uses keyword matching and regex for natural language understanding
- Extracts task titles and reminder timeframes from sentences
- Fallback: *"I didn't quite understand that. Could you rephrase?"*

### ✅ Task 4 — Activity Log
- Every significant action is **timestamped and logged**
- Categories: Task | Reminder | Quiz | NLP | Chat
- View last 10 actions via chat or full history in the **Activity Log tab**
- Colour-coded by category

### ✅ Parts 1 & 2 Integration
- All original features still work:
  - Cybersecurity topic responses (phishing, passwords, malware, VPN, 2FA, etc.)
  - Sentiment detection (Worried, Frustrated, Curious, Happy)
  - User memory (name + favourite topic)
  - Personality modes (Friendly, Professional, Futuristic AI, Casual)
  - Quick topic buttons in sidebar

---

## 🗂️ Project Structure

```
CyberBot_Part3/
├── Program.cs                  ← Entry point
├── Forms/
│   ├── MainForm.cs             ← Main window with TabControl + sidebar
│   ├── TasksForm.cs            ← Task Assistant GUI tab
│   └── ActivityLogForm.cs      ← Activity Log GUI tab
├── Logic/
│   ├── ChatbotEngine.cs        ← Core chatbot logic
│   ├── NlpEngine.cs            ← Intent detection & phrase extraction
│   ├── QuizEngine.cs           ← 12-question quiz engine
│   ├── ActivityLog.cs          ← In-memory action logger
│   ├── SentimentDetector.cs    ← Sentiment analysis
│   └── UserMemory.cs           ← User name & topic memory
└── Data/
    └── DatabaseManager.cs      ← MySQL CRUD operations
```

---

## 🛠️ Setup Instructions

### Requirements
- Visual Studio 2022
- .NET Framework 4.8
- MySQL Server 8.x
- MySql.Data NuGet package

### Step 1 — Install MySQL
1. Download **MySQL Community Server** from: https://dev.mysql.com/downloads/mysql/
2. Run installer → choose **Developer Default**
3. Set a root password during setup

### Step 2 — Install MySql.Data NuGet Package
1. Open Visual Studio
2. Right-click project → **Manage NuGet Packages**
3. Search **MySql.Data** → Install

### Step 3 — Configure Database Connection
Open `Data/DatabaseManager.cs` and update:
```csharp
private const string Server   = "localhost";
private const string Database = "cyberbot_db";
private const string User     = "root";
private const string Password = "your_password_here";
```
The app **automatically creates** the database and table on first run.

### Step 4 — Run the Project
1. Clone this repository
2. Open `CyberBot_Part3.sln` in Visual Studio 2022
3. Install the MySql.Data NuGet package
4. Press **F5** to build and run

---

## 💬 Chat Commands

| Command | What it does |
|---|---|
| `add a task to enable 2FA` | Adds task to MySQL database |
| `remind me in 3 days` | Sets a reminder on the last task |
| `show my tasks` | Lists all tasks from database |
| `start quiz` | Begins the cybersecurity quiz |
| `show activity log` | Shows recent chatbot actions |
| `what have you done for me?` | Also shows activity log |
| `tell me about phishing` | Cybersecurity topic response |
| `what is 2FA` | Explains a topic |
| `how do I create a password` | Password advice |
| `I'm interested in privacy` | Saves favourite topic to memory |
| `what do you remember about me` | Recalls name and favourite topic |
| `exit` | Closes the application |

---

## 📸 Screenshots

> *(Add screenshots of your running app here)*

---

## 🔗 GitHub Releases

| Version | Description |
|---|---|
| v1.0 | Part 1 — Console chatbot with keyword detection |
| v2.0 | Part 2 — GUI upgrade with sentiment and memory |
| v3.0 | Part 3 — Full feature set: Tasks, Quiz, NLP, Log |

---

## 📚 References

- Whitman, M. & Mattord, H. *Principles of Information Security*
- MySQL Documentation: https://dev.mysql.com/doc/
- Microsoft Docs — Windows Forms: https://docs.microsoft.com/en-us/dotnet/desktop/winforms/

---

*© 2026 — PROG6221 POE Part 3 | The Independent Institute of Education*
