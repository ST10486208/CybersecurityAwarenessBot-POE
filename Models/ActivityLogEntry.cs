namespace CybersecurityAwarenessBot.Models;

/// <summary>
/// Single activity log record with timestamp.
/// </summary>
public class ActivityLogEntry
{
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public string Formatted => $"[{Timestamp:dd MMM yyyy HH:mm}] {Description}";
}
