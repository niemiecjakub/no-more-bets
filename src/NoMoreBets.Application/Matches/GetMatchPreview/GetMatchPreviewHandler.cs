using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Matches.GetMatchPreview;

public record GetMatchPreviewQuery(int MatchId) : IRequest<string?>;

public sealed class GetMatchPreviewHandler(IUnitOfWork unitOfWork, ILogger<GetMatchPreviewHandler>? logger = null) : IRequestHandler<GetMatchPreviewQuery, string?>
{
  public async Task<string?> Handle(GetMatchPreviewQuery request, CancellationToken cancellationToken)
  {
    var preview = await unitOfWork.Matches.GetMatchPreview(request.MatchId).ConfigureAwait(false);
    if (preview == null)
    {
      logger?.LogWarning("No preview found for match {MatchId}.", request.MatchId);
    }

    return preview?.BuildMarkdownPreview() ?? "No preview available.";
  }
}
