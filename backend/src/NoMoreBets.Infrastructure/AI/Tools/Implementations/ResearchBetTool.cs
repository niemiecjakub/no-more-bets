using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.AI.Tools.Implementations.Models;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Tools.Implementations;

public class ResearchBetTool
{
  private const decimal ResearchStakeAmount = 10m;

  private readonly IUnitOfWork _unitOfWork;
  private readonly AgentSessionContext _agentSessionContext;
  private readonly int _matchId;
  private readonly ILogger<ResearchBetTool> _logger;

  public ResearchBetTool(
    int matchId,
    IUnitOfWork unitOfWork,
    AgentSessionContext agentSessionContext,
    ILogger<ResearchBetTool>? logger = null)
  {
    if (matchId <= 0)
    {
      throw new ArgumentException("matchId must be greater than zero.", nameof(matchId));
    }

    _unitOfWork = unitOfWork;
    _matchId = matchId;
    _agentSessionContext = agentSessionContext;
    _logger = logger ?? NullLogger<ResearchBetTool>.Instance;
  }

  [Description("Paper / Research slip only: records a fictional prediction for this match. One selection is a single; multiple selections on one slip combine as a parlay.")]
  public async Task<string> PlaceBetSlip(
    [Description("JSON object with property betSelections: an array of selection objects. Each object must have: eventType (string, from GetMatchEvents eventTypeName), eventOption (string, from GetMatchEvents options). Example: {\"betSelections\":[{\"eventType\":\"BothTeamsToScore\",\"eventOption\":\"BothTeamsToScore_Yes\"}]}")]
    string betSelectionsJson,
    CancellationToken cancellationToken = default)
  {
    if (!ResearchBetSlipJsonParser.TryParse(betSelectionsJson, out var betSelections, out var parseError))
    {
      _logger.LogWarning("Invalid bet selections JSON while placing research bet slip: {Error}", parseError);
      return parseError!;
    }

    var selectionOdds = new List<decimal>(betSelections.Count);
    foreach (var record in betSelections)
    {
      var odds = await _unitOfWork.Betting
        .GetCurrentOddsForSelectionAsync(_matchId, record.EventType, record.Option, cancellationToken)
        .ConfigureAwait(false);

      if (odds is null)
      {
        _logger.LogWarning(
          "Current odds not found while placing research bet slip. MatchId={MatchId}, EventType={EventType}, Option={Option}",
          _matchId,
          record.EventType,
          record.Option);
        return
          $"Current odds not found for match {_matchId}, event {record.EventType}, option {record.Option}. "
          + "Call GetMatchEvents and use exact eventTypeName and option values from that response.";
      }

      selectionOdds.Add(odds.Value);
    }

    var totalOdds = selectionOdds.Aggregate(1m, (acc, o) => acc * o);
    var betSlip = new BetSlip
    {
      AgentSessionId = _agentSessionContext.SessionId,
      StakeAmount = ResearchStakeAmount,
      TotalOdds = totalOdds,
      PotentialPayout = ResearchStakeAmount * totalOdds,
      StatusId = (int)BetStatus.Pending,
      CreatedAt = DateTime.UtcNow,
      Selections = new List<BetSelection>()
    };

    for (var i = 0; i < betSelections.Count; i++)
    {
      var record = betSelections[i];
      betSlip.Selections.Add(new BetSelection
      {
        MatchId = _matchId,
        EventTypeId = (int)record.EventType,
        EventOptionId = (int)record.Option,
        OddsAtPlacement = selectionOdds[i],
        BetStatus = BetStatus.Pending
      });
    }

    await _unitOfWork.Betting.AddBetSlipAsync(betSlip, cancellationToken).ConfigureAwait(false);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "Research bet slip placed successfully.";
  }

  [Description("Lists available markets and outcome option names for this match.")]
  public async Task<IReadOnlyList<MatchEventMarket>> GetMatchEventsAsync(CancellationToken cancellationToken = default)
  {
    var snapshots = await _unitOfWork.Betting.GetBettingOddsSnapshotsForMatchAsync(_matchId, cancellationToken).ConfigureAwait(false);

    if (snapshots.Count == 0)
    {
      _logger.LogWarning("No current odds snapshots found for match {MatchId}.", _matchId);
      return Array.Empty<MatchEventMarket>();
    }

    var latest = snapshots[0];
    var byEventType = new Dictionary<int, (string Name, HashSet<string> Options)>();

    foreach (var row in latest.Rows)
    {
      var optionName = row.EventOptionEntity?.Name;
      if (string.IsNullOrEmpty(optionName))
      {
        _logger.LogWarning("Skipping event row with missing option for match {MatchId}. EventTypeId={EventTypeId}", _matchId, row.EventTypeId);
        continue;
      }

      if (!byEventType.TryGetValue(row.EventTypeId, out var bucket))
      {
        bucket = (row.EventTypeEntity.Name, new HashSet<string>(StringComparer.Ordinal));
        byEventType[row.EventTypeId] = bucket;
      }

      bucket.Options.Add(optionName);
    }

    return byEventType
      .OrderBy(kv => kv.Key)
      .Select(kv => new MatchEventMarket(kv.Key, kv.Value.Name, kv.Value.Options.OrderBy(o => o, StringComparer.Ordinal).ToList()))
      .ToList();
  }

  [Description("Returns basic information for this match: home/away club ids and names.")]
  public async Task<MatchBasicInfo> GetMatchBasicInfoAsync(CancellationToken cancellationToken = default)
  {
    var match = await _unitOfWork.Matches.GetMatchByIdAsync(_matchId, cancellationToken).ConfigureAwait(false)
      ?? throw new InvalidOperationException($"Match {_matchId} not found.");

    return new MatchBasicInfo(
      match.Id,
      match.HomeClubId,
      match.HomeClub.Name,
      match.AwayClubId,
      match.AwayClub.Name);
  }
}

public record MatchBasicInfo(
  int MatchId,
  int HomeClubId,
  string HomeClubName,
  int AwayClubId,
  string AwayClubName);
