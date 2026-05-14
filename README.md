# Cybersecurity Awareness Chatbot - Part 2

A WPF desktop application that extends the Part 1 console chatbot with a GUI, keyword recognition, random responses, conversation flow, memory and recall, and sentiment detection.

## Features

- WPF graphical user interface with dark theme
- Voice greeting (.wav) and ASCII art carried over from Part 1
- Recognises 10 cybersecurity topics (password, phishing, scam, privacy, safe browsing, malware, 2FA, public wifi, social media, identity theft)
- Multiple random responses per topic
- Handles "tell me more" / "another tip" / "explain more" to continue the same topic
- Remembers the user's name and what they say they are interested in
- Detects worried, frustrated, curious, confused, happy, and sad sentiment
- Graceful fallback for unrecognised input
- South African context (FNB, SARS, POPIA, SABRIC, SAPS)

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
  Services/
    ChatbotEngine.cs
    KeywordResponder.cs
    SentimentDetector.cs
    AudioService.cs
  Assets/
    greeting.wav
```

## Prerequisites

- Windows 10 or 11
- Visual Studio 2022 with the .NET Desktop Development workload installed
- .NET 8 SDK (installed via the Visual Studio installer)

## How to Run

1. Open `CyberAwarenessBot.csproj` in Visual Studio 2022
2. Press F5 or click the green Start button
3. The window opens, the voice greeting plays, and the bot asks for your name

## Things to Try

- Type your name
- "How are you?"
- "What is your purpose?"
- "Tell me about phishing"
- "Tell me more"
- "Another tip"
- "I'm worried about online scams"
- "I'm interested in privacy"
- "Tell me about passwords"
- "What do you remember about me?"
- Click any of the Quick Topics in the sidebar
- "Bye"
