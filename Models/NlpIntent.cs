namespace CybersecurityAwarenessBot.Models;

/// <summary>
/// Recognized user intent from NLP pattern matching.
/// </summary>
public enum NlpIntentType
{
    None,
    AddTask,
    SetReminder,
    ViewActivityLog,
    StartQuiz,
    ViewTasks,
    Summary
}

public class NlpIntent
{
    public NlpIntentType Type { get; set; } = NlpIntentType.None;
    public string ExtractedText { get; set; } = string.Empty;
    public string? ReminderPhrase { get; set; }
}
