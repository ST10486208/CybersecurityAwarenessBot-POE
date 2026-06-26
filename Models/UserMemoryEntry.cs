namespace CybersecurityAwarenessBot.Models;

/// <summary>
/// A single fact stored in conversational memory for later recall.
/// </summary>
public class UserMemoryEntry
{
    public string Key { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public DateTime StoredAt { get; init; } = DateTime.UtcNow;
}
