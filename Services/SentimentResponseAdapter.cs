using CybersecurityAwarenessBot.Delegates;
using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// Applies sentiment-aware prefixes using a delegate for flexible response tailoring.
/// </summary>
public static class SentimentResponseAdapter
{
    public static readonly SentimentResponseHandler Adapt = (baseResponse, sentiment, userName) =>
    {
        string prefix = sentiment switch
        {
            SentimentType.Worried =>
                $"It's completely understandable to feel that way, {userName}. You're taking the right step by learning more. ",
            SentimentType.Curious =>
                $"Great question, {userName}! I'm glad you're curious about staying safe online. ",
            SentimentType.Frustrated =>
                $"I hear you, {userName}—cybersecurity can feel overwhelming at first. Let's break it down simply. ",
            _ => string.Empty
        };

        return prefix + baseResponse;
    };
}
