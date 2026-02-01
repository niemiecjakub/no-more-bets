using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Common;

/// <summary>
/// Shared DTO for injury status (code + full name).
/// </summary>
/// <param name="Code">Status code (e.g. OUT, QUES, SUS).</param>
/// <param name="FullName">Full display name (e.g. Out, Questionable).</param>
public record StatusDto(string Code, string FullName)
{
  /// <summary>Build from a code string; full name is resolved via the status map, or the code if unknown.</summary>
  public static StatusDto FromCode(string? code)
  {
    if (string.IsNullOrWhiteSpace(code))
      return new StatusDto("Unknown", "Unknown");
    var trimmed = code.Trim();
    if (!InjuryStatuses.TryParseFromCode(trimmed, out var status))
      return new StatusDto(trimmed, trimmed);
    return From(status);
  }

  /// <summary>Build from a domain <see cref="InjuryStatus"/>.</summary>
  public static StatusDto From(InjuryStatus status)
  {
    return new StatusDto(
        InjuryStatuses.GetCode(status),
        InjuryStatuses.GetFullName(status));
  }
}
