using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Leagues;
using NoMoreBets.Application.Common.MatchMatcher;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Matches.UpdateMatchDetails;

/// <summary>Command to fetch Fotmob match details by URL and update or create the corresponding Match and MatchDetails.</summary>
public record UpdateMatchDetailsCommand(string FotmobGameUrl) : IRequest<UpdateMatchDetailsResult>;

/// <summary>Outcome of syncing Fotmob match details (e.g. whether a new match row was inserted).</summary>
public record UpdateMatchDetailsResult(bool CreatedNewMatch, int? MatchId = null);

/// <summary>Handles <see cref="UpdateMatchDetailsCommand"/>: fetches Fotmob details, finds or creates Match, and persists MatchDetails (and optional status/score update).</summary>
public class UpdateMatchDetailsHandler(
  IMatchDetailsProvider matchDetailsProvider,
  IMatchMatcher matchMatcher,
  IUnitOfWork unitOfWork,
  ILogger<UpdateMatchDetailsHandler> logger) : IRequestHandler<UpdateMatchDetailsCommand, UpdateMatchDetailsResult>
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly UpdateMatchDetailsResult NoChange = new(CreatedNewMatch: false);

  public async Task<UpdateMatchDetailsResult> Handle(UpdateMatchDetailsCommand request, CancellationToken cancellationToken)
  {
    var fotmobGameUrl = request.FotmobGameUrl?.Trim();
    if (string.IsNullOrWhiteSpace(fotmobGameUrl))
    {
      logger.LogWarning(
        "Handler {HandlerName} skipped: FotmobGameUrl is null or empty.",
        nameof(UpdateMatchDetailsHandler));
      return NoChange;
    }

    if (!Uri.TryCreate(fotmobGameUrl, UriKind.Absolute, out _))
    {
      logger.LogWarning(
        "Handler {HandlerName} skipped: FotmobGameUrl is not a valid absolute URL. Value: {FotmobGameUrl}",
        nameof(UpdateMatchDetailsHandler),
        fotmobGameUrl.Length > 200 ? fotmobGameUrl[..200] + "…" : fotmobGameUrl);
      return NoChange;
    }

    // Path A: existing match by FotmobUrl
    var existingDetails = await unitOfWork.Matches.GetMatchDetailsByFotmobUrlAsync(fotmobGameUrl, cancellationToken).ConfigureAwait(false);
    if (existingDetails != null)
    {
      logger.LogInformation(
        "Handler {HandlerName} match details for MatchId={MatchId} alredy exists.",
        nameof(UpdateMatchDetailsHandler),
        existingDetails.MatchId);
      return NoChange;
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
      return NoChange;
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
      return NoChange;
    }

    // Path C: insert new match under the Unknown league (Fotmob-discovered fixtures).
    var allClubs = (await unitOfWork.Clubs.GetClubs().ConfigureAwait(false)).ToList();
    var unknownLeague = (await unitOfWork.Leagues.GetLeagues().ConfigureAwait(false))
      .First(l => l.SoccerdataId == League.UnknownSoccerdataId);

    var clubsCreated = false;
    (Club homeClub, var homeCreated) = await ResolveOrCreateClubAsync(
      dto.HomeTeam, allClubs, unknownLeague.Id, cancellationToken).ConfigureAwait(false);
    clubsCreated |= homeCreated;
    (Club awayClub, var awayCreated) = await ResolveOrCreateClubAsync(
      dto.AwayTeam, allClubs, unknownLeague.Id, cancellationToken).ConfigureAwait(false);
    clubsCreated |= awayCreated;

    if (clubsCreated)
    {
      await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    var stage = await unitOfWork.Leagues.GetCurrentStage(League.UnknownSoccerdataId).ConfigureAwait(false);
    var newMatch = Match.CreateUpcomming(matchDate, stage.Id, homeClub.Id, awayClub.Id);
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
      "Handler {HandlerName} inserted new Match (Id={MatchId}) and MatchDetails for {Home} vs {Away} from Fotmob recent game URL {FotmobGameUrl}.",
      nameof(UpdateMatchDetailsHandler),
      newMatch.Id,
      dto.HomeTeam,
      dto.AwayTeam,
      fotmobGameUrl);
    return new UpdateMatchDetailsResult(CreatedNewMatch: true, MatchId: newMatch.Id);
  }

  private async Task<(Club Club, bool Created)> ResolveOrCreateClubAsync(
    string teamName,
    List<Club> allClubs,
    int unknownLeagueId,
    CancellationToken cancellationToken)
  {
    if (allClubs.Count > 0)
    {
      try
      {
        return (matchMatcher.FindClub(teamName, allClubs), false);
      }
      catch (ClubMatchNotFoundException)
      {
        // Create below.
      }
    }

    var trimmed = (teamName ?? string.Empty).Trim();
    var effectiveName = ClubNameMatchHints.ResolveEffectiveName(trimmed);
    var slug = EnsureUniqueSlug(ToSlug(effectiveName), allClubs);
    var soccerdataId = AllocateSyntheticSoccerdataId(effectiveName, allClubs);

    var club = new Club
    {
      Name = effectiveName,
      Slug = slug,
      LeagueId = unknownLeagueId,
      SoccerdataId = soccerdataId,
    };

    await unitOfWork.Clubs.AddClubAsync(club, cancellationToken).ConfigureAwait(false);
    allClubs.Add(club);

    logger.LogInformation(
      "Handler {HandlerName} created club '{ClubName}' (Slug={Slug}) in Unknown league for Fotmob team '{TeamName}'.",
      nameof(UpdateMatchDetailsHandler),
      effectiveName,
      slug,
      trimmed);

    return (club, true);
  }

  private static string ToSlug(string name)
  {
    var folded = ClubNameMatchHints.FoldDiacritics(name).ToLowerInvariant();
    var slug = System.Text.RegularExpressions.Regex.Replace(folded, @"[^a-z0-9]+", "-").Trim('-');
    return string.IsNullOrEmpty(slug) ? "unknown-club" : slug;
  }

  private static string EnsureUniqueSlug(string baseSlug, IReadOnlyList<Club> existing)
  {
    var existingSlugs = existing.Select(c => c.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
    if (!existingSlugs.Contains(baseSlug))
    {
      return baseSlug;
    }

    for (var i = 2; ; i++)
    {
      var candidate = $"{baseSlug}-{i}";
      if (!existingSlugs.Contains(candidate))
      {
        return candidate;
      }
    }
  }

  private static int AllocateSyntheticSoccerdataId(string name, IReadOnlyList<Club> existing)
  {
    var existingIds = existing.Select(c => c.SoccerdataId).ToHashSet();
    var candidate = unchecked(-Math.Abs(StringComparer.OrdinalIgnoreCase.GetHashCode(name)));
    while (existingIds.Contains(candidate))
    {
      candidate--;
    }

    return candidate;
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
