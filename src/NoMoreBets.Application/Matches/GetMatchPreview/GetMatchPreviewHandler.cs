using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Matches.GetMatchPreview;

public record GetMatchPreviewQuery(int MatchId) : IRequest<string?>;

public sealed class GetMatchPreviewHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMatchPreviewQuery, string?>
{
  public async Task<string?> Handle(GetMatchPreviewQuery request, CancellationToken cancellationToken)
  {
    var preview = await unitOfWork.Matches.GetMatchPreview(request.MatchId).ConfigureAwait(false);
    return preview?.BuildMarkdownPreview() ?? "No preview available.";
  }
}
