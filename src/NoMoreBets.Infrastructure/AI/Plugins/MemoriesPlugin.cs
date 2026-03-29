using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class MemoriesPlugin
{
  private static readonly string FilesDirectory = Path.Combine(AppContext.BaseDirectory,"AI","Plugins","Files");

  private static string ResolvePath(string filename)
  {
    var name = Path.GetFileName(filename);
    if (string.IsNullOrEmpty(name))
    {
      throw new ArgumentException("Filename must not be empty.", nameof(filename));
    }

    return Path.Combine(FilesDirectory, name);
  }

  [KernelFunction]
  [Description("Lists all available documentation and log file names.")]
  public List<string> GetMemoryFilenames()
  {
    if (!Directory.Exists(FilesDirectory))
    {
      Directory.CreateDirectory(FilesDirectory);
      return new List<string>();
    }

    var allowedExtensions = new[] { "*.md", "*.log" };

    return allowedExtensions
        .SelectMany(pattern => Directory.GetFiles(FilesDirectory, pattern))
        .Select(Path.GetFileName)
        .Where(name => name != null)
        .Select(name => name!)
        .ToList();
  }

  [KernelFunction("Read")]
  [Description("Loads the full contents of a saved memory file (markdown or plain text) from the plugin memory directory. Use this before editing so snippets match exactly. Fails if the file is missing.")]
  public string Read(
    [Description("Base file name only, no folders or path separators (e.g. STRATEGY.md).")]
    string filename)
  {
    var path = ResolvePath(filename);
    if (!File.Exists(path))
    {
      throw new FileNotFoundException("Memory file does not exist.", path);
    }

    return File.ReadAllText(path);
  }

  [KernelFunction("Write")]
  [Description("Replaces the entire memory file with new content. Creates the file if it does not exist. Prefer Append or Replace for small changes so you do not drop existing text.")]
  public string Write(
    [Description("Base file name only, no folders or path separators (e.g. STRATEGY.md).")]
    string filename,
    [Description("Complete new file body to persist (overwrites everything previously in the file).")]
    string text)
  {
    var path = ResolvePath(filename);
    Directory.CreateDirectory(FilesDirectory);
    File.WriteAllText(path, text);
    return "Strategy updated successfully";
  }

  [KernelFunction("Append")]
  [Description("Adds text to the end of an existing memory file without reading or rewriting the whole file. Fails if the file does not exist; use Write to create a new file.")]
  public string Append(
    [Description("Base file name only, no folders or path separators (e.g. STRATEGY.md).")]
    string filename,
    [Description("Content to add after the current end of the file (e.g. a new section or log line).")]
    string text)
  {
    var path = ResolvePath(filename);
    if (!File.Exists(path))
    {
      throw new FileNotFoundException("Memory file does not exist.", path);
    }

    File.AppendAllText(path, text);
    return "Text appended successfully";
  }

  [KernelFunction("Replace")]
  [Description("Finds an exact byte-for-byte substring in a memory file and substitutes newText. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.")]
  public string Replace(
    [Description("Base file name only, no folders or path separators (e.g. STRATEGY.md).")]
    string filename,
    [Description("Literal text to find; copy from Read output so spacing and casing match.")]
    string oldText,
    [Description("Replacement for matched text; may be empty to delete the matched segment.")]
    string newText,
    [Description("True: replace every match. False: replace only if oldText appears once (safer for targeted edits).")]
    bool replaceAll = false)
  {
    if (string.IsNullOrEmpty(oldText))
    {
      throw new ArgumentException("oldText must not be null or empty.", nameof(oldText));
    }

    var path = ResolvePath(filename);
    if (!File.Exists(path))
    {
      throw new FileNotFoundException("Memory file does not exist.", path);
    }

    var content = File.ReadAllText(path);
    newText ??= string.Empty;

    string updated;
    if (replaceAll)
    {
      if (content.IndexOf(oldText, StringComparison.Ordinal) < 0)
      {
        throw new InvalidOperationException("oldText was not found in the file.");
      }

      updated = content.Replace(oldText, newText, StringComparison.Ordinal);
    }
    else
    {
      var first = content.IndexOf(oldText, StringComparison.Ordinal);
      if (first < 0)
      {
        throw new InvalidOperationException("oldText was not found in the file.");
      }

      var second = content.IndexOf(oldText, first + oldText.Length, StringComparison.Ordinal);
      if (second >= 0)
      {
        throw new InvalidOperationException(
          "oldText appears more than once; use a longer unique snippet, or set replaceAll to true.");
      }

      updated = string.Concat(content.AsSpan(0, first), newText, content.AsSpan(first + oldText.Length));
    }

    File.WriteAllText(path, updated);
    return "Replacement applied successfully";
  }
}
