using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.UpdateMatchDetails;

/// <summary>Command to fetch Fotmob match details by URL and update or create the corresponding Match and MatchDetails.</summary>
public record UpdateMatchDetailsCommand(string FotmobGameUrl) : IRequest<Unit>;

/// <summary>Handles <see cref="UpdateMatchDetailsCommand"/>: fetches Fotmob details, finds or creates Match, and persists MatchDetails (and optional status/score update).</summary>
public class UpdateMatchDetailsHandler(
  IMatchDetailsProvider matchDetailsProvider,
  IMatchMatcher matchMatcher,
  IUnitOfWork unitOfWork,
  ILogger<UpdateMatchDetailsHandler> logger) : IRequestHandler<UpdateMatchDetailsCommand, Unit>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  private const int LeagueId = 1;
  private const int StageId = 1;

  public async Task<Unit> Handle(UpdateMatchDetailsCommand request, CancellationToken cancellationToken)
  {
    var fotmobGameUrl = request.FotmobGameUrl?.Trim();
    if (string.IsNullOrWhiteSpace(fotmobGameUrl))
    {
      logger.LogWarning(
        "Handler {HandlerName} skipped: FotmobGameUrl is null or empty.",
        nameof(UpdateMatchDetailsHandler));
      return Unit.Value;
    }

    if (!Uri.TryCreate(fotmobGameUrl, UriKind.Absolute, out _))
    {
      logger.LogWarning(
        "Handler {HandlerName} skipped: FotmobGameUrl is not a valid absolute URL. Value: {FotmobGameUrl}",
        nameof(UpdateMatchDetailsHandler),
        fotmobGameUrl.Length > 200 ? fotmobGameUrl[..200] + "…" : fotmobGameUrl);
      return Unit.Value;
    }

    // Path A: existing match by FotmobUrl
    var existingDetails = await unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(fotmobGameUrl, cancellationToken).ConfigureAwait(false);
    if (existingDetails != null)
    {
      logger.LogInformation(
        "Handler {HandlerName} match details for MatchId={MatchId} alredy exists.",
        nameof(UpdateMatchDetailsHandler),
        existingDetails.MatchId);
      return Unit.Value;
    }

    MatchDetailsDto dto;
    try
    {
      dto = await matchDetailsProvider.GetMatchDetailsAsync(fotmobGameUrl, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
      logger.LogError(ex,
        "Handler {HandlerName} failed to fetch match details from Fotmob for URL {FotmobGameUrl}.",
        nameof(UpdateMatchDetailsHandler),
        fotmobGameUrl);
      throw;
    }

    var payload = new FotmobDetailsPayload(dto.HomeLineup, dto.AwayLineup, dto.Statistics, dto.Players);
    var json = JsonSerializer.Serialize(payload, JsonOptions);

    // Path B: find match by teams + date
    if (!dto.MatchDate.HasValue)
    {
      logger.LogWarning(
        "Handler {HandlerName} cannot search or insert match: MatchDate is missing for {Home} vs {Away}, FotmobGameUrl={FotmobGameUrl}.",
        nameof(UpdateMatchDetailsHandler),
        dto.HomeTeam,
        dto.AwayTeam,
        fotmobGameUrl);
      return Unit.Value;
    }

    var matchDate = dto.MatchDate.Value.UtcDateTime;
    var matchesOnDay = await unitOfWork.Matches.GetMatches(matchDate).ConfigureAwait(false);
    var candidates = matchesOnDay
      .Select(m => (m.HomeClub.Name, m.AwayClub.Name, m))
      .ToList();
    var matched = matchMatcher.FindBestMatch(dto.HomeTeam, dto.AwayTeam, candidates);

    if (matched != null)
    {
      var detailsForMatch = await unitOfWork.Matches.GetMatchDetailsByMatchIdAsync(matched.Id, cancellationToken).ConfigureAwait(false);
      if (detailsForMatch != null)
      {
        detailsForMatch.FotmobUrl = fotmobGameUrl;
        detailsForMatch.FotmobDetailsJson = json;
      }
      else
      {
        var newDetails = new MatchDetails
        {
          MatchId = matched.Id,
          FotmobUrl = fotmobGameUrl,
          FotmobDetailsJson = json
        };
        await unitOfWork.Matches.AddMatchDetailsAsync(newDetails, cancellationToken).ConfigureAwait(false);
      }
      ApplyStatusAndScoreIfUpcoming(matched, dto);
      await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
      logger.LogInformation(
        "Handler {HandlerName} linked MatchDetails to existing match by teams+date for MatchId={MatchId}.",
        nameof(UpdateMatchDetailsHandler),
        matched.Id);
      return Unit.Value;
    }

    // Path C: insert new match
    var clubs = await unitOfWork.Clubs.GetClubs(LeagueId).ConfigureAwait(false);
    Club homeClub;
    Club awayClub;
    try
    {
      homeClub = matchMatcher.FindClub(dto.HomeTeam, clubs);
      awayClub = matchMatcher.FindClub(dto.AwayTeam, clubs);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex,
        "Handler {HandlerName} could not resolve clubs for insert ({Home} vs {Away}); skipping insert.",
        nameof(UpdateMatchDetailsHandler),
        dto.HomeTeam,
        dto.AwayTeam);
      return Unit.Value;
    }

    var newMatch = Match.CreateUpcomming(matchDate, StageId, homeClub.Id, awayClub.Id);
    await unitOfWork.Matches.AddMatch(newMatch, cancellationToken).ConfigureAwait(false);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    var matchDetails = new MatchDetails
    {
      MatchId = newMatch.Id,
      FotmobUrl = fotmobGameUrl,
      FotmobDetailsJson = json
    };
    await unitOfWork.Matches.AddMatchDetailsAsync(matchDetails, cancellationToken).ConfigureAwait(false);
    ApplyStatusAndScoreIfUpcoming(newMatch, dto);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    logger.LogInformation(
      "Handler {HandlerName} inserted new Match (Id={MatchId}) and MatchDetails for {Home} vs {Away}.",
      nameof(UpdateMatchDetailsHandler),
      newMatch.Id,
      dto.HomeTeam,
      dto.AwayTeam);
    return Unit.Value;
  }

  private static void ApplyStatusAndScoreIfUpcoming(Match? match, MatchDetailsDto dto)
  {
    if (match == null)
      return;
    if (dto.HomeScore is null || dto.AwayScore is null)
      return;
    var scoreMissing = match.HomeGoals is null || match.AwayGoals is null;
    var isUpcoming = match.MatchStatus == MatchStatus.Upcomming;
    if (!isUpcoming && !scoreMissing)
      return;
    match.HomeGoals = dto.HomeScore;
    match.AwayGoals = dto.AwayScore;
    if (isUpcoming)
      match.MatchStatus = MatchStatus.Finished;
  }
}
