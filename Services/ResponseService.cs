namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// Organises topic responses in lists and selects them at random for variety.
/// </summary>
public static class ResponseService
{
    private static readonly Random Random = new();

    private static readonly Dictionary<string, List<string>> TopicResponses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["password"] =
        [
            "Use strong, unique passwords for every account. Mix uppercase, lowercase, numbers, and symbols.",
            "Never reuse the same password across websites. A password manager can help you store them safely.",
            "Avoid personal details in passwords (birthdays, names, pet names). Attackers guess those first.",
            "Enable multi-factor authentication wherever possible—it adds a critical second layer of protection."
        ],
        ["phishing"] =
        [
            "Be cautious of emails asking for personal information. Scammers often disguise themselves as trusted organisations.",
            "Check the sender's email address carefully. Phishing addresses often contain subtle misspellings.",
            "Never click suspicious links in unsolicited messages—hover over links to preview the real destination first.",
            "If an offer sounds too good to be true or creates extreme urgency, pause and verify through official channels."
        ],
        ["browsing"] =
        [
            "Stick to trusted websites and look for HTTPS in the address bar before entering sensitive data.",
            "Avoid downloading files from unknown sources—they are a common malware delivery method.",
            "Keep your browser updated so security patches protect you against newly discovered threats.",
            "Use private browsing when on shared computers, and always log out of accounts when finished."
        ],
        ["malware"] =
        [
            "Malware includes viruses, ransomware, and spyware—install reputable antivirus software and keep it updated.",
            "Do not open email attachments from unknown senders; they are a primary malware infection route.",
            "Regularly back up important files offline so ransomware cannot hold your data hostage.",
            "Only install apps from official stores or verified publishers to reduce trojan risk."
        ],
        ["scams"] =
        [
            "Online scammers create fake urgency—prizes, jobs, or threats—to pressure you into acting without thinking.",
            "Never send money or gift cards to strangers who contacted you online, even if they seem official.",
            "Verify unexpected calls or messages by contacting the organisation directly using a number from their real website.",
            "If someone asks for remote access to your computer, refuse—it is a common tech-support scam tactic."
        ],
        ["links"] =
        [
            "Inspect links before clicking: shortened URLs and misspelled domains often hide phishing sites.",
            "When in doubt, type the website address manually instead of following a link from an email or message.",
            "Hover over links to see the true destination before clicking, especially in unsolicited messages.",
            "Report suspicious links to your IT team or email provider to help protect others."
        ],
        ["privacy"] =
        [
            "Review privacy settings on social media and limit who can see your personal information.",
            "Share only what is necessary online—oversharing helps attackers build targeted scams against you.",
            "Use strong privacy controls on accounts and disable location sharing when it is not needed.",
            "Read app permissions before installing—some apps request far more data than they require."
        ],
        ["vpn"] =
        [
            "A VPN encrypts your internet traffic on public Wi-Fi, making it harder for others on the network to intercept data.",
            "Choose a reputable VPN provider with a clear no-logs policy—free VPNs sometimes sell your data.",
            "A VPN protects your connection but does not replace antivirus, strong passwords, or safe browsing habits."
        ],
        ["twofactor"] =
        [
            "Two-factor authentication (2FA) requires something you know (password) plus something you have (phone/app)—dramatically reducing account takeover risk.",
            "Prefer authenticator apps over SMS codes when possible, as SIM-swap attacks can intercept text messages.",
            "Enable 2FA on email, banking, and social accounts first—they are the most valuable targets."
        ]
    };

    public static string GetRandomResponse(string topic)
    {
        if (!TopicResponses.TryGetValue(topic, out var responses) || responses.Count == 0)
            return "I can help with cybersecurity topics—try asking about passwords, phishing, privacy, or scams.";

        return responses[Random.Next(responses.Count)];
    }

    public static string GetFollowUpDetail(string topic)
    {
        if (!TopicResponses.TryGetValue(topic, out var responses) || responses.Count < 2)
            return GetRandomResponse(topic);

        return responses[Random.Next(responses.Count)];
    }

    public static IReadOnlyList<string> GetTopicList() => TopicResponses.Keys.ToList();
}
