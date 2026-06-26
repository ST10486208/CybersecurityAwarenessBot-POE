using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// In-memory activity log with paginated display support.
/// </summary>
public class ActivityLogService
{
    private readonly List<ActivityLogEntry> _entries = [];
    private int _displayCount = 10;

    public void Log(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return;

        _entries.Add(new ActivityLogEntry { Description = description.Trim() });
    }

    public IReadOnlyList<ActivityLogEntry> GetRecent(int count) =>
        _entries.TakeLast(count).ToList();

    public string FormatRecentSummary(int count = 10)
    {
        var recent = GetRecent(count);
        if (recent.Count == 0)
            return "No activity recorded yet. Add tasks, take the quiz, or chat with me to build your log.";

        var lines = recent.Select((e, i) => $"{i + 1}. {e.Description}");
        return "Here's a summary of recent actions:\n" + string.Join("\n", lines);
    }

    public string ShowMore()
    {
        _displayCount = Math.Min(_displayCount + 10, _entries.Count);
        return FormatRecentSummary(_displayCount);
    }

    public int DisplayCount => _displayCount;
    public int TotalCount => _entries.Count;
    public bool HasMore => _displayCount < _entries.Count;

    public void ResetDisplayCount() => _displayCount = 10;

    public IReadOnlyList<ActivityLogEntry> GetAllForGui(int? limit = null)
    {
        if (limit.HasValue)
            return _entries.TakeLast(limit.Value).ToList();
        return _entries.ToList();
    }
}
