using CybersecurityAwarenessBot.Delegates;

using CybersecurityAwarenessBot.Models;



namespace CybersecurityAwarenessBot.Services;



/// <summary>

/// Core chatbot engine: Parts 1–2 features plus NLP, tasks, quiz, and activity logging.

/// </summary>

public class ChatbotService

{

    private readonly KeywordRecognizer _keywordRecognizer = new();

    private readonly MemoryService _memory = new();

    private readonly ConversationContext _context = new();

    private readonly UserProfile _user = new();

    private readonly SentimentResponseHandler _adaptResponse;

    private readonly NlpIntentRecognizer _nlp = new();

    private readonly ActivityLogService _activityLog;

    private readonly TaskDatabaseService _taskDb;

    private readonly QuizService _quiz;



    private TaskItem? _pendingReminderTask;

    private bool _awaitingQuizAnswer;



    public event BotMessageHandler? BotMessage;

    public event UserMessageHandler? UserMessage;

    public event Action? TasksChanged;



    public ChatbotService(

        SentimentResponseHandler? adaptResponse = null,

        ActivityLogService? activityLog = null,

        TaskDatabaseService? taskDb = null,

        QuizService? quiz = null)

    {

        _adaptResponse = adaptResponse ?? SentimentResponseAdapter.Adapt;

        _activityLog = activityLog ?? new ActivityLogService();

        _taskDb = taskDb ?? new TaskDatabaseService();

        _quiz = quiz ?? new QuizService();

    }



    public UserProfile User => _user;

    public ConversationContext Context => _context;

    public MemoryService Memory => _memory;

    public ActivityLogService ActivityLog => _activityLog;

    public TaskDatabaseService TaskDatabase => _taskDb;

    public QuizService Quiz => _quiz;



    public void BeginSession()

    {

        EmitBot("Hello! Welcome to the Cybersecurity Awareness Bot.");

        EmitBot("I am here to help you stay safe online.");

        if (!_taskDb.IsAvailable)

        {

            EmitBot($"Note: Task database is offline ({_taskDb.LastError}). Configure MySQL in appsettings.json and use the Tasks tab.", BotMessageOutcome.Warning);

        }

        EmitBot("What is your name?");

        _context.AwaitingName = true;

        _activityLog.Log("Session started.");

    }



    public string ProcessMessage(string rawInput)

    {

        try

        {

            if (string.IsNullOrWhiteSpace(rawInput))

            {

                return EmitBot("You entered an empty message. Please type a valid cybersecurity question.", BotMessageOutcome.Warning);

            }



            UserMessage?.Invoke(rawInput);



            string input = rawInput.Trim();

            string normalized = input.ToLowerInvariant();



            if (_context.AwaitingName)

                return HandleNameInput(input);



            if (IsExitCommand(normalized))

            {

                _activityLog.Log("User ended session.");

                return EmitBot($"Goodbye, {_user.Name}! Stay alert, stay secure, and stay informed.", BotMessageOutcome.Farewell);

            }



            if (_awaitingQuizAnswer && TryHandleQuizAnswer(input, out string? quizReply))

                return quizReply!;



            if (_pendingReminderTask != null && TryHandleReminderFollowUp(input, out string? reminderReply))

                return reminderReply!;



            var sentiment = SentimentDetector.Detect(input);

            _context.LastSentiment = sentiment;



            // NLP intent detection (regex-based)

            string? nlpReply = TryHandleNlpIntent(input, sentiment);

            if (nlpReply != null)

                return nlpReply;



            if (TryCaptureInterest(input, out string interestTopic))

            {

                _user.Interests.Add(interestTopic);

                _memory.Remember("interest", interestTopic);

                string ack = $"Great! I'll remember that you're interested in {interestTopic}. ";

                string tip = BuildTopicResponse(interestTopic, sentiment, includeMemory: false);

                _context.SetTopic(interestTopic);

                _activityLog.Log($"NLP: remembered interest in {interestTopic}.");

                return EmitBot(_adaptResponse(ack + tip, sentiment, _user.Name), OutcomeForSentiment(sentiment));

            }



            if (IsFollowUpRequest(normalized))

                return HandleFollowUp(sentiment);



            string? meta = TryMetaResponse(normalized, sentiment);

            if (meta != null)

                return EmitBot(meta, OutcomeForSentiment(sentiment));



            string? topic = _keywordRecognizer.RecognizeTopic(input);

            if (topic != null)

            {

                _context.SetTopic(topic);

                return EmitBot(BuildTopicResponse(topic, sentiment, includeMemory: true), OutcomeForSentiment(sentiment));

            }



            string? memoryReply = TryMemoryRecall(normalized, sentiment);

            if (memoryReply != null)

                return EmitBot(memoryReply, OutcomeForSentiment(sentiment));



            return EmitBot(_adaptResponse(

                "I didn't quite understand that. Could you rephrase? " +

                "Try: 'Add a task to enable 2FA', 'Remind me to update my password tomorrow', 'Start the quiz', or 'What have you done for me?'",

                sentiment,

                _user.Name), BotMessageOutcome.Warning);

        }

        catch (Exception)

        {

            return EmitBot("Something unexpected happened, but I'm still here. Please try your question again.", BotMessageOutcome.Error);

        }

    }



    private string? TryHandleNlpIntent(string input, SentimentType sentiment)

    {

        var intent = _nlp.Recognize(input);



        switch (intent.Type)

        {

            case NlpIntentType.ViewActivityLog:

            case NlpIntentType.Summary when intent.ExtractedText == "show more":

                _activityLog.Log("User viewed activity log via chat.");

                if (intent.ExtractedText == "show more")

                    return EmitBot(_activityLog.ShowMore(), BotMessageOutcome.Success);

                _activityLog.ResetDisplayCount();

                return EmitBot(_activityLog.FormatRecentSummary(10), BotMessageOutcome.Success);



            case NlpIntentType.StartQuiz:

                return StartQuizFromChat();



            case NlpIntentType.AddTask:

                return AddTaskFromChat(intent.ExtractedText, sentiment);



            case NlpIntentType.SetReminder:

                return SetReminderFromChat(intent.ExtractedText, intent.ReminderPhrase, sentiment);



            case NlpIntentType.ViewTasks:

                return EmitBot(FormatTaskListSummary(), BotMessageOutcome.Success);



            default:

                return null;

        }

    }



    private string AddTaskFromChat(string taskText, SentimentType sentiment)

    {

        if (string.IsNullOrWhiteSpace(taskText))

            return EmitBot("What task would you like to add? For example: 'Add a task to enable two-factor authentication.'", BotMessageOutcome.Warning);



        string title = taskText.Length > 80 ? taskText[..80] : taskText;

        var (success, id, error) = _taskDb.AddTask(title, taskText, null);



        if (!success)

        {

            return EmitBot($"Could not save the task: {error}. Use the Tasks tab or check your MySQL connection.", BotMessageOutcome.Error);

        }



        _pendingReminderTask = new TaskItem { Id = id, Title = title, Description = taskText };

        _activityLog.Log($"Task added: '{title}'.");

        _activityLog.Log($"NLP recognized add-task intent for '{title}'.");

        TasksChanged?.Invoke();



        return EmitBot(_adaptResponse(

            $"Task added: '{title}'. Would you like to set a reminder for this task? (Reply 'yes' with a timeframe, e.g. 'yes, in 3 days')",

            sentiment,

            _user.Name), BotMessageOutcome.Success);

    }



    private string SetReminderFromChat(string taskText, string? reminderPhrase, SentimentType sentiment)

    {

        if (string.IsNullOrWhiteSpace(taskText))

            return EmitBot("What should I remind you about? For example: 'Remind me to update my password tomorrow.'", BotMessageOutcome.Warning);



        DateTime? reminderDate = NlpIntentRecognizer.ParseReminderDate(reminderPhrase) ?? DateTime.Today.AddDays(1).AddHours(9);

        string title = taskText.Length > 80 ? taskText[..80] : taskText;



        var (success, id, error) = _taskDb.AddTask(title, taskText, reminderDate);

        if (!success)

            return EmitBot($"Could not set reminder: {error}", BotMessageOutcome.Error);



        _activityLog.Log($"Reminder set: '{title}' on {reminderDate:dd MMM yyyy}.");

        _activityLog.Log($"NLP recognized reminder intent for '{title}'.");

        TasksChanged?.Invoke();



        return EmitBot(_adaptResponse(

            $"Reminder set for '{title}' on {reminderDate:dd MMM yyyy}.",

            sentiment,

            _user.Name), BotMessageOutcome.Success);

    }



    private bool TryHandleReminderFollowUp(string input, out string? reply)

    {

        reply = null;

        string lower = input.ToLowerInvariant();



        if (lower is "no" or "n" or "no thanks" or "not now")

        {

            _pendingReminderTask = null;

            reply = EmitBot("No problem! You can set a reminder anytime from the Tasks tab.", BotMessageOutcome.Neutral);

            return true;

        }



        if (lower.StartsWith("yes") || lower.Contains("remind"))

        {

            DateTime? date = NlpIntentRecognizer.ParseReminderDate(input) ?? DateTime.Today.AddDays(3).AddHours(9);

            var task = _pendingReminderTask!;

            task.ReminderDate = date;

            _taskDb.UpdateTask(task);

            _activityLog.Log($"Reminder set for task '{task.Title}' on {date:dd MMM yyyy}.");

            TasksChanged?.Invoke();

            _pendingReminderTask = null;

            reply = EmitBot($"Got it! I'll remind you about '{task.Title}' on {date:dd MMM yyyy}.", BotMessageOutcome.Success);

            return true;

        }



        return false;

    }



    private string StartQuizFromChat()

    {

        _quiz.StartSession(5);

        _awaitingQuizAnswer = true;

        _activityLog.Log("Quiz started via chat — 5 questions.");



        var question = _quiz.GetCurrentQuestion();

        if (question == null)

            return EmitBot("Sorry, the quiz could not start. Try the Quiz tab.", BotMessageOutcome.Error);



        return EmitBot(FormatQuizQuestion(question), BotMessageOutcome.Curious);

    }



    private bool TryHandleQuizAnswer(string input, out string? reply)

    {

        reply = null;

        if (!_quiz.IsActive)

        {

            _awaitingQuizAnswer = false;

            return false;

        }



        int? selected = ParseQuizAnswer(input, _quiz.GetCurrentQuestion());

        if (!selected.HasValue)

            return false;



        string? feedback = _quiz.SubmitAnswer(selected.Value);

        if (feedback == null)

            return false;



        if (_quiz.IsActive)

        {

            var next = _quiz.GetCurrentQuestion();

            reply = EmitBot(feedback + "\n\n" + FormatQuizQuestion(next!), BotMessageOutcome.Success);

        }

        else

        {

            _awaitingQuizAnswer = false;

            _activityLog.Log($"Quiz completed — score {_quiz.Score}/{_quiz.TotalQuestions}.");

            reply = EmitBot(feedback, BotMessageOutcome.Success);

        }



        return true;

    }



    public string FormatQuizQuestion(QuizQuestion question)

    {

        var options = string.Join("\n", question.Options.Select((o, i) => $"{i + 1}. {o}"));

        return $"Question {_quiz.CurrentQuestionNumber}/{_quiz.TotalQuestions}: {question.Question}\n{options}\n(Reply with the number or answer text)";

    }



    private static int? ParseQuizAnswer(string input, QuizQuestion? question)

    {

        if (question == null) return null;



        string trimmed = input.Trim();

        if (int.TryParse(trimmed, out int num) && num >= 1 && num <= question.Options.Count)

            return num - 1;



        for (int i = 0; i < question.Options.Count; i++)

        {

            if (question.Options[i].Equals(trimmed, StringComparison.OrdinalIgnoreCase)

                || trimmed.Equals(question.Options[i].Split(' ')[0], StringComparison.OrdinalIgnoreCase))

                return i;

        }



        if (question.IsTrueFalse)

        {

            if (trimmed.StartsWith('t')) return 0;

            if (trimmed.StartsWith('f')) return 1;

        }



        return null;

    }



    public string FormatTaskListSummary()

    {

        var tasks = _taskDb.GetAllTasks();

        if (tasks.Count == 0)

            return "You have no tasks yet. Add one via chat ('Add a task to review privacy settings') or the Tasks tab.";



        var lines = tasks.Take(10).Select((t, i) =>

            $"{i + 1}. {(t.IsCompleted ? "[Done] " : "")}{t.Title} — {t.Description} (Reminder: {t.ReminderDisplay})");

        return "Your tasks:\n" + string.Join("\n", lines);

    }



    private string HandleNameInput(string input)

    {

        if (string.IsNullOrWhiteSpace(input))

            return EmitBot("Name cannot be empty. Please enter your name:", BotMessageOutcome.Error);



        _user.Name = input.Trim();

        _memory.Remember("name", _user.Name);

        _context.AwaitingName = false;



        EmitBot($"Nice to meet you, {_user.Name}!");

        EmitBot("You can ask me things like:");

        EmitBot("• I'm worried about online scams");

        EmitBot("• Add a task to enable two-factor authentication");

        EmitBot("• Remind me to update my password tomorrow");

        EmitBot("• Start the cybersecurity quiz");

        EmitBot("• What have you done for me?");

        EmitBot("Type 'exit' when you are done.");



        return string.Empty;

    }



    private string HandleFollowUp(SentimentType sentiment)

    {

        if (string.IsNullOrEmpty(_context.CurrentTopic))

        {

            return EmitBot(_adaptResponse(

                "Happy to share more! Which topic should we continue—passwords, phishing, privacy, scams, or malware?",

                sentiment,

                _user.Name), OutcomeForSentiment(sentiment));

        }



        string tip = ResponseService.GetFollowUpDetail(_context.CurrentTopic);

        string intro = sentiment == SentimentType.Curious ? "Here's more on that topic: " : "Another tip for you: ";

        return EmitBot(_adaptResponse(intro + tip, sentiment, _user.Name), OutcomeForSentiment(sentiment));

    }



    private string BuildTopicResponse(string topic, SentimentType sentiment, bool includeMemory)

    {

        string tip = ResponseService.GetRandomResponse(topic);

        string response = tip;



        if (includeMemory && _memory.HasInterest(topic))

            response = $"As someone interested in {topic}, this is especially relevant: " + tip;

        else if (_user.Interests.Count > 0 && includeMemory)

        {

            string lastInterest = _user.Interests[^1];

            if (!lastInterest.Equals(topic, StringComparison.OrdinalIgnoreCase))

                response += $" (You also mentioned interest in {lastInterest}—happy to explore that next.)";

        }



        return _adaptResponse(response, sentiment, _user.Name);

    }



    private bool TryCaptureInterest(string input, out string topic)

    {

        topic = string.Empty;

        string lower = input.ToLowerInvariant();



        string[] patterns = ["interested in ", "interest in ", "i like ", "i love ", "i care about "];



        foreach (string pattern in patterns)

        {

            int idx = lower.IndexOf(pattern, StringComparison.Ordinal);

            if (idx < 0) continue;



            string remainder = input[(idx + pattern.Length)..].Trim().TrimEnd('.', '!', '?');

            if (remainder.Length == 0) continue;



            topic = _keywordRecognizer.RecognizeTopic(remainder) ?? remainder.Split(' ')[0].ToLowerInvariant();

            return true;

        }



        return false;

    }



    private string? TryMetaResponse(string normalized, SentimentType sentiment)

    {

        if (normalized is "how are you" or "how are you?" or "how are you doing")

        {

            return _adaptResponse(

                $"I am doing well, {_user.Name}. Thank you for asking! I am ready to help you with cybersecurity awareness.",

                sentiment,

                _user.Name);

        }



        if (normalized.Contains("purpose") || normalized.Contains("what do you do"))

        {

            return _adaptResponse(

                "My purpose is to teach users about cybersecurity, manage security tasks, run quizzes, and help them stay safe from online threats.",

                sentiment,

                _user.Name);

        }



        if (normalized.Contains("what can i ask") || normalized.Contains("what can you help") || normalized.Contains("help me with"))

        {

            var topics = string.Join(", ", ResponseService.GetTopicList());

            return _adaptResponse(

                $"You can ask about {topics}, add tasks, set reminders, take the quiz, or say 'What have you done for me?' for your activity log.",

                sentiment,

                _user.Name);

        }



        if (normalized.Contains("hello") || normalized is "hi" or "hey")

            return _adaptResponse($"Hello again, {_user.Name}! What cybersecurity topic can I help with?", sentiment, _user.Name);



        return null;

    }



    private string? TryMemoryRecall(string normalized, SentimentType sentiment)

    {

        if (normalized.Contains("my name") || normalized.Contains("who am i") || normalized.Contains("remember my name"))

        {

            string? name = _memory.Recall("name") ?? _user.Name;

            return _adaptResponse($"Your name is {name}.", sentiment, _user.Name);

        }



        if (normalized.Contains("what do you remember") || normalized.Contains("what did i tell you"))

        {

            var entries = _memory.GetAll();

            if (entries.Count == 0)

                return _adaptResponse("I haven't stored any details yet—tell me what you're interested in!", sentiment, _user.Name);



            string facts = string.Join("; ", entries.Select(e => $"{e.Key}: {e.Value}"));

            return _adaptResponse($"Here's what I remember: {facts}.", sentiment, _user.Name);

        }



        string? interest = _memory.Recall("interest");

        if (interest != null && (normalized.Contains("interest") || normalized.Contains("remember")))

        {

            return _adaptResponse(

                $"You mentioned you're interested in {interest}. Would you like another tip about that?",

                sentiment,

                _user.Name);

        }



        return null;

    }



    private static bool IsFollowUpRequest(string normalized) =>

        normalized.Contains("another tip") || normalized.Contains("more tip") ||

        normalized.Contains("explain more") || normalized.Contains("tell me more") ||

        normalized.Contains("give me more") || normalized is "more" ||

        normalized.Contains("go on") || normalized.Contains("continue");



    private static bool IsExitCommand(string normalized) =>

        normalized is "exit" or "quit" or "bye" or "goodbye" or "close";



    private static BotMessageOutcome OutcomeForSentiment(SentimentType sentiment) =>

        sentiment switch

        {

            SentimentType.Worried => BotMessageOutcome.Worried,

            SentimentType.Curious => BotMessageOutcome.Curious,

            SentimentType.Frustrated => BotMessageOutcome.Frustrated,

            _ => BotMessageOutcome.Success

        };



    private string EmitBot(string message, BotMessageOutcome outcome = BotMessageOutcome.Neutral)

    {

        BotMessage?.Invoke(message, outcome);

        return message;

    }

}


