using System.ComponentModel;
using MediatR;
using ModelContextProtocol.Server;
using NoMoreBets.Application.Clubs.GetClubById;
using NoMoreBets.Application.Clubs.GetClubMatches;
using NoMoreBets.Application.Clubs.GetClubNextMatch;
using NoMoreBets.Application.Clubs.GetClubRecentGames;
using NoMoreBets.Application.Clubs.GetClubRollingPerformance;
using NoMoreBets.Application.Clubs.GetClubsList;
using NoMoreBets.Application.Matches.GetMatchesPage;

namespace NoMoreBets.Infrastructure.Mcp.Tools;

/// <summary>Read-only MCP adapters over the <c>Application.Clubs</c> slice.</summary>
[McpServerToolType]
public sealed class ClubsMcpTools(IMediator mediator)
{
  [McpServerTool(
    Name = "clubs_getList",
    Title = "List clubs",
    ReadOnly = true)]
  [Description("Returns all clubs ordered by name with their league/season memberships. Use it to resolve a club name into the clubId required by other tools.")]
  public Task<IReadOnlyList<ClubDto>> GetListAsync(CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetClubsListQuery(), cancellationToken);
  }

  [McpServerTool(
    Name = "clubs_getById",
    Title = "Look up a club",
    ReadOnly = true)]
  [Description("Returns a single club with its league/season memberships. Null when the club is unknown.")]
  public Task<ClubDetailDto?> GetByIdAsync(
    [Description("The club identifier.")] int clubId,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetClubByIdQuery(clubId), cancellationToken);
  }

  [McpServerTool(
    Name = "clubs_getMatches",
    Title = "List club matches",
    ReadOnly = true)]
  [Description("Returns all stored matches for a club (past and upcoming). Null when the club is unknown.")]
  public Task<IReadOnlyList<MatchDto>?> GetMatchesAsync(
    [Description("The club identifier.")] int clubId,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetClubMatchesQuery(clubId), cancellationToken);
  }

  [McpServerTool(
    Name = "clubs_getNextMatch",
    Title = "Find next fixture",
    ReadOnly = true)]
  [Description("Returns the club's next upcoming match. Null when the club is unknown or has no scheduled match.")]
  public Task<ClubNextMatchDto?> GetNextMatchAsync(
    [Description("The club identifier.")] int clubId,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetClubNextMatchQuery(clubId), cancellationToken);
  }

  [McpServerTool(
    Name = "clubs_getRecentGames",
    Title = "Review recent form",
    ReadOnly = true)]
  [Description("Returns the club's last 5 finished matches with opponent, score and result.")]
  public Task<IReadOnlyList<RecentMatch>?> GetRecentGamesAsync(
    [Description("The club identifier.")] int clubId,
    [Description("Optional cut-off date (yyyy-MM-dd); omit for the latest games.")] DateOnly? date = null,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetClubRecentGamesQuery(clubId, date), cancellationToken);
  }

  [McpServerTool(
    Name = "clubs_getRollingPerformance",
    Title = "Review recent performance",
    ReadOnly = true)]
  [Description("Returns player ratings, team ratings and formations aggregated over the club's last 5 finished matches.")]
  public Task<TeamPerformanceResult?> GetRollingPerformanceAsync(
    [Description("The club identifier.")] int clubId,
    [Description("Optional cut-off date (yyyy-MM-dd); omit for the latest games.")] DateOnly? date = null,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetClubRollingPerformanceQuery(clubId, date), cancellationToken);
  }
}
