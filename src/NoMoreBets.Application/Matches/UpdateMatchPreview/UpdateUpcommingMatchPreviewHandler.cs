using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;

/// <summary>Command to refresh a single match preview from SoccerData API and upsert into the database.</summary>
public record UpdateUpcommingMatchPreviewCommand(int SoccerdataMatchId) : IRequest<Unit>;

public class UpdateUpcommingMatchPreviewHandler(
  IMatchPreviewProvider matchPreviewProvider,
  IUnitOfWork unitOfWork,
  ILogger<UpdateUpcommingMatchPreviewHandler> logger) : IRequestHandler<UpdateUpcommingMatchPreviewCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public async Task<Unit> Handle(UpdateUpcommingMatchPreviewCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName} for Soccerdata match {SoccerdataMatchId}",
      nameof(UpdateUpcommingMatchPreviewHandler),
      request.SoccerdataMatchId);

    var matchPreview = await matchPreviewProvider.GetMatchPreviewAsync(request.SoccerdataMatchId);

    var match = await unitOfWork.Matches.GetMatchBySoccerdataId(request.SoccerdataMatchId);

    if (match == null)
    {
      logger.LogWarning(
        "Handler {HandlerName} found no match in DB for Soccerdata match {SoccerdataMatchId}",
        nameof(UpdateUpcommingMatchPreviewHandler),
        request.SoccerdataMatchId);
      return Unit.Value;
    }

    var previewContentJson = JsonSerializer.Serialize(matchPreview.PreviewContent, JsonOptions);
    var entity = await unitOfWork.Matches.GetMatchPreview(match.Id);

    if (entity == null)
    {
      entity = new MatchPreview
      {
        MatchId = match.Id,
        PreviewContentJson = previewContentJson
      };
      await unitOfWork.Matches.AddMatchPreview(entity);

      logger.LogInformation(
        "Handler {HandlerName} created new match preview for MatchId={MatchId}, SoccerdataMatchId={SoccerdataMatchId}",
        nameof(UpdateUpcommingMatchPreviewHandler),
        match.Id,
        request.SoccerdataMatchId);
    }
    else
    {
      entity.PreviewContentJson = previewContentJson;

      logger.LogInformation(
        "Handler {HandlerName} updated existing match preview for MatchId={MatchId}, SoccerdataMatchId={SoccerdataMatchId}",
        nameof(UpdateUpcommingMatchPreviewHandler),
        match.Id,
        request.SoccerdataMatchId);
    }

    await unitOfWork.SaveChangesAsync(cancellationToken);

    return Unit.Value;
  }
}
