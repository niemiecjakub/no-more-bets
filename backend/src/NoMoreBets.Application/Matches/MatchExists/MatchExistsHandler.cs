using MediatR;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.MatchExists;

public record MatchExistsQuery(int MatchId) : IRequest<bool>;

public sealed class MatchExistsHandler(IMatchRepository matches)
  : IRequestHandler<MatchExistsQuery, bool>
{
  public async Task<bool> Handle(MatchExistsQuery request, CancellationToken cancellationToken)
  {
    var match = await matches
      .GetMatchByIdAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);
    return match != null;
  }
}
