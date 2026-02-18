using System.ComponentModel;
using MediatR;
using Microsoft.SemanticKernel;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview;
using NoMoreBets.Features.Fotmob.GetFotmobClubOverview.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm;
using NoMoreBets.Features.Fotmob.GetFotmobClubRollingForm.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable;
using NoMoreBets.Features.Fotmob.GetFotmobLeagueTable.Dtos;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats;
using NoMoreBets.Features.Fotmob.GetFotmobXgStats.Dtos;
using NoMoreBets.Features.Fotmob.Scraping;
using NoMoreBets.Features.SoccerData.GetSoccerDataHeadToHead;
using NoMoreBets.Features.SoccerData.GetSoccerDataMatchPreview;
using NoMoreBets.Features.SoccerData.Model;

namespace NoMoreBets.Features.Prediction.Plugins;

/// <summary>
/// Plugin exposing football data context for quantitative analysis.
/// </summary>
public sealed class FootballDataPlugin(IMediator mediator)
{
  [KernelFunction("get_league_table")]
  [Description("Fetches league table data.")]
  public async Task<IEnumerable<ClubDto>> GetLeagueTableAsync(
      [Description("Table filter: all, home, away, form.")] TableFilter filter = TableFilter.All,
      CancellationToken cancellationToken = default)
  {
    return await mediator.Send(new GetFotmobLeagueTableQuery(filter), cancellationToken);
  }

  [KernelFunction("get_xg_stats")]
  [Description("Fetches xG statistics table.")]
  public async Task<IEnumerable<XgStatsDto>> GetXgStatsAsync(CancellationToken cancellationToken = default)
  {
    return await mediator.Send(new GetFotmobXgStatsQuery(), cancellationToken);
  }

  [KernelFunction("get_head_to_head")]
  [Description("Fetches head-to-head statistics.")]
  public async Task<HeadToHead?> GetHeadToHeadAsync(
      [Description("Home team SoccerData ID.")] int homeTeamId,
      [Description("Away team SoccerData ID.")] int awayTeamId,
      CancellationToken cancellationToken = default)
  {
    await mediator.Send(new RefreshSoccerDataHeadToHeadCommand(homeTeamId, awayTeamId), cancellationToken);
    return await mediator.Send(new GetSoccerDataHeadToHeadQuery(homeTeamId, awayTeamId), cancellationToken);
  }

  [KernelFunction("get_club_overview")]
  [Description("Fetches Fotmob club overview for a team ID.")]
  public async Task<ClubOverviewDto> GetClubOverviewAsync(
      [Description("Fotmob team ID.")] int teamId,
      CancellationToken cancellationToken = default)
  {
    return await mediator.Send(new GetFotmobClubOverviewQuery(teamId), cancellationToken);
  }

  [KernelFunction("get_rolling_form")]
  [Description("Fetches rolling form for a team from Fotmob based on latest matches.")]
  public async Task<ClubRollingFormDto> GetRollingFormAsync(
      [Description("Fotmob team ID.")] int teamId,
      [Description("Team name used on match pages.")] string teamName,
      CancellationToken cancellationToken = default)
  {
    return await mediator.Send(new GetFotmobClubRollingFormQuery(teamId, teamName), cancellationToken);
  }

  [KernelFunction("get_match_preview")]
  [Description("Fetches match preview from the news.")]
  public async Task<IEnumerable<PreviewContentItem>> GetMatchPreviewAsync(
      [Description("Soccerdata match ID.")] int matchId,
      CancellationToken cancellationToken = default)
  {
    await mediator.Send(new RefreshSoccerDataMatchPreviewCommand(matchId), cancellationToken);
    var data = await mediator.Send(new GetSoccerDataMatchPreviewQuery(matchId), cancellationToken);
    return data?.PreviewContent ?? [];
  }
}
