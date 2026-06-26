using System.IO;
using System.Media;

namespace CybersecurityAwarenessBot.Services;

/// <summary>
/// Plays the greeting WAV file from Task 1 when available.
/// </summary>
public static class AudioPlayer
{
    private static SoundPlayer? _player;

    /// <summary>
    /// Attempts to play the greeting WAV. Returns false if the file is missing or playback fails.
    /// </summary>
    public static bool PlayGreeting(string filePath)
    {
        try
        {
            string? fullPath = ResolveGreetingPath(filePath);
            if (fullPath == null)
                return false;

            _player?.Stop();
            _player?.Dispose();

            _player = new SoundPlayer(fullPath);
            _player.Load();
            _player.PlaySync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool GreetingFileExists(string filePath) =>
        ResolveGreetingPath(filePath) != null;

    private static string? ResolveGreetingPath(string filePath)
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, filePath),
            Path.Combine(AppContext.BaseDirectory, "Assets", "Greetings.wav"),
            Path.GetFullPath(filePath),
            Path.GetFullPath(Path.Combine("Assets", "Greetings.wav"))
        ];

        foreach (string path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }
}
