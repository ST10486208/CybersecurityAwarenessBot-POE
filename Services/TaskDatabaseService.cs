using System.IO;
using System.Text.Json;
using CybersecurityAwarenessBot.Models;
using MySqlConnector;

namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// MySQL-backed task storage with CRUD operations and error handling.
/// </summary>
public class TaskDatabaseService
{
    private readonly string _connectionString;
    private bool _isAvailable;

    public TaskDatabaseService()
    {
        _connectionString = LoadConnectionString();
        _isAvailable = TryInitialize();
    }

    public bool IsAvailable => _isAvailable;
    public string? LastError { get; private set; }

    public List<TaskItem> GetAllTasks()
    {
        var tasks = new List<TaskItem>();
        if (!_isAvailable) return tasks;

        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand(
                "SELECT id, title, description, reminder_date, is_completed, created_at FROM tasks ORDER BY created_at DESC",
                conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                tasks.Add(MapTask(reader));
            }
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }

        return tasks;
    }

    public (bool Success, int Id, string? Error) AddTask(string title, string description, DateTime? reminderDate)
    {
        if (!_isAvailable)
            return (false, 0, LastError ?? "Database is not available.");

        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand(
                @"INSERT INTO tasks (title, description, reminder_date, is_completed, created_at)
                  VALUES (@title, @desc, @reminder, 0, @created);
                  SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@desc", description);
            cmd.Parameters.AddWithValue("@reminder", reminderDate.HasValue ? reminderDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@created", DateTime.Now);
            int id = Convert.ToInt32(cmd.ExecuteScalar());
            return (true, id, null);
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return (false, 0, ex.Message);
        }
    }

    public bool UpdateTask(TaskItem task)
    {
        if (!_isAvailable) return false;

        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand(
                @"UPDATE tasks SET title=@title, description=@desc, reminder_date=@reminder, is_completed=@completed
                  WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", task.Id);
            cmd.Parameters.AddWithValue("@title", task.Title);
            cmd.Parameters.AddWithValue("@desc", task.Description);
            cmd.Parameters.AddWithValue("@reminder", task.ReminderDate.HasValue ? task.ReminderDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@completed", task.IsCompleted);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public bool MarkCompleted(int taskId)
    {
        if (!_isAvailable) return false;

        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("UPDATE tasks SET is_completed=1 WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", taskId);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public bool DeleteTask(int taskId)
    {
        if (!_isAvailable) return false;

        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand("DELETE FROM tasks WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", taskId);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    public bool RetryConnection()
    {
        _isAvailable = TryInitialize();
        return _isAvailable;
    }

    private bool TryInitialize()
    {
        try
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var cmd = new MySqlCommand(@"
                CREATE TABLE IF NOT EXISTS tasks (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    title VARCHAR(255) NOT NULL,
                    description TEXT NOT NULL,
                    reminder_date DATETIME NULL,
                    is_completed TINYINT(1) NOT NULL DEFAULT 0,
                    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                )", conn);
            cmd.ExecuteNonQuery();
            LastError = null;
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    private static string LoadConnectionString()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path))
                return "Server=localhost;Port=3306;Database=cybersecurity_bot;Uid=root;Pwd=;";

            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var conn)
                && conn.TryGetProperty("TasksDb", out var tasksDb))
            {
                return tasksDb.GetString() ?? string.Empty;
            }
        }
        catch
        {
            // Fall through to default
        }

        return "Server=localhost;Port=3306;Database=cybersecurity_bot;Uid=root;Pwd=;";
    }

    private static TaskItem MapTask(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32("id"),
        Title = reader.GetString("title"),
        Description = reader.GetString("description"),
        ReminderDate = reader.IsDBNull(reader.GetOrdinal("reminder_date")) ? null : reader.GetDateTime("reminder_date"),
        IsCompleted = reader.GetBoolean("is_completed"),
        CreatedAt = reader.GetDateTime("created_at")
    };
}
