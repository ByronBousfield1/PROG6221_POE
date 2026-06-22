# Cybersecurity Awareness Chatbot - Part 3 / POE

A WPF desktop application that helps South African citizens stay safe online. This final part combines Parts 1, 2, and 3 into one cohesive GUI app with a task assistant (backed by a MySQL database), a quiz mini-game, NLP-style command recognition, and an activity log.

GitHub: (add your repository link here)
Video: (add your YouTube unlisted link here)

## Features

Carried over from Parts 1 and 2:
- WPF graphical interface with a dark theme
- Voice greeting (.wav) and ASCII-style branding
- Recognises 10 cybersecurity topics with varied random responses
- Conversation flow ("tell me more", "another tip")
- Memory and recall (remembers your name and interests)
- Sentiment detection (worried, frustrated, curious, confused, happy, sad)

New in Part 3:
- Task Assistant with reminders, stored in a MySQL database (add, view, complete, delete)
- Reminder parsing from natural text ("remind me in 7 days", "tomorrow", "next week") plus a date picker
- Due-reminder notifications that pop up in the chat
- Cybersecurity quiz with 14 mixed multiple-choice and true/false questions, immediate feedback, and a final score
- NLP simulation that recognises commands worded in different ways
- Activity log that records every action with timestamps, viewable in chat or its own tab

## Tabs

- Chat: talk to the bot. Everything works here, including task and quiz commands.
- Tasks: add tasks with a form, see the list, mark complete, or delete.
- Quiz: play the cybersecurity quiz with one question at a time.
- Activity Log: see the last 5-10 actions, with a "Show all" option.

## Project Structure

```
CyberAwarenessBot/
  App.xaml / App.xaml.cs
  AssemblyInfo.cs
  CyberAwarenessBot.csproj
  MainWindow.xaml / MainWindow.xaml.cs
  Models/
    UserMemory.cs
    ChatMessage.cs
    CyberTask.cs
    QuizQuestion.cs
    ActivityLogEntry.cs
  Services/
    ChatbotEngine.cs       (orchestrates everything, uses delegates)
    KeywordResponder.cs    (keyword recognition, dictionaries)
    SentimentDetector.cs   (sentiment, delegates)
    NlpProcessor.cs        (intent recognition, regex, string manipulation)
    TaskService.cs         (MySQL CRUD with in-memory fallback)
    DatabaseHelper.cs       (MySQL connection and auto table creation)
    QuizService.cs         (14 questions, scoring)
    ActivityLogger.cs      (action log)
    AudioService.cs        (voice greeting)
  Database/
    schema.sql             (MySQL schema, also created automatically)
  Assets/
    greeting.wav
```

## Prerequisites

- Windows 10 or 11
- Visual Studio 2022 with the .NET Desktop Development workload
- .NET 8 SDK
- MySQL running locally. The easiest option for students is XAMPP, which includes MySQL/MariaDB and phpMyAdmin.

## Database Setup (XAMPP - easiest)

1. Download and install XAMPP from apachefriends.org.
2. Open the XAMPP Control Panel and click Start next to MySQL.
3. That is all. When the app starts it automatically creates the `cyberbot` database and the `tasks` table.

If MySQL is not running, the app still opens and everything works, but tasks are stored only for that session and the footer shows "Database: not connected". Start MySQL and restart the app to enable saving.

### If you use MySQL Community Server instead of XAMPP

MySQL Community Server usually sets a root password during installation. Open `Services/DatabaseHelper.cs` and put your password in the `DbPassword` constant near the top of the file:

```csharp
private const string DbPassword = "your_password_here";
```

## How to Run

1. Open `CyberAwarenessBot.csproj` in Visual Studio 2022.
2. Visual Studio restores the MySql.Data NuGet package automatically on first build. If it does not, right-click the project and choose Restore NuGet Packages.
3. Make sure MySQL is running (XAMPP MySQL started).
4. Press F5.

## Things to Try

In the Chat tab:
- Type your name when asked
- "Tell me about phishing" then "tell me more"
- "I'm worried about online scams"
- "add task enable two-factor authentication, remind me in 7 days"
- "add task review privacy settings" then answer the reminder question
- "show my tasks"
- "complete task 1"
- "delete task 2"
- "start the quiz"
- "what have you done for me?"

In the Tasks tab: add a task with the form, then complete or delete it. Switch to MySQL/phpMyAdmin to see the rows in the `cyberbot.tasks` table.

In the Quiz tab: answer the questions and read the feedback after each one.

## How the NLP Works

`NlpProcessor` normalises the input and matches it against keyword sets for each intent (add task, show tasks, complete, delete, quiz, activity log). It uses `string.Contains` plus regular expressions to pull out task numbers and reminder timeframes, so the same command works in several phrasings, for example "add task X", "remind me to X", and "I need to X" all create a task.

## GitHub Submission Checklist

- Public repository with the complete project folder and this README
- At least six commits with meaningful messages
- At least three tagged releases with version notes
- GitHub link submitted on ARC
- YouTube unlisted video link with a voice-over explaining the code, logic, and techniques

## Reference

Pieterse, H. 2021. The Cyber Threat Landscape in South Africa: A 10-Year Review. The African Journal of Information and Communication, 28(28). doi: https://doi.org/10.23962/10539/32213.
