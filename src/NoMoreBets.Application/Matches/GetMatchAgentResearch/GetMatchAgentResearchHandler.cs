using MediatR;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.GetMatchAgentResearch;

public record GetMatchAgentResearchQuery(int MatchId) : IRequest<string?>;

public sealed class GetMatchAgentResearchHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMatchAgentResearchQuery, string?>
{
  public async Task<string?> Handle(GetMatchAgentResearchQuery request, CancellationToken cancellationToken)
  {
    var analysis = await unitOfWork.Matches
      .GetLatestMatchAnalysisByCodeAsync(request.MatchId, MatchAnalysis.ResearchCode, cancellationToken)
      .ConfigureAwait(false);

    if (analysis == null)
      return null;

    var text = analysis.GetAgentResearch();
    return string.IsNullOrEmpty(text) ? null : text;
  }
}
