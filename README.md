# Cybersecurity Awareness Bot — Part 3 / POE

## Student Details

Name: Karabo Matsetela

Student Number: ST10486208

A **WPF (Windows Presentation Foundation)** cybersecurity chatbot with task management, quiz mini-game, NLP intent detection, and activity logging. Parts 1–2 (keyword recognition, sentiment, memory, conversation flow) are integrated with Part 3 features in a single cohesive GUI.

## Overview

The bot helps users learn about password safety, phishing, malware, scams, privacy, safe browsing, and more. It also lets users manage cybersecurity tasks with MySQL-backed reminders, take an interactive quiz, and review a timestamped activity log—all from one application.

## POE Rubric Alignment (Maximum Standard)

| Criterion | Implementation |
|-----------|----------------|
| **GUI (WPF)** | Tabbed interface: Chat, Tasks, Quiz, Activity Log |
| **Task Assistant** | Add/view/complete/delete tasks; optional reminders via DatePicker or natural language |
| **MySQL database** | `TaskDatabaseService` — CRUD with auto table creation and error handling |
| **Cybersecurity Quiz** | 12 questions (MC + true/false), immediate feedback, score-based closing message |
| **NLP simulation** | `NlpIntentRecognizer` — Regex patterns for tasks, reminders, quiz, activity log |
| **Activity log** | `ActivityLogService` — timestamps, last 10 items in chat, GUI list with Show More |
| **Parts 1–2 integration** | Keyword recognition, sentiment, memory, delegates, random responses |
| **Error handling** | Empty input, DB offline messages, try/catch — no crashes |

## Features

### Task Assistant (GUI + MySQL)
- Add tasks with **title**, **description**, and **optional reminder date**
- View all tasks in a DataGrid
- **Mark complete** or **delete** — changes sync to MySQL
- Chat commands: `Add a task to enable 2FA`, `Remind me to update my password tomorrow`

### Cybersecurity Mini-Game (Quiz)
- **12 questions** covering phishing, passwords, 2FA, HTTPS, ransomware, social engineering
- Multiple-choice and true/false formats
- **Immediate feedback** with explanations after each answer
- Final score with tiered messages (e.g. "Great job! You're a cybersecurity pro!")

### NLP Simulation
- **Regex-based** intent detection (not just exact-match commands)
- Recognizes variations like:
  - "Can you remind me to update my password?" → reminder task
  - "Add a reminder to check my privacy settings." → reminder task
  - "What have you done for me?" → activity log summary
- Fallback: "I didn't quite understand that. Could you rephrase?"

### Activity Log
- Logs tasks, reminders, quiz starts/completions, NLP actions, session events
- Each entry has a **timestamp**
- Chat: shows last **10** actions; "show more" expands the list
- **Activity Log** tab: paginated view with **Show More** button

## Project Structure

```
CybersecurityAwarenessBot/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / .cs           # Tabbed GUI
├── appsettings.json                # MySQL connection string
├── Assets/Greetings.wav
├── Database/schema.sql             # Optional manual setup script
├── Delegates/ResponseDelegates.cs
├── Models/
│   ├── TaskItem.cs
│   ├── QuizQuestion.cs
│   ├── ActivityLogEntry.cs
│   ├── NlpIntent.cs
│   └── ... (Part 2 models)
└── Services/
    ├── ChatbotService.cs           # Unified conversation engine
    ├── TaskDatabaseService.cs      # MySQL CRUD
    ├── QuizService.cs
    ├── NlpIntentRecognizer.cs
    ├── ActivityLogService.cs
    └── ... (Part 2 services)
```

## Requirements

- **.NET 10 SDK** (`net10.0-windows`)
- **Windows** (WPF)
- **MySQL Server** (local or remote) for task persistence

## Setup

### 1. Clone and restore

```bash
git clone https://github.com/YOUR_USERNAME/CybersecurityAwarenessBot.git
cd CybersecurityAwarenessBot
dotnet restore
```

### 2. Configure MySQL

1. Install [MySQL Server](https://dev.mysql.com/downloads/mysql/) (or use XAMPP/WAMP).
2. Create the database (optional — the app auto-creates the table):

```sql
CREATE DATABASE IF NOT EXISTS cybersecurity_bot;
```

3. Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "TasksDb": "Server=localhost;Port=3306;Database=cybersecurity_bot;Uid=root;Pwd=YOUR_PASSWORD;"
  }
}
```

4. Or run `Database/schema.sql` in MySQL Workbench for manual setup.

### 3. Run the application

```bash
dotnet run
```

Optional: add `Assets/Greetings.wav` for the voice greeting (the app runs without it).

## Example Conversations

**NLP — Reminder:**
```
User: Remind me to update my password tomorrow.
Bot: Reminder set for 'Update my password' on 15 Jun 2026.
```

**NLP — Add task with follow-up:**
```
User: Add a task to enable two-factor authentication.
Bot: Task added: 'Enable two-factor authentication.' Would you like to set a reminder for this task?
User: Yes, in 3 days.
Bot: Got it! I'll remind you about 'Enable two-factor authentication' on 17 Jun 2026.
```

**Activity log:**
```
User: What have you done for me?
Bot: Here's a summary of recent actions:
1. Task added: 'Enable two-factor authentication'.
2. Reminder set for task 'Enable two-factor authentication' on 17 Jun 2026.
3. Quiz started via chat — 5 questions.
```

**Quiz (Chat tab):**
```
User: Start the quiz
Bot: Question 1/5: What is phishing?
1. A type of malware
2. A social engineering attack using fake messages
...
User: 2
Bot: Correct! Phishing tricks users into revealing sensitive information...
```

## GUI Tour

| Tab | Purpose |
|-----|---------|
| **Chat** | Main bot conversation, NLP commands, in-chat quiz |
| **Tasks** | Add/edit tasks, set reminders, mark complete, delete |
| **Quiz** | Standalone 5-question cybersecurity quiz with buttons |
| **Activity Log** | Timestamped history with Show More pagination |

## Repository & Submission

### GitHub
- Minimum **6 meaningful commits** (feature-focused messages)
- Minimum **3 tagged releases** with version notes (e.g. `v1.0-part1`, `v2.0-part2`, `v3.0-poe`)

```bash
git tag -a v3.0-poe -m "Part 3 POE: tasks, quiz, NLP, activity log"
git push origin v3.0-poe
```

Create releases on GitHub: **Releases → Draft a new release** → select tag → add notes.

### ARC Submission Checklist
- [ ] Complete project on GitHub (source, README, `appsettings.json` template without real passwords)
- [ ] Minimum 6 commits with meaningful messages
- [ ] At least 3 releases/tags with notes
- [ ] **YouTube unlisted video** — voice-over explaining structure, logic, and all features
- [ ] Submit GitHub + video links on ARC

## Video Presentation Outline

1. Introduction and purpose of the cybersecurity bot  
2. GUI tour — all four tabs  
3. **Task Assistant** — MySQL setup, CRUD demo, reminder flow  
4. **Quiz** — questions, feedback, scoring  
5. **NLP** — regex intent detection, varied phrasings  
6. **Activity log** — logging triggers and Show More  
7. Code walkthrough: `ChatbotService`, `NlpIntentRecognizer`, `TaskDatabaseService`  
8. Parts 1–2 integration: sentiment, memory, delegates  

## Video Presentation

_Add your YouTube unlisted link here after recording._

## References

- [W3Schools - Cyber Security Tutorial](https://www.w3schools.com/cybersecurity/)
- [Microsoft Learn - WPF Documentation](https://learn.microsoft.com/dotnet/desktop/wpf/)
- [MySqlConnector Documentation](https://mysqlconnector.net/)
- [Microsoft Learn - .NET Documentation](https://learn.microsoft.com/dotnet/)
