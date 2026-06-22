# Cybersecurity Awareness Chatbot (PROG6221 POE)

My POE project for PROG6221. It's a WPF app in C# that teaches people about cybersecurity. It started as a console app in Part 1, got a GUI in Part 2, and this final version adds a task assistant with a MySQL database, a quiz, an activity log, and better command handling.

GitHub: https://github.com/ByronBousfield1/PROG6221_POE
Video: 

## What it does

- Chat interface where you can ask about cybersecurity topics like phishing, passwords, scams, privacy and more
- Remembers your name and what you're interested in
- Picks up on your mood (worried, curious, frustrated etc.) and adjusts what it says
- Task assistant where you can add tasks with reminders, mark them done, or delete them. Tasks are saved in a MySQL database
- A quiz with 14 questions (multiple choice and true/false) that gives feedback and a score
- Understands commands worded in different ways, e.g "add task", "remind me to", "I need to" all add a task
- Keeps a log of everything it does that you can view any time

## Tabs

- Chat - talk to the bot, all commands work here
- Tasks - add and manage your tasks
- Quiz - take the quiz
- Activity Log - see what the bot has done

## Running it

You need:
- Windows
- Visual Studio 2022 with the .NET desktop workload
- .NET 8
- MySQL running (I used XAMPP)

Steps:
1. Start MySQL in XAMPP (open the control panel and hit Start next to MySQL)
2. Open CyberAwarenessBot.csproj in Visual Studio
3. Let it restore the MySql.Data package on the first build
4. Press F5

The database and table get created automatically the first time it runs. If MySQL isn't on, the app still works but tasks won't save and it shows "not connected" at the bottom.

If your MySQL root account has a password, open Services/DatabaseHelper.cs and put it in the DbPassword line near the top.

## Things to try

- Type your name when it asks
- "tell me about phishing" then "tell me more"
- "add task enable two factor authentication, remind me in 7 days"
- "show my tasks"
- "complete task 1"
- "start the quiz"
- "what have you done for me"

## Notes on the code

The code is split into Models (the data classes), Services (the actual logic) and the MainWindow for the GUI. The NlpProcessor handles working out what the user wants using string matching and a bit of regex for things like dates. TaskService does the database stuff. KeywordResponder and SentimentDetector are from Part 2.

## Reference

Pieterse, H. 2021. The Cyber Threat Landscape in South Africa: A 10-Year Review. The African Journal of Information and Communication, 28(28).