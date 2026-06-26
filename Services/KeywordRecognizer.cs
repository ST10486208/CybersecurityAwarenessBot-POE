namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// Maps cybersecurity keywords found inside natural language to canonical topics.
/// </summary>
public class KeywordRecognizer
{
    private readonly Dictionary<string, string> _keywordToTopic;

    public KeywordRecognizer()
    {
        _keywordToTopic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["password"] = "password",
            ["passwords"] = "password",
            ["passcode"] = "password",
            ["credential"] = "password",
            ["phishing"] = "phishing",
            ["phish"] = "phishing",
            ["fake email"] = "phishing",
            ["spoof"] = "phishing",
            ["malware"] = "malware",
            ["virus"] = "malware",
            ["ransomware"] = "malware",
            ["spyware"] = "malware",
            ["trojan"] = "malware",
            ["scam"] = "scams",
            ["scams"] = "scams",
            ["fraud"] = "scams",
            ["con artist"] = "scams",
            ["privacy"] = "privacy",
            ["private data"] = "privacy",
            ["personal information"] = "privacy",
            ["data protection"] = "privacy",
            ["safe browsing"] = "browsing",
            ["browse safely"] = "browsing",
            ["browsing"] = "browsing",
            ["https"] = "browsing",
            ["suspicious link"] = "links",
            ["suspicious links"] = "links",
            ["bad link"] = "links",
            ["clickbait"] = "links",
            ["vpn"] = "vpn",
            ["two-factor"] = "twofactor",
            ["2fa"] = "twofactor",
            ["mfa"] = "twofactor"
        };
    }

    /// <summary>
    /// Returns the first matching topic key, or null if no keyword is found.
    /// </summary>
    public string? RecognizeTopic(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        string lower = input.ToLowerInvariant();

        foreach (var pair in _keywordToTopic.OrderByDescending(p => p.Key.Length))
        {
            if (lower.Contains(pair.Key))
                return pair.Value;
        }

        return null;
    }

    public IReadOnlyCollection<string> GetAllTopics() =>
        _keywordToTopic.Values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
