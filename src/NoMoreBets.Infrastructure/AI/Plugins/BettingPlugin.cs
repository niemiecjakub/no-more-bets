using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Betting;
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
  private readonly IUnitOfWork _unitOfWork;

  public BettingPlugin(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
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


    foreach (var row in latest.Rows.Where(r => PluginConst.BettingOddsHistoryEventTypeWhitelist.Contains(r.EventType)))
    {
      BookmakerEvent? ev = JsonSerializer.Deserialize<BookmakerEvent>(row.EventJson, SerializerOptions);


      if (ev == null)
        continue;

      var options = ev.Options
        .Select(o => new CurrentOddsOption(o.Label, o.Odds))
        .ToList();

      markets.Add(new CurrentOddsMarket(
        row.EventTypeId,
        row.EventTypeEntity.Name,
        ev.Title,
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
    [Description("JSON object with a single property 'betSelections': an array of selection objects. Each object must have: MatchId (int, from GetAvailableMatches), EventType (string, one of: OverUnderGoals, TeamGoals, DoubleChance, BothTeamsToScore, MatchResult, Handicap, ExactScore), OutcomeKey (string, exact label from GetCurrentOdds for that market). Example: {\"betSelections\":[{\"MatchId\":39,\"EventType\":\"BothTeamsToScore\",\"OutcomeKey\":\"Tak\"}]}")]
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
      throw new ArgumentException("Invalid betSelections JSON. Expected object with betSelections array of { MatchId (int), EventType (enum name, e.g. BothTeamsToScore), OutcomeKey (string) }.", nameof(betSelectionsJson), ex);
    }

    if (betSelections is null || betSelections.Count == 0)
      throw new ArgumentException("At least one selection is required to place a bet slip.", nameof(betSelectionsJson));

    var selectionOdds = new List<decimal>(betSelections.Count);
    foreach (var record in betSelections)
    {
      var odds = await _unitOfWork.Betting.GetCurrentOddsForSelectionAsync(record.MatchId, record.EventType, record.OutcomeKey, cancellationToken).ConfigureAwait(false);
      if (odds is null)
        throw new InvalidOperationException($"Current odds not found for match {record.MatchId}, event {record.EventType}, outcome '{record.OutcomeKey}'.");
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
        OutcomeKey = record.OutcomeKey,
        OddsAtPlacement = selectionOdds[i],
        StatusId = (int)BetStatus.Pending
      });
    }

    await _unitOfWork.Betting.AddBetSlipAsync(betSlip, cancellationToken).ConfigureAwait(false);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
  }

  private sealed record PlaceBetSlipArgs(List<BetSelectionRecord>? BetSelections);
}
