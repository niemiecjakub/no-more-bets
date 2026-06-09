using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Matches.MatchExists;

public record MatchExistsQuery(int MatchId) : IRequest<bool>;

public sealed class MatchExistsHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<MatchExistsQuery, bool>
{
  public async Task<bool> Handle(MatchExistsQuery request, CancellationToken cancellationToken)
  {
    var match = await unitOfWork.Matches
      .GetMatchByIdAsync(request.MatchId, cancellationToken)
      .ConfigureAwait(false);
    return match != null;
  }
}
