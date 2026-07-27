using System.ComponentModel;
using MediatR;
using ModelContextProtocol.Server;
using NoMoreBets.Application.Leagues.GetClubLeagueStatistics;
using NoMoreBets.Application.Leagues.GetLeagueTable;
using NoMoreBets.Application.Leagues.GetLeaguesList;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;

namespace NoMoreBets.Infrastructure.Mcp.Tools;

/// <summary>Read-only MCP adapters over the <c>Application.Leagues</c> slice.</summary>
[McpServerToolType]
public sealed class LeaguesMcpTools(IMediator mediator)
{
  [McpServerTool(
    Name = "leagues_getList",
    Title = "List leagues",
    ReadOnly = true)]
  [Description("Returns all known leagues ordered by name (id, name, slug). Use it to resolve a league name into the leagueId required by other tools.")]
  public Task<IReadOnlyList<LeagueDto>> GetListAsync(CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetLeaguesListQuery(), cancellationToken);
  }

  [McpServerTool(
    Name = "leagues_getTable",
    Title = "View league table",
    ReadOnly = true)]
  [Description("Returns the standings for a league: one row per club with position, points, W/D/L, goals and expected metrics. Pass asOfDate to get the table as it stood on that date.")]
  public Task<IReadOnlyList<LeagueTableStanding>?> GetTableAsync(
    [Description("The league identifier (see leagues_getList).")] int leagueId,
    [Description("Optional cut-off date (yyyy-MM-dd); omit for the current table.")] DateOnly? asOfDate = null,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetLeagueTableQuery(leagueId, asOfDate), cancellationToken);
  }

  [McpServerTool(
    Name = "leagues_getClubStatistics",
    Title = "Check club league stats",
    ReadOnly = true)]
  [Description("Returns one club's league statistics: table position, points, W/D/L record, goals for/against and expected metrics.")]
  public Task<ClubLeagueStats?> GetClubStatisticsAsync(
    [Description("The club identifier (see clubs_getList).")] int clubId,
    [Description("Optional cut-off date (yyyy-MM-dd); omit for current statistics.")] DateOnly? date = null,
    [Description("Optional season identifier; omit for the club's current season.")] int? seasonId = null,
    CancellationToken cancellationToken = default)
  {
    return mediator.Send(new GetClubLeagueStatisticsQuery(clubId, date, seasonId), cancellationToken);
  }
}
