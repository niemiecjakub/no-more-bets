using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.Betting.CancelBetSlip;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Betting.GetMatchBettingOdds;
using NoMoreBets.Application.Betting.GetMatchesAvailableForBetting;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.AI.Tools.Implementations.Models;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Tools.Implementations;

public class BettingTool
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
  };

  private readonly IUnitOfWork _unitOfWork;
  private readonly IMediator _mediator;
  private readonly AgentSessionContext _agentSessionContext;
  private readonly ILogger<BettingTool> _logger;

  public BettingTool(IUnitOfWork unitOfWork, IMediator mediator, AgentSessionContext agentSessionContext, ILogger<BettingTool>? logger = null)
  {
    _unitOfWork = unitOfWork;
    _mediator = mediator;
    _agentSessionContext = agentSessionContext;
    _logger = logger ?? NullLogger<BettingTool>.Instance;
  }

  [Description("Retrieves matches for which bets can currently be placed.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetAvailableMatchesAsync(CancellationToken cancellationToken = default)
  {
    var matches = await _mediator
      .Send(new GetMatchesAvailableForBettingQuery(), cancellationToken)
      .ConfigureAwait(false);
    return matches
      .Select(m => new AvailableMatch(m.Id, m.HomeClub.Name, m.AwayClub.Name, m.MatchDate))
      .ToList();
  }

  [Description("Returns current odds for the match. By default returns compact markets (1X2, BTTS, double chance, O/U goals). Set includeExoticMarkets true only when you need handicap or exact-score lines.")]
  public Task<IReadOnlyList<CurrentOddsMarket>> GetCurrentOddsAsync(
    int matchId,
    [Description("When false (default), omits Handicap and ExactScore markets to save tokens. Set true only if your intended slip uses those markets.")]
    bool includeExoticMarkets = false,
    CancellationToken cancellationToken = default)
  {
    return _mediator.Send(new GetMatchBettingOddsQuery(matchId, includeExoticMarkets), cancellationToken);
  }

  [Description("Returns current odds for a single market on the match.")]
  public async Task<IReadOnlyList<CurrentOddsMarket>> GetCurrentOddsForMarketAsync(
    int matchId,
    [Description("Market to return: MatchResult, BothTeamsToScore, DoubleChance, OverUnderGoals, Handicap, or ExactScore.")]
    BettingEventType market,
    CancellationToken cancellationToken = default)
  {
    var markets = await _mediator
      .Send(new GetMatchBettingOddsQuery(matchId, true), cancellationToken)
      .ConfigureAwait(false);
    return markets.Where(m => m.EventTypeId == (int)market).ToList();
  }

  [Description("Returns structured match analysis for the given match.")]
  public async Task<MatchResearchOutput?> GetMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _unitOfWork.Matches.GetLatestMatchAnalysisAsync(matchId, cancellationToken).ConfigureAwait(false);
    return analysis?.TryGetAgentResearchOutput();
  }

  [Description("Places one bet slip per call. One selection is a single bet; multiple selections combine as a parlay on that slip. Call once per slip; you may call multiple times for multiple separate slips.")]
  public async Task<string> PlaceBetSlip(
    [Description("Stake in currency units. Required; must not exceed GetCurrentBalance (call GetCurrentBalance first).")]
    decimal stakeAmount,
    [Description("JSON object with property betSelections: an array of selection objects. Each object must have: matchId (int, from GetAvailableMatches), eventType (string, from GetCurrentOdds eventTypeName), eventOption (string, from GetCurrentOdds option label). Example: {\"betSelections\":[{\"matchId\":39,\"eventType\":\"bothTeamsToScore\",\"eventOption\":\"bothTeamsToScore_Yes\"}]}")]
    string betSelectionsJson,
    [Description("Short public note on the edge behind this bet. Locked with the slip and reviewed against the outcome during reflection.")]
    string rationale,
    [Description("Your honest estimated probability (0-1, exclusive) that this whole slip wins. Locked with the slip; used to score your calibration over time.")]
    decimal estimatedWinProbability,
    CancellationToken cancellationToken = default)
  {
    if (stakeAmount <= 0m)
    {
      _logger.LogWarning("Invalid stake amount {StakeAmount} while placing a bet slip.", stakeAmount);
      throw new ArgumentException("stakeAmount must be greater than zero.", nameof(stakeAmount));
    }

    if (string.IsNullOrWhiteSpace(rationale))
    {
      _logger.LogWarning("Missing rationale while placing a bet slip.");
      throw new ArgumentException("rationale is required.", nameof(rationale));
    }

    if (estimatedWinProbability is <= 0m or >= 1m)
    {
      _logger.LogWarning("Invalid estimated win probability {Probability} while placing a bet slip.", estimatedWinProbability);
      throw new ArgumentException("estimatedWinProbability must be between 0 and 1 (exclusive).", nameof(estimatedWinProbability));
    }

    List<BetSelectionRecord>? betSelections;
    try
    {
      var wrapper = JsonSerializer.Deserialize<PlaceBetSlipArgs>(betSelectionsJson, SerializerOptions);
      betSelections = wrapper?.BetSelections;
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Invalid bet selections JSON received while placing bet slip.");
      throw new ArgumentException("Invalid betSelections JSON. Expected object with betSelections array of { matchId (int), eventType (enum name), eventOption (BettingEventOption enum name) }.", nameof(betSelectionsJson), ex);
    }

    if (betSelections is null || betSelections.Count == 0)
    {
      _logger.LogError("No bet selections provided while placing a bet slip.");
      throw new ArgumentException("At least one selection is required to place a bet slip.", nameof(betSelectionsJson));
    }

    var balance = await _unitOfWork.Bankroll.GetCurrentBalanceAsync(cancellationToken).ConfigureAwait(false);

    if (stakeAmount > balance)
    {
      _logger.LogError("Stake amount {StakeAmount} exceeds current balance {Balance}.", stakeAmount, balance);
      throw new ArgumentException($"stakeAmount ({stakeAmount}) cannot exceed the current bankroll balance ({balance}).", nameof(stakeAmount));
    }

    var selectionOdds = new List<decimal>(betSelections.Count);
    foreach (var record in betSelections)
    {
      var odds = await _unitOfWork.Betting.GetCurrentOddsForSelectionAsync(record.MatchId, record.EventType, record.EventOption, cancellationToken).ConfigureAwait(false);
      if (odds is null)
      {
        _logger.LogWarning(
          "Current odds not found while placing bet slip. MatchId={MatchId}, EventType={EventType}, EventOption={EventOption}",
          record.MatchId,
          record.EventType,
          record.EventOption);
        throw new InvalidOperationException($"Current odds not found for match {record.MatchId}, event {record.EventType}, option {record.EventOption}.");
      }
      selectionOdds.Add(odds.Value);
    }

    var totalOdds = selectionOdds.Aggregate(1m, (acc, o) => acc * o);
    var betSlip = new BetSlip
    {
      AgentSessionId = _agentSessionContext.SessionId,
      StakeAmount = stakeAmount,
      TotalOdds = totalOdds,
      PotentialPayout = stakeAmount * totalOdds,
      Rationale = rationale.Trim(),
      EstimatedWinProbability = estimatedWinProbability,
      StatusId = (int)BetStatus.Pending,
      CreatedAt = DateTime.UtcNow,
      Selections = new List<BetSelection>()
    };

    betSlip.Bankrolls.Add(Bankroll.Create("Bet stake", stakeAmount, BankrollFlow.Out));

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
    return "Bet slip placed successfully.";
  }

  [Description("Cancels a pending bet slip and refunds its stake. A slip can be canceled only when all of its selections are still pending.")]
  public async Task CancelBetSlipAsync(
    [Description("Identifier of the bet slip to cancel.")]
    int betSlipId,
    CancellationToken cancellationToken = default)
  {
    if (betSlipId <= 0)
    {
      _logger.LogWarning("Invalid betSlipId {BetSlipId} while canceling bet slip.", betSlipId);
      throw new ArgumentException("betSlipId must be greater than zero.", nameof(betSlipId));
    }

    try
    {
      await _mediator.Send(new CancelBetSlipCommand(betSlipId), cancellationToken).ConfigureAwait(false);
    }
    catch (KeyNotFoundException ex)
    {
      _logger.LogWarning(ex, "Bet slip {BetSlipId} not found during cancel request.", betSlipId);
      throw new InvalidOperationException($"Bet slip {betSlipId} was not found.", ex);
    }
    catch (InvalidOperationException ex)
    {
      _logger.LogWarning(ex, "Bet slip {BetSlipId} cannot be canceled in its current state.", betSlipId);
      throw;
    }
  }

  [Description("Returns bet slips, newest first. Optional status: Pending, Won, Lost — omit the argument to return slips in every status.")]
  public async Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAsync(
    [Description("Filter by slip status, or omit for all statuses.")] BetStatus? status = null,
    CancellationToken cancellationToken = default)
  {
    return await _mediator.Send(new GetBetSlipsQuery(status), cancellationToken).ConfigureAwait(false);
  }

  [Description("Returns settled bet slips (Won, Lost) created within the last N days, newest first.")]
  public async Task<IReadOnlyList<BetSlipSummary>> GetNonPendingBetSlipsFromLastDaysAsync(
    [Description("Number of days to look back from now; must be greater than zero.")]
    int lastDays,
    CancellationToken cancellationToken = default)
  {
    if (lastDays <= 0)
    {
      _logger.LogWarning("Invalid lastDays value {LastDays} for recent non-pending bet slips query.", lastDays);
      throw new ArgumentException("lastDays must be greater than zero.", nameof(lastDays));
    }

    return await _mediator
      .Send(new GetNonPendingBetSlipsRecentQuery(lastDays), cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlipSummary>> GetNonPendingBetSlipsUpdatedInLastDaysAsync(
    int lastDays,
    CancellationToken cancellationToken = default)
  {
    if (lastDays <= 0)
    {
      _logger.LogWarning("Invalid lastDays value {LastDays} for updated non-pending bet slips query.", lastDays);
      throw new ArgumentException("lastDays must be greater than zero.", nameof(lastDays));
    }

    var slips = await _unitOfWork.Betting
      .GetNonPendingBetSlipsUpdatedInLastDaysAsync(lastDays, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipSummaryMapper.ToSummaries(slips);
  }

  public async Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAwaitingReflectionAsync(
    CancellationToken cancellationToken = default)
  {
    var slips = await _unitOfWork.Betting
      .GetNonPendingBetSlipsAwaitingReflectionAsync(cancellationToken)
      .ConfigureAwait(false);

    return BetSlipSummaryMapper.ToSummaries(slips);
  }

  private sealed record PlaceBetSlipArgs(List<BetSelectionRecord>? BetSelections);
}
