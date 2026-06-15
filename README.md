# Quiz Application — Setup & Deployment Guide

## Overview
A reusable Windows Forms quiz application that:
- Accepts participant name entry
- Displays multiple-choice questions one at a time
- Shows score + wrong-answer review on completion
- Writes results to a shared Excel file

---

## Prerequisites
- **Windows OS** (Windows 10 or later)
- **.NET 6.0 Desktop Runtime** — Download from:
  https://dotnet.microsoft.com/en-us/download/dotnet/6.0
  (Select "Run desktop apps → .NET Desktop Runtime 6.0.x")
- **Visual Studio 2022** (to build from source)
  OR pre-built `.exe` (if distributed)

---

## Project Structure

```
QuizApp/
├── QuizApp.csproj          ← Project file (build config + NuGet packages)
├── Program.cs              ← Application entry point
├── Question.cs             ← Question data model
├── NameEntryForm.cs        ← Screen 1: Name entry
├── QuizForm.cs             ← Screen 2: Question display
├── ResultForm.cs           ← Screen 3: Score + review
├── ExcelScoreWriter.cs     ← Excel logging utility
├── App.config              ← ⚙ CONFIGURATION FILE (edit this!)
└── questions.json          ← 📝 QUESTIONS FILE (edit this monthly!)
```

---

## Configuration (App.config)

Before building/deploying, open `App.config` and update these values:

```xml
<appSettings>
  <!-- Path to the shared Excel score file -->
  <add key="ScoreFilePath" value="\\YourServer\SharedFolder\QuizScores.xlsx" />

  <!-- Title shown in the app header -->
  <add key="QuizTitle" value="CATS Team Monthly Quiz" />

  <!-- Path to questions JSON (relative to .exe or absolute) -->
  <add key="QuestionsFilePath" value="questions.json" />
</appSettings>
```

**ScoreFilePath examples:**
- UNC path (recommended): `\\ServerName\Teams\QuizScores\scores.xlsx`
- Mapped drive: `Z:\QuizData\QuizScores.xlsx`
- Local (for testing): `C:\QuizApp\scores.xlsx`

> ⚠ Ensure all quiz participants have **Read + Write** access to the shared folder.

---

## How to Update Questions (Monthly)

Edit the `questions.json` file in any text editor (Notepad, VS Code):

```json
[
  {
    "QuestionText": "Your question here?",
    "Options": [
      "Option A text",
      "Option B text",
      "Option C text",
      "Option D text"
    ],
    "CorrectAnswerIndex": 1
  }
]
```

- **CorrectAnswerIndex**: 0 = Option A, 1 = Option B, 2 = Option C, 3 = Option D
- You can add as many questions as needed — the app handles any count.
- After editing `questions.json`, just replace the file in the deployment folder. **No rebuild needed.**

---

## Building the Application

1. Open `QuizApp.csproj` in **Visual Studio 2022**
2. Restore NuGet packages (auto on first open, or: `dotnet restore`)
3. Set configuration to **Release**
4. Build → **Publish** for a self-contained `.exe`

### Publish as Single .exe (recommended for distribution)

In Visual Studio:
- Right-click Project → Publish
- Target: Folder
- Deployment mode: Self-contained
- Target runtime: win-x64
- Produce single file: ✔

Or via terminal:
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output will be in: `bin\Release\net6.0-windows\win-x64\publish\`

---

## Deployment Checklist

- [ ] Update `App.config` with correct shared drive path and quiz title
- [ ] Place `questions.json` in same folder as the `.exe`
- [ ] Verify shared drive folder exists and participants have write access
- [ ] Test with one user before rolling out
- [ ] Distribute the `.exe` + `questions.json` (or publish as single `.exe`)

---

## Excel Score File

The app **auto-creates** `QuizScores.xlsx` on first run if it doesn't exist.

Columns written:
| S.No | Full Name | Score | Total | Percentage | Date & Time | Grade |

Color coding:
- 🟢 Green: ≥ 80% (Excellent)
- 🟡 Yellow: 60–79% (Good)
- 🔴 Red: < 60% (Needs Improvement)

---

## NuGet Packages Used

| Package | Purpose |
|---|---|
| EPPlus 6.2.10 | Excel read/write (non-commercial license) |
| Newtonsoft.Json 13.0.3 | JSON question file parsing |

---

## Troubleshooting

| Problem | Solution |
|---|---|
| "Questions file not found" | Ensure `questions.json` is in same folder as `.exe` |
| "Could not save score" | Check shared drive path in `App.config`; verify write permissions |
| App won't launch | Install .NET 6.0 Desktop Runtime |
| Questions not updated | Replace `questions.json` in the deployment folder and restart app |
