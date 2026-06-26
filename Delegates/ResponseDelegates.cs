using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Delegates;

/// <summary>
/// Transforms a base response based on detected user sentiment.
/// </summary>
public delegate string SentimentResponseHandler(string baseResponse, SentimentType sentiment, string userName);

/// <summary>
/// Raised when the bot produces a message for the UI layer.
/// </summary>
public delegate void BotMessageHandler(string message, BotMessageOutcome outcome);

/// <summary>
/// Raised when the user sends a message (for logging or UI updates).
/// </summary>
public delegate void UserMessageHandler(string message);
