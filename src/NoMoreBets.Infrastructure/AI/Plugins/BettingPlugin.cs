using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class BettingPlugin
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
  };

  private static readonly HashSet<BettingEventType> BettingOddsHistoryEventTypeWhitelist = new()
  {
    BettingEventType.OverUnderGoals,
    BettingEventType.DoubleChance,
    BettingEventType.BothTeamsToScore,
    BettingEventType.MatchResult,
    BettingEventType.Handicap,
    BettingEventType.ExactScore,
  };

  private readonly IUnitOfWork _unitOfWork;
  private readonly IMediator _mediator;

  public BettingPlugin(IUnitOfWork unitOfWork, IMediator mediator)
  {
    _unitOfWork = unitOfWork;
    _mediator = mediator;
  }

  [KernelFunction("GetAvailableMatches")]
  [Description("Returns matches that are upcoming, have at least one betting odds snapshot, and have match analysis.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetAvailableMatchesAsync(CancellationToken cancellationToken = default)
  {
    var matches = await _unitOfWork.Betting.GetMatchesAvailableForBettingAsync(cancellationToken).ConfigureAwait(false);
    return matches
      .Select(m => new AvailableMatch(m.Id, m.HomeClub.Name, m.AwayClub.Name, m.MatchDate))
      .ToList();
  }

  [KernelFunction("GetCurrentOdds")]
  [Description("Returns the current betting odds from the latest snapshot for the given match.")]
  public async Task<IReadOnlyList<CurrentOddsMarket>> GetCurrentOddsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var snapshots = await _unitOfWork.Betting.GetBettingOddsSnapshotsForMatchAsync(matchId, cancellationToken).ConfigureAwait(false);
    if (snapshots.Count == 0)
    {
      return Array.Empty<CurrentOddsMarket>();
    }

    var latest = snapshots[0];
    var markets = new List<CurrentOddsMarket>(latest.Rows.Count);


    foreach (var row in latest.Rows.Where(r => BettingOddsHistoryEventTypeWhitelist.Contains(r.EventType)))
    {
      var outcomeName = row.EventOptionEntity?.Name;
      if (string.IsNullOrEmpty(outcomeName) || !row.Odds.HasValue)
        continue;

      var options = new List<CurrentOddsOption>
      {
        new(outcomeName, (double)row.Odds.Value)
      };

      markets.Add(new CurrentOddsMarket(
        row.EventTypeId,
        row.EventTypeEntity.Name,
        row.EventTypeEntity.Name,
        options));
    }

    return markets;
  }

  [KernelFunction("GetMatchAnalysis")]
  [Description("Returns the latest structured match analysis for the given match.")]
  public async Task<StructuredMatchAnalysis?> GetMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _unitOfWork.Matches.GetLatestMatchAnalysisAsync(matchId, cancellationToken).ConfigureAwait(false);
    return analysis?.GetAnalysis();
  }

  [KernelFunction("PlaceBetSlip")]
  [Description("Places a bet slip with one or more selections across one or more matches. Call this once you have finished analyzing all available matches and have selected the best value bets.")]
  public async Task PlaceBetSlip(
    [Description("JSON object with a single property 'betSelections': an array of selection objects. Each object must have: matchId (int, from GetAvailableMatches), eventType (string, one of: OverUnderGoals, DoubleChance, BothTeamsToScore, MatchResult, Handicap, ExactScore), eventOption (string, BettingEventOption enum name, e.g. bothTeamsToScore_Yes in camelCase or BothTeamsToScore_Yes). Example: {\"betSelections\":[{\"matchId\":39,\"eventType\":\"bothTeamsToScore\",\"eventOption\":\"bothTeamsToScore_Yes\"}]}")]
    string betSelectionsJson,
    CancellationToken cancellationToken = default)
  {
    List<BetSelectionRecord>? betSelections;
    try
    {
      // Agent sends full arguments object: {"betSelections":[...]}
      var wrapper = JsonSerializer.Deserialize<PlaceBetSlipArgs>(betSelectionsJson, SerializerOptions);
      betSelections = wrapper?.BetSelections;
    }
    catch (JsonException ex)
    {
      throw new ArgumentException("Invalid betSelections JSON. Expected object with betSelections array of { matchId (int), eventType (enum name), eventOption (BettingEventOption enum name) }.", nameof(betSelectionsJson), ex);
    }

    if (betSelections is null || betSelections.Count == 0)
      throw new ArgumentException("At least one selection is required to place a bet slip.", nameof(betSelectionsJson));

    var selectionOdds = new List<decimal>(betSelections.Count);
    foreach (var record in betSelections)
    {
      var odds = await _unitOfWork.Betting.GetCurrentOddsForSelectionAsync(record.MatchId, record.EventType, record.EventOption, cancellationToken).ConfigureAwait(false);
      if (odds is null)
        throw new InvalidOperationException($"Current odds not found for match {record.MatchId}, event {record.EventType}, option {record.EventOption}.");
      selectionOdds.Add(odds.Value);
    }

    var totalOdds = selectionOdds.Aggregate(1m, (acc, o) => acc * o);
    const decimal stakeAmount = 10m;
    var betSlip = new BetSlip
    {
      StakeAmount = stakeAmount,
      TotalOdds = totalOdds,
      PotentialPayout = stakeAmount * totalOdds,
      StatusId = (int)BetStatus.Pending,
      CreatedAt = DateTime.UtcNow,
      Selections = new List<BetSelection>()
    };

    for (var i = 0; i < betSelections.Count; i++)
    {
      var record = betSelections[i];
      betSlip.Selections.Add(new BetSelection
      {
        MatchId = record.MatchId,
        EventTypeId = (int)record.EventType,
        EventOptionId = (int)record.EventOption,
        OddsAtPlacement = selectionOdds[i],
        StatusId = (int)BetStatus.Pending
      });
    }

    await _unitOfWork.Betting.AddBetSlipAsync(betSlip, cancellationToken).ConfigureAwait(false);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction("GetBetSlips")]
  [Description("Returns bet slips, newest first. Optional status: Pending, Won, Lost, or CashedOut — omit the argument to return slips in every status.")]
  public Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAsync(
    [Description("Filter by slip status, or omit for all statuses.")] BetStatus? status = null,
    CancellationToken cancellationToken = default) =>
    _mediator.Send(new GetBetSlipsQuery(status), cancellationToken);

  private sealed record PlaceBetSlipArgs(List<BetSelectionRecord>? BetSelections);
}
