using System.Diagnostics;

namespace NoMoreBets.Tests.Helpers;

/// <summary>
/// Loads fixture files from the test project's Fixtures folder (copied to output).
/// </summary>
public static class FixtureHelper
{
    /// <summary>
    /// Loads fixture text from a relative path under Fixtures (e.g. "rotowire/lineups_page.html").
    /// Returns null if the file does not exist.
    /// </summary>
    public static string? LoadFixtureText(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath);
        if (!File.Exists(path))
        {
            Trace.WriteLine($"Fixture file not found: {path} (relative path: {relativePath})");
            return null;
        }
        var content = File.ReadAllText(path, System.Text.Encoding.UTF8);
        if (string.IsNullOrEmpty(content))
            Trace.WriteLine($"Fixture file is empty: {path} (relative path: {relativePath})");
        return content;
    }
}
