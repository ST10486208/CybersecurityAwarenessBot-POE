using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// Detects simple sentiment cues in user messages for empathetic responses.
/// </summary>
public static class SentimentDetector
{
    private static readonly Dictionary<SentimentType, string[]> Indicators = new()
    {
        [SentimentType.Worried] =
        [
            "worried", "scared", "afraid", "anxious", "nervous", "stress", "stressed",
            "overwhelmed", "concerned", "fear", "panic", "unsure", "don't know what to do"
        ],
        [SentimentType.Curious] =
        [
            "curious", "wonder", "wondering", "how does", "how do", "what is", "what's",
            "tell me", "explain", "learn", "interested", "want to know", "can you teach"
        ],
        [SentimentType.Frustrated] =
        [
            "frustrated", "annoyed", "angry", "fed up", "sick of", "hate", "stupid",
            "doesn't work", "confusing", "confused", "lost", "give up"
        ]
    };

    public static SentimentType Detect(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return SentimentType.Neutral;

        string lower = input.ToLowerInvariant();

        foreach (var pair in Indicators.OrderByDescending(p => p.Value.Length))
        {
            if (pair.Value.Any(word => lower.Contains(word)))
                return pair.Key;
        }

        return SentimentType.Neutral;
    }
}
