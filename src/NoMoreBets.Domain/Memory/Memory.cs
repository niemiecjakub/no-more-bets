namespace NoMoreBets.Domain.Memories;

public class Memory
{
  public const int MaxNameLength = 200;

  private Memory()
  {
  }

  public int Id { get; private set; }
  public string Name { get; private set; } = null!;
  public string? Description { get; private set; }
  public string Content { get; private set; } = null!;
  public DateTime CreatedAt { get; private set; }
  public DateTime UpdatedAt { get; private set; }
  public DateTime? DeletedAt { get; private set; }

  public static Memory Create(string name, string? content, string? description = null)
  {
    ValidateName(name);
    var now = DateTime.UtcNow;
    return new Memory
    {
      Name = name,
      Content = content ?? string.Empty,
      Description = description,
      CreatedAt = now,
      UpdatedAt = now,
      DeletedAt = null
    };
  }

  public void MarkDeleted()
  {
    if (DeletedAt.HasValue)
    {
      return;
    }

    DeletedAt = DateTime.UtcNow;
    Touch();
  }

  public static void ValidateName(string name)
  {
    if (string.IsNullOrEmpty(name))
    {
      throw new ArgumentException("Name must not be empty.", nameof(name));
    }

    if (name.Length > MaxNameLength)
    {
      throw new ArgumentException($"Name must be at most {MaxNameLength} characters.", nameof(name));
    }
  }

  public void ReplaceContent(string? text)
  {
    ThrowIfDeleted();
    Content = text ?? string.Empty;
    Touch();
  }

  public void AppendContent(string? text)
  {
    ThrowIfDeleted();
    Content += text ?? string.Empty;
    Touch();
  }

  public void ReplaceSubstring(string oldText, string? newText, bool replaceAll)
  {
    ThrowIfDeleted();
    if (string.IsNullOrEmpty(oldText))
    {
      throw new ArgumentException("oldText must not be null or empty.", nameof(oldText));
    }

    var current = Content;
    var replacement = newText ?? string.Empty;

    string updated;
    if (replaceAll)
    {
      if (current.IndexOf(oldText, StringComparison.Ordinal) < 0)
      {
        throw new InvalidOperationException("oldText was not found in the memory record.");
      }

      updated = current.Replace(oldText, replacement, StringComparison.Ordinal);
    }
    else
    {
      var first = current.IndexOf(oldText, StringComparison.Ordinal);
      if (first < 0)
      {
        throw new InvalidOperationException("oldText was not found in the memory record.");
      }

      var second = current.IndexOf(oldText, first + oldText.Length, StringComparison.Ordinal);
      if (second >= 0)
      {
        throw new InvalidOperationException(
          "oldText appears more than once; use a longer unique snippet, or set replaceAll to true.");
      }

      updated = string.Concat(current.AsSpan(0, first), replacement, current.AsSpan(first + oldText.Length));
    }

    Content = updated;
    Touch();
  }

  private void Touch()
  {
    UpdatedAt = DateTime.UtcNow;
  }

  private void ThrowIfDeleted()
  {
    if (DeletedAt.HasValue)
    {
      throw new InvalidOperationException("Cannot modify a deleted memory record.");
    }
  }
}
