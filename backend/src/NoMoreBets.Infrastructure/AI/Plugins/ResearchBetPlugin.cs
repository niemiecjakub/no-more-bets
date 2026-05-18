using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.AI.Plugins.Models;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class ResearchBetPlugin
{
  private const decimal ResearchStakeAmount = 10m;

  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
  };

  private readonly IUnitOfWork _unitOfWork;
  private readonly AgentSessionContext _agentSessionContext;
  private readonly int _matchId;
  private readonly ILogger<ResearchBetPlugin> _logger;

  public ResearchBetPlugin(
    int matchId,
    IUnitOfWork unitOfWork,
    AgentSessionContext agentSessionContext,
    ILogger<ResearchBetPlugin>? logger = null)
  {
    if (matchId <= 0)
    {
      throw new ArgumentException("matchId must be greater than zero.", nameof(matchId));
    }

    _unitOfWork = unitOfWork;
    _matchId = matchId;
    _agentSessionContext = agentSessionContext;
    _logger = logger ?? NullLogger<ResearchBetPlugin>.Instance;
  }

  [KernelFunction("PlaceBetSlip")]
  [Description("Paper / Research slip only: records a fictional prediction for this match. One selection is a single; multiple selections on one slip combine as a parlay.")]
  public async Task<string> PlaceBetSlip(
    [Description("JSON object with property betSelections: an array of selection objects. Each object must have: eventType (string, from GetCurrentOdds eventTypeName), option (string, from GetCurrentOdds option label). Example: {\"betSelections\":[{\"eventType\":\"bothTeamsToScore\",\"option\":\"bothTeamsToScore_Yes\"}]}")]
    string betSelectionsJson,
    CancellationToken cancellationToken = default)
  {
    List<ResearchBetSelectionRecord>? betSelections;
    try
    {
      var wrapper = JsonSerializer.Deserialize<PlaceResearchBetSlipArgs>(betSelectionsJson, SerializerOptions);
      betSelections = wrapper?.BetSelections;
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Invalid bet selections JSON received while placing research bet slip.");
      throw new ArgumentException(
        "Invalid betSelections JSON. Expected object with betSelections array of { eventType (enum name), option (BettingEventOption enum name) }.",
        nameof(betSelectionsJson),
        ex);
    }

    if (betSelections is null || betSelections.Count == 0)
    {
      _logger.LogError("No bet selections provided while placing a research bet slip.");
      throw new ArgumentException("At least one selection is required to place a research bet slip.", nameof(betSelectionsJson));
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
        throw new InvalidOperationException(
          $"Current odds not found for match {_matchId}, event {record.EventType}, option {record.Option}.");
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

  [KernelFunction("GetMatchEvents")]
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

  [KernelFunction("GetMatchBasicInfo")]
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

  private sealed record PlaceResearchBetSlipArgs(List<ResearchBetSelectionRecord> BetSelections);
  private sealed record ResearchBetSelectionRecord(BettingEventType EventType, BettingEventOption Option);
}

public record MatchBasicInfo(
  int MatchId,
  int HomeClubId,
  string HomeClubName,
  int AwayClubId,
  string AwayClubName);
