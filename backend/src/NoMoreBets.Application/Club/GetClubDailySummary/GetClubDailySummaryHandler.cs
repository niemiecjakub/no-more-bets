using MediatR;
using NoMoreBets.Domain.Clubs;
using Microsoft.Extensions.Logging;

namespace NoMoreBets.Application.Clubs.GetClubDailySummary;

public record GetClubDailySummaryQuery(int ClubId, DateOnly? Date = null) : IRequest<string?>;

public sealed class GetClubDailySummaryHandler(IClubRepository clubs, ILogger<GetClubDailySummaryHandler>? logger = null) : IRequestHandler<GetClubDailySummaryQuery, string?>
{
  public async Task<string?> Handle(GetClubDailySummaryQuery request, CancellationToken cancellationToken)
  {
    var summary = await clubs.GetDailySummaryAsync(request.ClubId, request.Date, cancellationToken).ConfigureAwait(false);
    if (summary == null)
    {
      logger?.LogWarning("No daily summary found for club {ClubId} on date {Date}.", request.ClubId, request.Date);
    }

    return summary?.ToString() ?? "No daily summary available.";
  }
}
