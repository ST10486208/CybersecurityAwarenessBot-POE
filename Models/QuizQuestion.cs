namespace CybersecurityAwarenessBot.Models;

/// <summary>
/// Cybersecurity quiz question (multiple-choice or true/false).
/// </summary>
public class QuizQuestion
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = [];
    public int CorrectIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public bool IsTrueFalse { get; set; }
}
