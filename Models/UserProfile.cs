namespace CybersecurityAwarenessBot.Models;

/// <summary>
/// Stores user identity and preferences recalled during conversation.
/// </summary>
public class UserProfile
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Topics the user has expressed interest in (e.g. privacy, phishing).
    /// </summary>
    public List<string> Interests { get; } = new();
}
