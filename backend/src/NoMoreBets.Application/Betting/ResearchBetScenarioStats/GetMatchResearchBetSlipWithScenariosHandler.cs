using MediatR;
using NoMoreBets.Application.Betting.GetMatchResearchBetSlip;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Betting.ResearchBetScenarioStats;

public record GetMatchResearchBetSlipWithScenariosQuery(int MatchId) : IRequest<MatchResearchBetSlipDto?>;

public sealed class GetMatchResearchBetSlipWithScenariosHandler(
  ISender sender,
  IResearchBetScenarioStatsService scenarioStats)
  : IRequestHandler<GetMatchResearchBetSlipWithScenariosQuery, MatchResearchBetSlipDto?>
{
  public async Task<MatchResearchBetSlipDto?> Handle(
    GetMatchResearchBetSlipWithScenariosQuery request,
    CancellationToken cancellationToken)
  {
    var slip = await sender
      .Send(new GetMatchResearchBetSlipQuery(request.MatchId), cancellationToken)
      .ConfigureAwait(false);

    if (slip is null)
    {
      return null;
    }

    // Hypothetical P&L is only meaningful once the slip has settled.
    var scenarios = slip.Status == BetStatus.Pending
      ? null
      : scenarioStats.FromSummary(slip);

    return new MatchResearchBetSlipDto(slip, scenarios);
  }
}
