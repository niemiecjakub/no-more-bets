using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Tools.Implementations;

public class DailySlipTool
{
  private const decimal StakeAmount = 10m;

  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
  };

  private readonly IUnitOfWork _unitOfWork;
  private readonly AgentSessionContext _agentSessionContext;
  private readonly ILogger<DailySlipTool> _logger;

  public DailySlipTool(
    IUnitOfWork unitOfWork,
    AgentSessionContext agentSessionContext,
    ILogger<DailySlipTool>? logger = null)
  {
    _unitOfWork = unitOfWork;
    _agentSessionContext = agentSessionContext;
    _logger = logger ?? NullLogger<DailySlipTool>.Instance;
  }

  [Description("Paper daily slip: places one slip for the given risk level. Stake is always 10. Call once per risk (Low, Medium, High). Skip a tier rather than inventing a filler slip.")]
  public async Task<string> PlaceBetSlip(
    [Description("Risk tier for this slip: Low, Medium, or High. One slip per tier per day.")]
    BetRiskLevel riskLevel,
    [Description("JSON object with property betSelections: an array of selection objects. Each object must have: matchId (int, from GetAvailableMatches), eventType (string, from GetCurrentOdds eventTypeName), eventOption (string, from GetCurrentOdds option label).")]
    string betSelectionsJson,
    [Description("Short note on the edge behind this bet.")]
    string rationale,
    [Description("Honest estimated probability (0-1, exclusive) that this whole slip wins.")]
    decimal estimatedWinProbability,
    CancellationToken cancellationToken = default)
  {
    if (!Enum.IsDefined(riskLevel))
    {
      return "riskLevel must be Low, Medium, or High.";
    }

    if (string.IsNullOrWhiteSpace(rationale))
    {
      return "rationale is required.";
    }

    if (estimatedWinProbability is <= 0m or >= 1m)
    {
      return "estimatedWinProbability must be between 0 and 1 (exclusive).";
    }

    List<BetSelectionRecord>? betSelections;
    try
    {
      var wrapper = JsonSerializer.Deserialize<PlaceBetSlipArgs>(betSelectionsJson, SerializerOptions);
      betSelections = wrapper?.BetSelections;
    }
    catch (JsonException)
    {
      return "Invalid betSelections JSON. Expected object with betSelections array of { matchId, eventType, eventOption }.";
    }

    if (betSelections is null || betSelections.Count == 0)
    {
      return "At least one selection is required to place a daily slip.";
    }

    var slipDate = WarsawCalendar.DateFromUtc(DateTime.UtcNow);
    var alreadyPlaced = await _unitOfWork.Betting
      .AnyDailyPickOnDateWithRiskAsync(slipDate, (int)riskLevel, cancellationToken)
      .ConfigureAwait(false);
    if (alreadyPlaced)
    {
      return $"A {riskLevel} daily pick already exists for {slipDate}.";
    }

    var selectionOdds = new List<decimal>(betSelections.Count);
    foreach (var record in betSelections)
    {
      var odds = await _unitOfWork.Betting
        .GetCurrentOddsForSelectionAsync(record.MatchId, record.EventType, record.EventOption, cancellationToken)
        .ConfigureAwait(false);
      if (odds is null)
      {
        _logger.LogWarning(
          "Current odds not found while placing daily slip. MatchId={MatchId}, EventType={EventType}, EventOption={EventOption}",
          record.MatchId,
          record.EventType,
          record.EventOption);
        return
          $"Current odds not found for match {record.MatchId}, event {record.EventType}, option {record.EventOption}.";
      }

      selectionOdds.Add(odds.Value);
    }

    var totalOdds = selectionOdds.Aggregate(1m, (acc, o) => acc * o);
    var betSlip = new BetSlip
    {
      AgentSessionId = _agentSessionContext.SessionId,
      StakeAmount = StakeAmount,
      TotalOdds = totalOdds,
      PotentialPayout = StakeAmount * totalOdds,
      Rationale = rationale.Trim(),
      EstimatedWinProbability = estimatedWinProbability,
      StatusId = (int)BetStatus.Pending,
      CreatedAt = DateTime.UtcNow,
      Selections = new List<BetSelection>(),
      DailyPick = new DailyPick
      {
        RiskLevelId = (int)riskLevel,
        SlipDate = slipDate
      }
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
        BetStatus = BetStatus.Pending
      });
    }

    await _unitOfWork.Betting.AddBetSlipAsync(betSlip, cancellationToken).ConfigureAwait(false);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "Daily slip placed successfully.";
  }

  private sealed record PlaceBetSlipArgs(List<BetSelectionRecord>? BetSelections);
}
