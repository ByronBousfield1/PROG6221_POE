using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CyberAwarenessBot.Models;
using CyberAwarenessBot.Services;

namespace CyberAwarenessBot
{
    public partial class MainWindow : Window
    {
        private readonly UserMemory memory = new UserMemory();
        private readonly ChatbotEngine engine;
        private bool hasUserName;

        public MainWindow()
        {
            InitializeComponent();
            engine = new ChatbotEngine(memory);

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AsciiArtBlock.Text =
                "  ____      _               ____        _   \n" +
                " / ___|   _| |__   ___ _ _ | __ )  ___ | |_ \n" +
                "| |  | | | | '_ \\ / _ \\ '__|  _ \\ / _ \\| __|\n" +
                "| |__| |_| | |_) |  __/ |  | |_) | (_) | |_ \n" +
                " \\____\\__, |_.__/ \\___|_|  |____/ \\___/ \\__|\n" +
                "      |___/                                  ";

            TopicChipsList.ItemsSource = engine.AllTopics.ToList();

            string audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "greeting.wav");
            AudioService.PlayGreeting("Assets/greeting.wav"); await ShowBotMessageAsync("Hello! Welcome to the Cybersecurity Awareness Bot. I'm here to help you stay safe online.");
            await Task.Delay(300);
            await ShowBotMessageAsync("Before we begin, may I know your name?");

            InputBox.Focus();
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string input = InputBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                await ShowBotMessageAsync("[System] Invalid input. Please type something before sending.", category: "error");
                return;
            }

            InputBox.Clear();

            if (!hasUserName)
            {
                memory.UserName = SanitizeName(input);
                hasUserName = true;
                AddUserMessage(input);
                UpdateProfilePanel();

                await ShowBotMessageAsync($"Nice to meet you, {memory.UserName}. Let's improve your cybersecurity knowledge together.");
                await Task.Delay(200);
                await ShowBotMessageAsync(
                    "You can ask me about topics like phishing, passwords, scams, privacy, malware, or safe browsing. " +
                    "Try saying 'I'm interested in privacy' so I can remember it, or click one of the quick topics on the left.");
                return;
            }

            AddUserMessage(input);

            SendButton.IsEnabled = false;
            StatusText.Text = "Thinking...";

            try
            {
                string reply = engine.GetResponse(input);
                await Task.Delay(350);
                await ShowBotMessageAsync(reply, category: ClassifyCategory(reply));

                UpdateProfilePanel();

                string lower = input.ToLower();
                if (lower.Contains("exit") || lower.Contains("quit") || lower.Contains("goodbye"))
                {
                    await Task.Delay(1500);
                    Close();
                }
            }
            catch (Exception ex)
            {
                await ShowBotMessageAsync($"[System] Something went wrong: {ex.Message}", category: "error");
            }
            finally
            {
                SendButton.IsEnabled = true;
                StatusText.Text = "Online - ready";
                InputBox.Focus();
            }
        }

        private void TopicChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string topic)
            {
                InputBox.Text = $"Tell me about {topic}";
                SendButton_Click(sender, e);
            }
        }

        private void AddUserMessage(string message)
        {
            var (bubble, _) = BuildBubble(message, isUser: true, category: "user");
            ChatPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private async Task ShowBotMessageAsync(string message, string category = "info")
        {
            var (bubble, bodyText) = BuildBubble(string.Empty, isUser: false, category: category);
            ChatPanel.Children.Add(bubble);
            ScrollToBottom();

            foreach (char c in message)
            {
                bodyText.Text += c;
                if (c == '\n' || c == '.' || c == '!')
                    await Task.Delay(15);
                else
                    await Task.Delay(5);
            }
            ScrollToBottom();
        }

        private (Border bubble, TextBlock bodyText) BuildBubble(string message, bool isUser, string category)
        {
            Brush bg, fg, senderColor;
            string senderLabel;

            if (isUser)
            {
                bg = (Brush)Application.Current.Resources["AccentGreen"];
                fg = new SolidColorBrush(Color.FromRgb(13, 17, 23));
                senderColor = new SolidColorBrush(Color.FromRgb(13, 17, 23));
                senderLabel = memory.UserName;
            }
            else
            {
                bg = category switch
                {
                    "error" => new SolidColorBrush(Color.FromRgb(60, 30, 30)),
                    _       => (Brush)Application.Current.Resources["BackgroundCard"]
                };
                fg = (Brush)Application.Current.Resources["TextPrimary"];
                senderColor = (Brush)Application.Current.Resources["AccentCyan"];
                senderLabel = "CyberBot";
            }

            var senderText = new TextBlock
            {
                Text = senderLabel,
                Foreground = senderColor,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var bodyText = new TextBlock
            {
                Text = message,
                Foreground = fg,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };

            var stack = new StackPanel();
            stack.Children.Add(senderText);
            stack.Children.Add(bodyText);

            var border = new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(
                    isUser ? 80 : 0, 6,
                    isUser ? 0 : 80, 6),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 620,
                Child = stack
            };

            return (border, bodyText);
        }

        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        private void UpdateProfilePanel()
        {
            ProfileNameText.Text = string.IsNullOrEmpty(memory.UserName) ? "(not set)" : memory.UserName;
            ProfileTopicText.Text = memory.FavouriteTopic ?? memory.LastTopic ?? "-";
            ProfileSentimentText.Text = memory.LastSentiment ?? "neutral";
        }

        private string SanitizeName(string raw)
        {
            var cleaned = new string(raw.Where(c => char.IsLetter(c) || c == ' ' || c == '-').ToArray()).Trim();
            if (string.IsNullOrEmpty(cleaned)) return "Friend";

            var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = char.ToUpper(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1).ToLower() : "");
            }
            return string.Join(" ", parts);
        }

        private string ClassifyCategory(string reply)
        {
            if (reply.StartsWith("[System]")) return "error";
            if (reply.Contains("tip", StringComparison.OrdinalIgnoreCase)) return "tip";
            return "info";
        }
    }
}
