using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CybersecurityAwarenessBot.Models;
using CybersecurityAwarenessBot.Services;

namespace CybersecurityAwarenessBot;

/// <summary>
/// Main WPF window with Chat, Tasks, Quiz, and Activity Log tabs.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ChatbotService _chatbot = new();
    private int _activityLogDisplayCount = 10;
    private int? _selectedQuizAnswer;

    public MainWindow()
    {
        InitializeComponent();
        WireChatbot();
        Loaded += MainWindow_Loaded;
    }

    private void WireChatbot()
    {
        _chatbot.BotMessage += (message, outcome) =>
            Dispatcher.Invoke(() => AppendBotBubble(message, outcome));

        _chatbot.UserMessage += message =>
        {
            if (!string.IsNullOrWhiteSpace(message))
                Dispatcher.Invoke(() => AppendUserBubble(message));
        };

        _chatbot.TasksChanged += () => Dispatcher.Invoke(RefreshTasksGrid);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TryPlayGreeting(showWarning: false);
        _chatbot.BeginSession();
        UpdateTaskDbStatus();
        RefreshTasksGrid();
        RefreshActivityLog();
        InputBox.Focus();
    }

    private void PlayGreetingButton_Click(object sender, RoutedEventArgs e) =>
        TryPlayGreeting(showWarning: true);

    private void TryPlayGreeting(bool showWarning)
    {
        if (AudioPlayer.PlayGreeting("Assets/Greetings.wav"))
            return;

        if (showWarning)
        {
            MessageBox.Show(
                "Greeting audio file not found.\n\n" +
                "Add a WAV file named Greetings.wav to the Assets folder:\n" +
                $"{Path.Combine(AppContext.BaseDirectory, "Assets", "Greetings.wav")}\n\n" +
                "Then restart the app.",
                "Voice Greeting",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void SendButton_Click(object sender, RoutedEventArgs e) => SendUserInput();

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendUserInput();
            e.Handled = true;
        }
    }

    private void SendUserInput()
    {
        string text = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        InputBox.Clear();
        _chatbot.ProcessMessage(text);

        if (!_chatbot.Context.AwaitingName && !string.IsNullOrEmpty(_chatbot.User.Name))
            UserNameLabel.Text = _chatbot.User.Name;

        RefreshActivityLog();
        InputBox.Focus();
        ScrollToEnd();
    }

    #region Chat UI

    private void AppendUserBubble(string text)
    {
        ChatPanel.Children.Add(CreateBubble(text, "UserBubbleBrush", "OutcomeTextLightBrush", HorizontalAlignment.Right));
        ScrollToEnd();
    }

    private void AppendBotBubble(string text, BotMessageOutcome outcome)
    {
        var (bg, fg) = GetOutcomeBrushes(outcome);
        ChatPanel.Children.Add(CreateBubble($"Bot: {text}", bg, fg, HorizontalAlignment.Left));
        ScrollToEnd();
    }

    private static (string BackgroundKey, string ForegroundKey) GetOutcomeBrushes(BotMessageOutcome outcome) =>
        outcome switch
        {
            BotMessageOutcome.Success => ("SuccessBrush", "OutcomeTextLightBrush"),
            BotMessageOutcome.Warning => ("WarningBrush", "OutcomeTextDarkBrush"),
            BotMessageOutcome.Error => ("ErrorBrush", "OutcomeTextLightBrush"),
            BotMessageOutcome.Worried => ("WorriedBrush", "OutcomeTextDarkBrush"),
            BotMessageOutcome.Curious => ("CuriousBrush", "OutcomeTextDarkBrush"),
            BotMessageOutcome.Frustrated => ("FrustratedBrush", "OutcomeTextDarkBrush"),
            BotMessageOutcome.Farewell => ("FarewellBrush", "OutcomeTextLightBrush"),
            _ => ("BotBubbleBrush", "TextBrush")
        };

    private Border CreateBubble(string text, string bgKey, string fgKey, HorizontalAlignment alignment)
    {
        var label = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource(fgKey)!,
            FontSize = 14,
            Margin = new Thickness(12, 8, 12, 8),
            MaxWidth = 640
        };

        return new Border
        {
            Background = (Brush)FindResource(bgKey)!,
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(alignment == HorizontalAlignment.Right ? 80 : 0, 4, alignment == HorizontalAlignment.Left ? 80 : 0, 4),
            HorizontalAlignment = alignment,
            Child = label
        };
    }

    private void ScrollToEnd() => ChatScrollViewer.ScrollToEnd();

    #endregion

    #region Tasks Tab

    private void UpdateTaskDbStatus()
    {
        if (_chatbot.TaskDatabase.IsAvailable)
            TaskDbStatusLabel.Text = "MySQL connected. Tasks sync to the database.";
        else
            TaskDbStatusLabel.Text = $"Database offline: {_chatbot.TaskDatabase.LastError}. Update appsettings.json and click Refresh.";
    }

    private void RefreshTasksGrid()
    {
        TasksGrid.ItemsSource = null;
        TasksGrid.ItemsSource = _chatbot.TaskDatabase.GetAllTasks();
        UpdateTaskDbStatus();
    }

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        string title = TaskTitleBox.Text.Trim();
        string desc = TaskDescBox.Text.Trim();

        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(desc))
        {
            MessageBox.Show("Please enter both a title and description.", "Task Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DateTime? reminder = TaskReminderPicker.SelectedDate?.Date.AddHours(9);
        var (success, _, error) = _chatbot.TaskDatabase.AddTask(title, desc, reminder);

        if (!success)
        {
            MessageBox.Show($"Could not add task: {error}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _chatbot.ActivityLog.Log($"Task added via GUI: '{title}'" + (reminder.HasValue ? $" (Reminder: {reminder:dd MMM yyyy})" : "."));
        TaskTitleBox.Clear();
        TaskDescBox.Clear();
        TaskReminderPicker.SelectedDate = null;
        RefreshTasksGrid();
        RefreshActivityLog();
    }

    private void CompleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (TasksGrid.SelectedItem is not TaskItem task)
        {
            MessageBox.Show("Select a task to mark as complete.", "Task Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_chatbot.TaskDatabase.MarkCompleted(task.Id))
        {
            _chatbot.ActivityLog.Log($"Task marked complete: '{task.Title}'.");
            RefreshTasksGrid();
            RefreshActivityLog();
        }
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (TasksGrid.SelectedItem is not TaskItem task)
        {
            MessageBox.Show("Select a task to delete.", "Task Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Delete task '{task.Title}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        if (_chatbot.TaskDatabase.DeleteTask(task.Id))
        {
            _chatbot.ActivityLog.Log($"Task deleted: '{task.Title}'.");
            RefreshTasksGrid();
            RefreshActivityLog();
        }
    }

    private void RefreshTasksButton_Click(object sender, RoutedEventArgs e)
    {
        _chatbot.TaskDatabase.RetryConnection();
        RefreshTasksGrid();
    }

    #endregion

    #region Quiz Tab

    private void StartQuizButton_Click(object sender, RoutedEventArgs e)
    {
        _chatbot.Quiz.StartSession(5);
        _chatbot.ActivityLog.Log("Quiz started from Quiz tab — 5 questions.");
        _selectedQuizAnswer = null;
        QuizFeedbackLabel.Text = string.Empty;
        NextQuizButton.IsEnabled = false;
        DisplayCurrentQuizQuestion();
        RefreshActivityLog();
    }

    private void DisplayCurrentQuizQuestion()
    {
        QuizOptionsPanel.Children.Clear();
        _selectedQuizAnswer = null;

        var question = _chatbot.Quiz.GetCurrentQuestion();
        if (question == null)
        {
            QuizQuestionLabel.Text = "Quiz finished or not started.";
            QuizProgressLabel.Text = "Press Start to begin.";
            return;
        }

        QuizProgressLabel.Text = $"Question {_chatbot.Quiz.CurrentQuestionNumber} of {_chatbot.Quiz.TotalQuestions} — Score: {_chatbot.Quiz.Score}";
        QuizQuestionLabel.Text = question.Question;

        for (int i = 0; i < question.Options.Count; i++)
        {
            int index = i;
            var btn = new Button
            {
                Content = question.Options[i],
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 8, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = (Brush)FindResource("InputBrush")!,
                Foreground = (Brush)FindResource("TextBrush")!,
                BorderBrush = (Brush)FindResource("SecondaryBrush")!,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Tag = index
            };
            btn.Click += QuizOption_Click;
            QuizOptionsPanel.Children.Add(btn);
        }
    }

    private void QuizOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int index)
        {
            _selectedQuizAnswer = index;
            foreach (Button child in QuizOptionsPanel.Children)
                child.BorderThickness = new Thickness(1);

            btn.BorderThickness = new Thickness(2);
            btn.BorderBrush = (Brush)FindResource("PrimaryBrush")!;
            NextQuizButton.IsEnabled = true;
        }
    }

    private void NextQuizButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedQuizAnswer.HasValue || !_chatbot.Quiz.IsActive)
            return;

        string? feedback = _chatbot.Quiz.SubmitAnswer(_selectedQuizAnswer.Value);
        QuizFeedbackLabel.Text = feedback ?? string.Empty;
        NextQuizButton.IsEnabled = false;

        if (_chatbot.Quiz.IsActive)
        {
            DisplayCurrentQuizQuestion();
        }
        else
        {
            QuizQuestionLabel.Text = "Quiz complete!";
            QuizOptionsPanel.Children.Clear();
            QuizProgressLabel.Text = $"Final score: {_chatbot.Quiz.Score}/{_chatbot.Quiz.TotalQuestions}";
            _chatbot.ActivityLog.Log($"Quiz completed from Quiz tab — score {_chatbot.Quiz.Score}/{_chatbot.Quiz.TotalQuestions}.");
            RefreshActivityLog();
        }
    }

    #endregion

    #region Activity Log Tab

    private void RefreshActivityLog()
    {
        var entries = _chatbot.ActivityLog.GetAllForGui(_activityLogDisplayCount);
        ActivityLogList.ItemsSource = entries.Select(e => e.Formatted).ToList();
        int total = _chatbot.ActivityLog.TotalCount;
        ActivityLogCountLabel.Text = $"Showing {entries.Count} of {total} actions";
        ShowMoreLogButton.IsEnabled = _activityLogDisplayCount < total;
    }

    private void ShowMoreLogButton_Click(object sender, RoutedEventArgs e)
    {
        _activityLogDisplayCount = Math.Min(_activityLogDisplayCount + 10, _chatbot.ActivityLog.TotalCount);
        RefreshActivityLog();
    }

    #endregion
}
