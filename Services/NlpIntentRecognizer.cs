using System.Text.RegularExpressions;
using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// Regex-based NLP intent detection for tasks, reminders, quiz, and activity log.
/// </summary>
public partial class NlpIntentRecognizer
{
    [GeneratedRegex(@"(?:add|create|new)\s+(?:a\s+)?task(?:\s+to|\s*:|\s+for)?\s+(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex AddTaskPattern();

    [GeneratedRegex(@"remind\s+me\s+(?:to\s+)?(.+?)(?:\s+(tomorrow|in\s+\d+\s+days?|on\s+.+))?$", RegexOptions.IgnoreCase)]
    private static partial Regex RemindMePattern();

    [GeneratedRegex(@"(?:add|set)\s+(?:a\s+)?reminder(?:\s+to|\s+for)?\s+(.+)", RegexOptions.IgnoreCase)]
    private static partial Regex AddReminderPattern();

    [GeneratedRegex(@"(?:show|view|display)\s+(?:the\s+)?activity\s+log|what\s+have\s+you\s+done\s+for\s+me|recent\s+actions?", RegexOptions.IgnoreCase)]
    private static partial Regex ActivityLogPattern();

    [GeneratedRegex(@"(?:start|take|play|begin)\s+(?:the\s+)?(?:cybersecurity\s+)?quiz|(?:give\s+me\s+a\s+)?quiz\s+me", RegexOptions.IgnoreCase)]
    private static partial Regex StartQuizPattern();

    [GeneratedRegex(@"(?:show|list|view)\s+(?:my\s+)?tasks?", RegexOptions.IgnoreCase)]
    private static partial Regex ViewTasksPattern();

    [GeneratedRegex(@"show\s+more(?:\s+activity)?", RegexOptions.IgnoreCase)]
    private static partial Regex ShowMorePattern();

    public NlpIntent Recognize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new NlpIntent();

        string trimmed = input.Trim();

        if (ShowMorePattern().IsMatch(trimmed))
            return new NlpIntent { Type = NlpIntentType.Summary, ExtractedText = "show more" };

        if (ActivityLogPattern().IsMatch(trimmed))
            return new NlpIntent { Type = NlpIntentType.ViewActivityLog };

        if (StartQuizPattern().IsMatch(trimmed) || trimmed.Contains("quiz", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Contains("question", StringComparison.OrdinalIgnoreCase))
        {
            if (Regex.IsMatch(trimmed, @"\bquiz\b", RegexOptions.IgnoreCase))
                return new NlpIntent { Type = NlpIntentType.StartQuiz };
        }

        if (ViewTasksPattern().IsMatch(trimmed))
            return new NlpIntent { Type = NlpIntentType.ViewTasks };

        var remindMatch = RemindMePattern().Match(trimmed);
        if (remindMatch.Success)
        {
            return new NlpIntent
            {
                Type = NlpIntentType.SetReminder,
                ExtractedText = CleanExtractedText(remindMatch.Groups[1].Value),
                ReminderPhrase = remindMatch.Groups[2].Success ? remindMatch.Groups[2].Value.Trim() : null
            };
        }

        var addReminderMatch = AddReminderPattern().Match(trimmed);
        if (addReminderMatch.Success)
        {
            return new NlpIntent
            {
                Type = NlpIntentType.SetReminder,
                ExtractedText = CleanExtractedText(addReminderMatch.Groups[1].Value)
            };
        }

        var addTaskMatch = AddTaskPattern().Match(trimmed);
        if (addTaskMatch.Success)
        {
            return new NlpIntent
            {
                Type = NlpIntentType.AddTask,
                ExtractedText = CleanExtractedText(addTaskMatch.Groups[1].Value)
            };
        }

        // Broader keyword detection for reminder/task phrases
        if (ContainsReminderKeywords(trimmed) && !trimmed.Contains("quiz", StringComparison.OrdinalIgnoreCase))
        {
            string? extracted = TryExtractAfterKeyword(trimmed, "remind");
            if (extracted != null)
            {
                return new NlpIntent
                {
                    Type = NlpIntentType.SetReminder,
                    ExtractedText = extracted
                };
            }
        }

        if (ContainsTaskKeywords(trimmed))
        {
            string? extracted = TryExtractAfterKeyword(trimmed, "task");
            if (extracted != null)
            {
                return new NlpIntent
                {
                    Type = NlpIntentType.AddTask,
                    ExtractedText = extracted
                };
            }
        }

        return new NlpIntent();
    }

    public static DateTime? ParseReminderDate(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return null;

        string lower = phrase.ToLowerInvariant().Trim();

        if (lower is "tomorrow" or "tomorrow.")
            return DateTime.Today.AddDays(1).AddHours(9);

        var inDaysMatch = Regex.Match(lower, @"in\s+(\d+)\s+days?");
        if (inDaysMatch.Success && int.TryParse(inDaysMatch.Groups[1].Value, out int days))
            return DateTime.Today.AddDays(days).AddHours(9);

        var onDateMatch = Regex.Match(lower, @"on\s+(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})");
        if (onDateMatch.Success && DateTime.TryParse(onDateMatch.Groups[1].Value, out DateTime parsed))
            return parsed.AddHours(9);

        return null;
    }

    private static string CleanExtractedText(string text)
    {
        string cleaned = text.Trim().TrimEnd('.', '!', '?');
        cleaned = Regex.Replace(cleaned, @"^(to|about|regarding)\s+", "", RegexOptions.IgnoreCase);
        return cleaned;
    }

    private static bool ContainsReminderKeywords(string input) =>
        input.Contains("remind", StringComparison.OrdinalIgnoreCase)
        || input.Contains("reminder", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsTaskKeywords(string input) =>
        input.Contains("add", StringComparison.OrdinalIgnoreCase)
        && input.Contains("task", StringComparison.OrdinalIgnoreCase);

    private static string? TryExtractAfterKeyword(string input, string keyword)
    {
        int idx = input.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        string remainder = input[(idx + keyword.Length)..].Trim();
        remainder = Regex.Replace(remainder, @"^(me\s+to|a\s+to|to)\s+", "", RegexOptions.IgnoreCase);
        return string.IsNullOrWhiteSpace(remainder) ? null : CleanExtractedText(remainder);
    }
}
