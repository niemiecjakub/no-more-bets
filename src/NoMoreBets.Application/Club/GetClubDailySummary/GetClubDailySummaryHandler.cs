using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Clubs.GetClubDailySummary;

public record GetClubDailySummaryQuery(int ClubId) : IRequest<string?>;

public sealed class GetClubDailySummaryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetClubDailySummaryQuery, string?>
{
  public async Task<string?> Handle(GetClubDailySummaryQuery request, CancellationToken cancellationToken)
  {
    var summary = await unitOfWork.Clubs.GetLatestDailySummaryAsync(request.ClubId, cancellationToken).ConfigureAwait(false);
    return summary?.ToString() ?? "No daily summary available.";
  }
}
