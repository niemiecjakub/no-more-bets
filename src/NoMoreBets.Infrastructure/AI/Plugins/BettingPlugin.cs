using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Matches.Dto;
using NoMoreBets.Infrastructure.AI.Plugins.Models;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class BettingPlugin
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
  {
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
  };

  private readonly IUnitOfWork _unitOfWork;
  private readonly IMediator _mediator;
  private readonly IAgentSessionContext _agentSessionContext;

  public BettingPlugin(IUnitOfWork unitOfWork, IMediator mediator, IAgentSessionContext agentSessionContext)
  {
    _unitOfWork = unitOfWork;
    _mediator = mediator;
    _agentSessionContext = agentSessionContext;
  }

  [KernelFunction("GetAvailableMatches")]
  [Description("Retrieves matches for which bets can currently be placed.")]
  public async Task<IReadOnlyList<AvailableMatch>> GetAvailableMatchesAsync(CancellationToken cancellationToken = default)
  {
    var matches = await _unitOfWork.Betting.GetMatchesAvailableForBettingAsync(cancellationToken).ConfigureAwait(false);
    return matches
      .Select(m => new AvailableMatch(m.Id, m.HomeClub.Name, m.AwayClub.Name, m.MatchDate))
      .ToList();
  }

  [KernelFunction("GetCurrentOdds")]
  [Description("Returns the current betting odds for the given match.")]
  public async Task<IReadOnlyList<CurrentOddsMarket>> GetCurrentOddsAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var snapshots = await _unitOfWork.Betting.GetBettingOddsSnapshotsForMatchAsync(matchId, cancellationToken).ConfigureAwait(false);
    if (snapshots.Count == 0)
    {
      return Array.Empty<CurrentOddsMarket>();
    }

    var latest = snapshots[0];
    var markets = new List<CurrentOddsMarket>(latest.Rows.Count);


    foreach (var row in latest.Rows)
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
  [Description("Returns structured match analysis for the given match.")]
  public async Task<StructuredMatchAnalysis?> GetMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _unitOfWork.Matches.GetLatestMatchAnalysisAsync(matchId, cancellationToken).ConfigureAwait(false);
    return analysis?.GetAnalysis();
  }

  [KernelFunction("PlaceBetSlip")]
  [Description("Places one bet slip per call. One selection is a single bet; multiple selections combine as a parlay on that slip. Call once per slip; you may call multiple times for multiple separate slips.")]
  public async Task PlaceBetSlip(
    [Description("Stake in currency units. Required; must be greater than zero and must not exceed GetCurrentBalance (call GetCurrentBalance first).")]
    decimal stakeAmount,
    [Description("JSON object with property betSelections: an array of selection objects. Each object must have: matchId (int, from GetAvailableMatches), eventType (string enum name), eventOption (string BettingEventOption enum name). Example: {\"betSelections\":[{\"matchId\":39,\"eventType\":\"bothTeamsToScore\",\"eventOption\":\"bothTeamsToScore_Yes\"}]}")]
    string betSelectionsJson,
    CancellationToken cancellationToken = default)
  {
    if (stakeAmount <= 0m)
      throw new ArgumentException("stakeAmount must be greater than zero.", nameof(stakeAmount));

    List<BetSelectionRecord>? betSelections;
    try
    {
      var wrapper = JsonSerializer.Deserialize<PlaceBetSlipArgs>(betSelectionsJson, SerializerOptions);
      betSelections = wrapper?.BetSelections;
    }
    catch (JsonException ex)
    {
      throw new ArgumentException("Invalid betSelections JSON. Expected object with betSelections array of { matchId (int), eventType (enum name), eventOption (BettingEventOption enum name) }.", nameof(betSelectionsJson), ex);
    }

    if (betSelections is null || betSelections.Count == 0)
      throw new ArgumentException("At least one selection is required to place a bet slip.", nameof(betSelectionsJson));

    var balance = await _unitOfWork.Bankroll.GetCurrentBalanceAsync(cancellationToken).ConfigureAwait(false);
    if (stakeAmount > balance)
      throw new ArgumentException($"stakeAmount ({stakeAmount}) cannot exceed the current bankroll balance ({balance}).", nameof(stakeAmount));

    var selectionOdds = new List<decimal>(betSelections.Count);
    foreach (var record in betSelections)
    {
      var odds = await _unitOfWork.Betting.GetCurrentOddsForSelectionAsync(record.MatchId, record.EventType, record.EventOption, cancellationToken).ConfigureAwait(false);
      if (odds is null)
        throw new InvalidOperationException($"Current odds not found for match {record.MatchId}, event {record.EventType}, option {record.EventOption}.");
      selectionOdds.Add(odds.Value);
    }

    var totalOdds = selectionOdds.Aggregate(1m, (acc, o) => acc * o);
    var betSlip = new BetSlip
    {
      AgentSessionId = _agentSessionContext.SessionId,
      StakeAmount = stakeAmount,
      TotalOdds = totalOdds,
      PotentialPayout = stakeAmount * totalOdds,
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
  }

  [KernelFunction("GetBetSlips")]
  [Description("Returns bet slips, newest first. Optional status: Pending, Won, Lost — omit the argument to return slips in every status.")]
  public Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAsync(
    [Description("Filter by slip status, or omit for all statuses.")] BetStatus? status = null,
    CancellationToken cancellationToken = default) =>
    _mediator.Send(new GetBetSlipsQuery(status), cancellationToken);

  [KernelFunction]
  [Description("Returns settled bet slips (Won, Lost) created within the last N days, newest first.")]
  public async Task<IReadOnlyList<BetSlipSummary>> GetNonPendingBetSlipsFromLastDaysAsync(
    [Description("Number of days to look back from now; must be greater than zero.")]
    int lastDays,
    CancellationToken cancellationToken = default)
  {
    if (lastDays <= 0)
      throw new ArgumentException("lastDays must be greater than zero.", nameof(lastDays));

    return await _mediator
      .Send(new GetNonPendingBetSlipsRecentQuery(lastDays), cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<IReadOnlyList<BetSlipSummary>> GetNonPendingBetSlipsUpdatedInLastDaysAsync(
    int lastDays,
    CancellationToken cancellationToken = default)
  {
    if (lastDays <= 0)
      throw new ArgumentException("lastDays must be greater than zero.", nameof(lastDays));

    var slips = await _unitOfWork.Betting
      .GetNonPendingBetSlipsUpdatedInLastDaysAsync(lastDays, cancellationToken)
      .ConfigureAwait(false);

    return BetSlipSummaryMapper.ToSummaries(slips);
  }

  private sealed record PlaceBetSlipArgs(List<BetSelectionRecord>? BetSelections);
}
