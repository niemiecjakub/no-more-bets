using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Clubs.UpdateDailySummary;

/// <summary>Command to refresh club daily summary from FotMob and insert when content differs from latest. Club is loaded by ID; Fotmob team is resolved by club name.</summary>
public record UpdateDailySummaryCommand(int ClubId, string Summary) : IRequest<Unit>;

public class UpdateDailySummaryHandler(
  IClubOverviewProvider clubOverviewProvider,
  IUnitOfWork unitOfWork,
  ILogger<UpdateDailySummaryHandler> logger) : IRequestHandler<UpdateDailySummaryCommand, Unit>
{
  public async Task<Unit> Handle(UpdateDailySummaryCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation(
      "Handling {HandlerName} for ClubId={ClubId}",
      nameof(UpdateDailySummaryHandler),
      request.ClubId);

    var club = await unitOfWork.Clubs.GetByIdAsync(request.ClubId, cancellationToken).ConfigureAwait(false);

    if (club == null)
    {
      logger.LogWarning(
        "Handler {HandlerName} found no club in DB for ClubId={ClubId}",
        nameof(UpdateDailySummaryHandler),
        request.ClubId);
      return Unit.Value;
    }


    var latest = await unitOfWork.Clubs.GetDailySummaryAsync(club.Id, null, cancellationToken).ConfigureAwait(false);

    if (latest?.Summary == request.Summary)
    {
      logger.LogInformation(
        "Handler {HandlerName} skipping insert: daily summary unchanged for ClubId={ClubId}",
        nameof(UpdateDailySummaryHandler),
        club.Id);
      return Unit.Value;
    }

    var entity = new ClubDailySummary
    {
      ClubId = club.Id,
      Date = DateOnly.FromDateTime(DateTime.UtcNow),
      Summary = request.Summary
    };
    await unitOfWork.Clubs.AddDailySummaryAsync(entity, cancellationToken).ConfigureAwait(false);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation(
      "Handler {HandlerName} inserted new daily summary for ClubId={ClubId}, Date={Date}",
      nameof(UpdateDailySummaryHandler),
      club.Id,
      entity.Date);

    return Unit.Value;
  }
}
