namespace NoMoreBets.Domain.Feedback;

public class Feedback
{
  public const int MaxMessageLength = 2000;
  public const int MaxNameLength = 200;
  public const int MaxEmailLength = 320;

  private Feedback()
  {
  }

  public int Id { get; private set; }
  public string Message { get; private set; } = null!;
  public string? Name { get; private set; }
  public string? Email { get; private set; }
  public DateTime CreatedAt { get; private set; }

  public static Feedback Create(string message, string? name = null, string? email = null)
  {
    var trimmedMessage = (message ?? string.Empty).Trim();
    var trimmedName = NormalizeOptional(name);
    var trimmedEmail = NormalizeOptional(email);

    if (string.IsNullOrEmpty(trimmedMessage))
    {
      throw new ArgumentException("Message must not be empty.", nameof(message));
    }

    if (trimmedMessage.Length > MaxMessageLength)
    {
      throw new ArgumentException($"Message must be at most {MaxMessageLength} characters.", nameof(message));
    }

    if (trimmedName is { Length: > MaxNameLength })
    {
      throw new ArgumentException($"Name must be at most {MaxNameLength} characters.", nameof(name));
    }

    if (trimmedEmail is { Length: > MaxEmailLength })
    {
      throw new ArgumentException($"Email must be at most {MaxEmailLength} characters.", nameof(email));
    }

    if (trimmedEmail is not null && !IsValidEmail(trimmedEmail))
    {
      throw new ArgumentException("Email is not valid.", nameof(email));
    }

    return new Feedback
    {
      Message = trimmedMessage,
      Name = trimmedName,
      Email = trimmedEmail,
      CreatedAt = DateTime.UtcNow
    };
  }

  private static string? NormalizeOptional(string? value)
  {
    var trimmed = (value ?? string.Empty).Trim();
    return string.IsNullOrEmpty(trimmed) ? null : trimmed;
  }

  private static bool IsValidEmail(string email)
  {
    var at = email.IndexOf('@');
    return at > 0 && at == email.LastIndexOf('@') && at < email.Length - 1;
  }
}
