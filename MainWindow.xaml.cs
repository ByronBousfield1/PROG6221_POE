using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CyberAwarenessBot.Models;
using CyberAwarenessBot.Services;

namespace CyberAwarenessBot
{
    public partial class MainWindow : Window
    {
        private readonly UserMemory memory = new UserMemory();
        private readonly TaskService taskService = new TaskService();
        private readonly QuizService quizService = new QuizService();
        private readonly ActivityLogger logger = new ActivityLogger();
        private readonly ChatbotEngine engine;

        private bool hasUserName;
        private bool quizAnswered;
        private bool logShowingAll;
        private readonly System.Collections.Generic.HashSet<int> notifiedReminders = new();

        public MainWindow()
        {
            InitializeComponent();
            engine = new ChatbotEngine(memory, taskService, quizService, logger);
            logger.Changed += () => Dispatcher.Invoke(RefreshLog);
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TopicChipsList.ItemsSource = engine.AllTopics.ToList();

            // Database status in the footer.
            if (taskService.UsingDatabase)
            {
                DbStatusText.Text = "Database: connected (MySQL)";
                DbStatusText.Foreground = (Brush)Application.Current.Resources["AccentGreen"];
            }
            else
            {
                DbStatusText.Text = "Database: not connected - using temporary storage";
                DbStatusText.Foreground = (Brush)Application.Current.Resources["AccentYellow"];
            }

            string audioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "greeting.wav");
            AudioService.PlayGreeting(audioPath);

            RefreshTasks();
            RefreshLog();
            StartReminderTimer();

            await ShowBotMessageAsync("Hello! Welcome to the Cybersecurity Awareness Bot. I'm here to help you stay safe online.");
            await Task.Delay(250);
            await ShowBotMessageAsync("Before we begin, may I know your name?");

            InputBox.Focus();
        }

        // Reminder notifications

        private void StartReminderTimer()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            timer.Tick += (s, e) => CheckDueReminders();
            timer.Start();
        }

        private async void CheckDueReminders()
        {
            foreach (var task in taskService.GetTasks())
            {
                if (task.IsReminderDue && !notifiedReminders.Contains(task.Id))
                {
                    notifiedReminders.Add(task.Id);
                    logger.Log($"Reminder due: '{task.Title}'");
                    await ShowBotMessageAsync($"Reminder: '{task.Title}' is due now.", category: "tip");
                }
            }
        }

        // Chat handling

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
                    "You can ask about topics like phishing or passwords, manage tasks (try 'add task enable 2FA, remind me in 7 days'), " +
                    "take the quiz, or ask 'what have you done for me?' to see the activity log.");
                return;
            }

            AddUserMessage(input);
            SendButton.IsEnabled = false;
            StatusText.Text = "Thinking...";

            try
            {
                string reply = engine.GetResponse(input);
                await Task.Delay(300);
                await ShowBotMessageAsync(reply, category: ClassifyCategory(reply));

                UpdateProfilePanel();
                RefreshTasks();

                // React to NLP intent for tab switching.
                if (engine.LastIntent == Intent.StartQuiz)
                {
                    MainTabs.SelectedIndex = 2;
                    StartQuiz();
                }
                else if (engine.LastIntent == Intent.ShowTasks)
                {
                    MainTabs.SelectedIndex = 1;
                }
                else if (engine.LastIntent == Intent.ShowLog)
                {
                    MainTabs.SelectedIndex = 3;
                }

                string lower = input.ToLower();
                if (lower.Contains("exit") || lower.Contains("quit") || lower.Contains("goodbye"))
                {
                    await Task.Delay(1400);
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
            ChatScrollViewer.ScrollToEnd();
        }

        private async Task ShowBotMessageAsync(string message, string category = "info")
        {
            var (bubble, bodyText) = BuildBubble(string.Empty, isUser: false, category: category);
            ChatPanel.Children.Add(bubble);
            ChatScrollViewer.ScrollToEnd();

            foreach (char c in message)
            {
                bodyText.Text += c;
                if (c == '\n' || c == '.' || c == '!') await Task.Delay(12);
                else await Task.Delay(4);
            }
            ChatScrollViewer.ScrollToEnd();
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
                    _ => (Brush)Application.Current.Resources["BackgroundCard"]
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
                Margin = new Thickness(isUser ? 80 : 0, 6, isUser ? 0 : 80, 6),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 640,
                Child = stack
            };

            return (border, bodyText);
        }

        // Task management

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            string title = TaskTitleBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Please enter a task title.", "Missing title", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string desc = TaskDescBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(desc)) desc = $"Cybersecurity task: {title}.";

            DateTime? reminder = null;
            if (TaskReminderPicker.SelectedDate.HasValue)
            {
                reminder = TaskReminderPicker.SelectedDate.Value.AddHours(9);
            }
            else if (!string.IsNullOrWhiteSpace(TaskReminderTextBox.Text))
            {
                reminder = new NlpProcessor().ParseReminder(TaskReminderTextBox.Text.Trim());
            }

            var task = new CyberTask { Title = title, Description = desc, ReminderDate = reminder };
            taskService.AddTask(task);
            logger.Log($"Task added: '{title}'" + (reminder.HasValue ? $" (reminder {reminder.Value:dd MMM HH:mm})" : string.Empty));

            TaskTitleBox.Clear();
            TaskDescBox.Clear();
            TaskReminderTextBox.Clear();
            TaskReminderPicker.SelectedDate = null;

            RefreshTasks();
            StatusText.Text = "Task added";
        }

        private void RefreshTasksButton_Click(object sender, RoutedEventArgs e) => RefreshTasks();

        private void RefreshTasks()
        {
            TaskListPanel.Children.Clear();
            var tasks = taskService.GetTasks();

            if (tasks.Count == 0)
            {
                TaskListPanel.Children.Add(new TextBlock
                {
                    Text = "No tasks yet. Add one on the left, or tell the bot 'add task enable 2FA'.",
                    Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                    FontSize = 13,
                    Margin = new Thickness(4, 8, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            foreach (var task in tasks)
            {
                TaskListPanel.Children.Add(BuildTaskCard(task));
            }
        }

        private Border BuildTaskCard(CyberTask task)
        {
            var titleText = new TextBlock
            {
                Text = $"{task.Id}.  {task.Title}",
                Foreground = (Brush)Application.Current.Resources["TextPrimary"],
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null
            };

            var descText = new TextBlock
            {
                Text = task.Description,
                Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var metaText = new TextBlock
            {
                Text = $"Status: {task.StatusDisplay}    Reminder: {task.ReminderDisplay}",
                Foreground = task.IsReminderDue
                    ? (Brush)Application.Current.Resources["AccentRed"]
                    : (Brush)Application.Current.Resources["AccentCyan"],
                FontSize = 11,
                Margin = new Thickness(0, 6, 0, 0)
            };

            var info = new StackPanel();
            info.Children.Add(titleText);
            info.Children.Add(descText);
            info.Children.Add(metaText);

            var completeBtn = new Button
            {
                Content = task.IsCompleted ? "Done" : "Complete",
                Style = (Style)Application.Current.Resources["CyberButtonAlt"],
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 0, 0, 6),
                IsEnabled = !task.IsCompleted,
                Tag = task.Id
            };
            completeBtn.Click += CompleteTask_Click;

            var deleteBtn = new Button
            {
                Content = "Delete",
                Style = (Style)Application.Current.Resources["CyberButtonAlt"],
                Foreground = (Brush)Application.Current.Resources["AccentRed"],
                Padding = new Thickness(10, 6, 10, 6),
                Tag = task.Id
            };
            deleteBtn.Click += DeleteTask_Click;

            var buttons = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            buttons.Children.Add(completeBtn);
            buttons.Children.Add(deleteBtn);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(info, 0);
            Grid.SetColumn(buttons, 1);
            grid.Children.Add(info);
            grid.Children.Add(buttons);

            return new Border
            {
                Background = (Brush)Application.Current.Resources["BackgroundCard"],
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 8),
                Child = grid
            };
        }

        private void CompleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var task = taskService.GetTasks().FirstOrDefault(t => t.Id == id);
                taskService.MarkComplete(id);
                if (task != null) logger.Log($"Task completed: '{task.Title}'");
                RefreshTasks();
            }
        }

        private void DeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var task = taskService.GetTasks().FirstOrDefault(t => t.Id == id);
                taskService.DeleteTask(id);
                if (task != null) logger.Log($"Task deleted: '{task.Title}'");
                RefreshTasks();
            }
        }

        // Quiz

        private void StartQuizButton_Click(object sender, RoutedEventArgs e) => StartQuiz();

        private void StartQuiz()
        {
            quizService.Restart();
            QuizStartPanel.Visibility = Visibility.Collapsed;
            QuizResultPanel.Visibility = Visibility.Collapsed;
            QuizQuestionPanel.Visibility = Visibility.Visible;
            ShowCurrentQuestion();
        }

        private void ShowCurrentQuestion()
        {
            quizAnswered = false;
            var q = quizService.Current;

            QuizProgressText.Text = $"Question {quizService.CurrentNumber} of {quizService.Total}";
            QuizScoreText.Text = $"Score: {quizService.Score}";
            QuizQuestionText.Text = q.Question;
            QuizFeedbackBorder.Visibility = Visibility.Collapsed;
            QuizNextButton.Visibility = Visibility.Collapsed;

            QuizOptionsPanel.Children.Clear();
            for (int i = 0; i < q.Options.Count; i++)
            {
                var btn = new Button
                {
                    Content = q.Options[i],
                    Style = (Style)Application.Current.Resources["CyberButtonAlt"],
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(14, 10, 14, 10),
                    Margin = new Thickness(0, 0, 0, 8),
                    Tag = i
                };
                btn.Click += QuizOption_Click;
                QuizOptionsPanel.Children.Add(btn);
            }
        }

        private void QuizOption_Click(object sender, RoutedEventArgs e)
        {
            if (quizAnswered) return;
            if (sender is not Button btn || btn.Tag is not int chosen) return;

            quizAnswered = true;
            var q = quizService.Current;
            bool correct = quizService.Answer(chosen);

            // Highlight options.
            foreach (var child in QuizOptionsPanel.Children)
            {
                if (child is Button b && b.Tag is int idx)
                {
                    b.IsEnabled = false;
                    if (idx == q.CorrectIndex)
                        b.Foreground = (Brush)Application.Current.Resources["AccentGreen"];
                    else if (idx == chosen)
                        b.Foreground = (Brush)Application.Current.Resources["AccentRed"];
                }
            }

            QuizScoreText.Text = $"Score: {quizService.Score}";
            QuizFeedbackBorder.Background = correct
                ? new SolidColorBrush(Color.FromRgb(20, 50, 35))
                : new SolidColorBrush(Color.FromRgb(55, 28, 28));
            QuizFeedbackTitle.Text = correct ? "Correct" : "Not quite";
            QuizFeedbackTitle.Foreground = correct
                ? (Brush)Application.Current.Resources["AccentGreen"]
                : (Brush)Application.Current.Resources["AccentRed"];
            QuizFeedbackText.Text = q.Explanation;
            QuizFeedbackBorder.Visibility = Visibility.Visible;

            QuizNextButton.Content = (quizService.CurrentNumber >= quizService.Total) ? "SEE RESULTS" : "NEXT";
            QuizNextButton.Visibility = Visibility.Visible;
        }

        private void QuizNextButton_Click(object sender, RoutedEventArgs e)
        {
            quizService.Next();
            if (quizService.IsFinished)
            {
                logger.Log($"Quiz completed - scored {quizService.Score}/{quizService.Total}");
                QuizQuestionPanel.Visibility = Visibility.Collapsed;
                QuizResultPanel.Visibility = Visibility.Visible;
                QuizResultText.Text = quizService.FinalFeedback();
            }
            else
            {
                ShowCurrentQuestion();
            }
        }

        // Activity log

        private void RefreshLogButton_Click(object sender, RoutedEventArgs e)
        {
            logShowingAll = false;
            RefreshLog();
        }

        private void LogShowMoreButton_Click(object sender, RoutedEventArgs e)
        {
            logShowingAll = !logShowingAll;
            LogShowMoreButton.Content = logShowingAll ? "Show recent only" : "Show all";
            RefreshLog();
        }

        private void RefreshLog()
        {
            var entries = logShowingAll ? logger.GetAll() : logger.GetRecent(10);
            LogListPanel.ItemsSource = entries;

            int total = logger.Count;
            LogCountText.Text = logShowingAll
                ? $"Showing all {total} actions"
                : $"Showing last {Math.Min(10, total)} of {total} actions";
        }

        // Helpers

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
            return "info";
        }
    }
}
