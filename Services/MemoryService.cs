using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// Generic memory store using a List to recall user-provided facts.
/// </summary>
public class MemoryService
{
    private readonly List<UserMemoryEntry> _entries = new();

    public void Remember(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            return;

        _entries.RemoveAll(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        _entries.Add(new UserMemoryEntry { Key = key.Trim(), Value = value.Trim() });
    }

    public string? Recall(string key)
    {
        return _entries
            .Where(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.StoredAt)
            .Select(e => e.Value)
            .FirstOrDefault();
    }

    public IReadOnlyList<UserMemoryEntry> GetAll() => _entries.AsReadOnly();

    public bool HasInterest(string topic) =>
        _entries.Any(e =>
            e.Key.Equals("interest", StringComparison.OrdinalIgnoreCase) &&
            e.Value.Contains(topic, StringComparison.OrdinalIgnoreCase));
}
