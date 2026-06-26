using CybersecurityAwarenessBot.Models;

namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// Cybersecurity quiz with 12+ questions, scoring, and immediate feedback.
/// </summary>
public class QuizService
{
    private readonly List<QuizQuestion> _questions;
    private readonly Random _random = new();
    private List<QuizQuestion> _sessionQuestions = [];
    private int _currentIndex;
    private int _score;
    private bool _active;

    public QuizService()
    {
        _questions = BuildQuestionBank();
    }

    public bool IsActive => _active;
    public int CurrentQuestionNumber => _active ? _currentIndex + 1 : 0;
    public int TotalQuestions => _sessionQuestions.Count;
    public int Score => _score;

    public QuizQuestion? GetCurrentQuestion() =>
        _active && _currentIndex < _sessionQuestions.Count ? _sessionQuestions[_currentIndex] : null;

    public void StartSession(int questionCount = 5)
    {
        _sessionQuestions = _questions.OrderBy(_ => _random.Next()).Take(questionCount).ToList();
        _currentIndex = 0;
        _score = 0;
        _active = _sessionQuestions.Count > 0;
    }

    public string? SubmitAnswer(int selectedIndex)
    {
        if (!_active || _currentIndex >= _sessionQuestions.Count)
            return null;

        var question = _sessionQuestions[_currentIndex];
        bool correct = selectedIndex == question.CorrectIndex;

        if (correct)
            _score++;

        string feedback = correct
            ? $"Correct! {question.Explanation}"
            : $"Not quite. The correct answer is \"{question.Options[question.CorrectIndex]}\". {question.Explanation}";

        _currentIndex++;

        if (_currentIndex >= _sessionQuestions.Count)
        {
            _active = false;
            feedback += "\n\n" + BuildFinalScoreMessage();
        }

        return feedback;
    }

    public string BuildFinalScoreMessage()
    {
        int total = _sessionQuestions.Count;
        double pct = total > 0 ? (double)_score / total * 100 : 0;

        string message = $"Quiz complete! Your score: {_score}/{total} ({pct:F0}%).\n";

        message += pct switch
        {
            >= 80 => "Great job! You're a cybersecurity pro! Keep sharing what you know.",
            >= 50 => "Good effort! Review the explanations and try again to sharpen your skills.",
            _ => "Keep learning to stay safe online! Practice makes perfect—retake the quiz anytime."
        };

        return message;
    }

    private static List<QuizQuestion> BuildQuestionBank() =>
    [
        new()
        {
            Question = "What is phishing?",
            Options = ["A type of malware", "A social engineering attack using fake messages", "A secure encryption method", "A firewall feature"],
            CorrectIndex = 1,
            Explanation = "Phishing tricks users into revealing sensitive information through deceptive emails or websites."
        },
        new()
        {
            Question = "A strong password should include:",
            Options = ["Your name and birth year", "Uppercase, lowercase, numbers, and symbols", "Only lowercase letters", "The word 'password'"],
            CorrectIndex = 1,
            Explanation = "Complex passwords with mixed character types are much harder to crack."
        },
        new()
        {
            Question = "Two-factor authentication (2FA) adds an extra layer of security.",
            Options = ["True", "False"],
            CorrectIndex = 0,
            IsTrueFalse = true,
            Explanation = "2FA requires a second verification step beyond your password."
        },
        new()
        {
            Question = "You should use the same password for all your accounts.",
            Options = ["True", "False"],
            CorrectIndex = 1,
            IsTrueFalse = true,
            Explanation = "Unique passwords prevent one breach from compromising all your accounts."
        },
        new()
        {
            Question = "What does HTTPS indicate in a browser address bar?",
            Options = ["The site is always trustworthy", "The connection is encrypted", "The site is free of malware", "The site loads faster"],
            CorrectIndex = 1,
            Explanation = "HTTPS encrypts data between your browser and the server."
        },
        new()
        {
            Question = "Which is a sign of a suspicious email?",
            Options = ["Personalized greeting with your full name", "Urgent threats demanding immediate action", "Clear company branding", "Unsubscribe link"],
            CorrectIndex = 1,
            Explanation = "Urgency and pressure tactics are common phishing red flags."
        },
        new()
        {
            Question = "Ransomware is malware that:",
            Options = ["Speeds up your computer", "Encrypts files and demands payment", "Improves network security", "Deletes only temporary files"],
            CorrectIndex = 1,
            Explanation = "Ransomware locks your data until a ransom is paid—backups are essential."
        },
        new()
        {
            Question = "Public Wi-Fi without a VPN is always safe for banking.",
            Options = ["True", "False"],
            CorrectIndex = 1,
            IsTrueFalse = true,
            Explanation = "Public networks can be intercepted; use VPN or avoid sensitive transactions."
        },
        new()
        {
            Question = "What should you do if you receive an unexpected attachment?",
            Options = ["Open it immediately", "Forward it to colleagues", "Verify the sender before opening", "Reply with your password"],
            CorrectIndex = 2,
            Explanation = "Unexpected attachments are a common malware delivery method."
        },
        new()
        {
            Question = "Social engineering attacks exploit:",
            Options = ["Hardware flaws only", "Human psychology and trust", "Encryption algorithms", "Power outages"],
            CorrectIndex = 1,
            Explanation = "Attackers manipulate people into revealing information or taking unsafe actions."
        },
        new()
        {
            Question = "Keeping software updated helps protect against known vulnerabilities.",
            Options = ["True", "False"],
            CorrectIndex = 0,
            IsTrueFalse = true,
            Explanation = "Updates patch security holes that attackers actively exploit."
        },
        new()
        {
            Question = "Which link is most likely safe to click?",
            Options = ["http://paypa1-secure.com/login", "https://www.paypal.com", "bit.ly/unknown", "Email link with no context"],
            CorrectIndex = 1,
            Explanation = "Always verify the official domain; look-alike URLs are a phishing tactic."
        }
    ];
}
