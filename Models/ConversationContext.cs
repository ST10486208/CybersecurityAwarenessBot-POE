namespace CybersecurityAwarenessBot.Models;

/// <summary>
/// Tracks the active topic so follow-up prompts continue the same thread.
/// </summary>
public class ConversationContext
{
    public string? CurrentTopic { get; set; }

    public SentimentType LastSentiment { get; set; } = SentimentType.Neutral;

    public bool AwaitingName { get; set; } = true;

    public void SetTopic(string topic)
    {
        CurrentTopic = topic;
    }

    public void ClearTopic()
    {
        CurrentTopic = null;
    }
}
