using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Features.Rotowire.GetRotowireLineups.Dtos;

/// <summary>
/// Shared DTO for injury status (code + full name).
/// </summary>
/// <param name="Code">Status code (e.g. OUT, QUES, SUS).</param>
/// <param name="FullName">Full display name (e.g. Out, Questionable).</param>
public record InjuryStatusDto(string Code, string FullName)
{
  /// <summary>Build from a code string; full name is resolved via the status map, or the code if unknown.</summary>
  public static InjuryStatusDto FromCode(string? code)
  {
    if (string.IsNullOrWhiteSpace(code))
      return new InjuryStatusDto("Unknown", "Unknown");
    var trimmed = code.Trim();
    if (!InjuryStatuses.TryParseFromCode(trimmed, out var status))
      return new InjuryStatusDto(trimmed, trimmed);
    return From(status);
  }

  /// <summary>Build from a domain <see cref="InjuryStatus"/>.</summary>
  public static InjuryStatusDto From(InjuryStatus status)
  {
    return new InjuryStatusDto(
        InjuryStatuses.GetCode(status),
        InjuryStatuses.GetFullName(status));
  }
}
