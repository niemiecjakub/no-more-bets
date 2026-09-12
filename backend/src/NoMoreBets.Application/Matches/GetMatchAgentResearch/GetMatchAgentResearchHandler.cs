using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchAgentResearch;

public record GetMatchAgentResearchQuery(int MatchId) : IRequest<MatchResearchOutputDto?>;

public sealed class GetMatchAgentResearchHandler(IMatchRepository matches, ILogger<GetMatchAgentResearchHandler> logger)
  : IRequestHandler<GetMatchAgentResearchQuery, MatchResearchOutputDto?>
{
  public async Task<MatchResearchOutputDto?> Handle(GetMatchAgentResearchQuery request, CancellationToken cancellationToken)
  {
    var analysis = await matches
      .GetLatestMatchAnalysisByCodeAsync(request.MatchId, MatchAnalysis.StructuredResearchCode, cancellationToken)
      .ConfigureAwait(false);

    analysis ??= await matches
      .GetLatestMatchAnalysisByCodeAsync(request.MatchId, MatchAnalysis.ResearchCode, cancellationToken)
      .ConfigureAwait(false);

    if (analysis == null)
    {
      logger.LogError("Match research analysis is null for match {MatchId}.", request.MatchId);
      return null;
    }

    var output = analysis.TryGetAgentResearchOutput();
    if (output == null)
    {
      return null;
    }

    return new MatchResearchOutputDto(
      output.MatchOverview,
      output.KeyPoints,
      output.RisksAndUnknowns);
  }
}
