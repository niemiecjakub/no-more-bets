using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Memories;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentBettingPlugin
{
  private readonly BettingPlugin _bettingPlugin;
  private readonly IUnitOfWork _unitOfWork;
  private readonly MemoriesPlugin _memoriesPlugin;
  private readonly SearchPlugin _searchPlugin;

  public AgentBettingPlugin(
    BettingPlugin bettingPlugin,
    IUnitOfWork unitOfWork,
    MemoriesPlugin memoriesPlugin,
    SearchPlugin searchPlugin)
  {
    _bettingPlugin = bettingPlugin;
    _unitOfWork = unitOfWork;
    _memoriesPlugin = memoriesPlugin;
    _searchPlugin = searchPlugin;
  }

  [KernelFunction]
  [Description("Retrieves matches for which bets can currently be placed.")]
  public Task<IReadOnlyList<AvailableMatch>> GetAvailableMatchesAsync(CancellationToken cancellationToken = default) =>
    _bettingPlugin.GetAvailableMatchesAsync(cancellationToken);

  [KernelFunction]
  [Description("Returns the current betting odds for the given match.")]
  public Task<IReadOnlyList<CurrentOddsMarket>> GetCurrentOddsAsync(int matchId, CancellationToken cancellationToken = default) =>
    _bettingPlugin.GetCurrentOddsAsync(matchId, cancellationToken);

  [KernelFunction]
  [Description("Returns the latest research analysis content for the given match as plain text.")]
  public async Task<string?> GetMatchAnalysisAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _unitOfWork.Matches
      .GetLatestMatchAnalysisByCodeAsync(matchId, MatchAnalysis.ResearchCode, cancellationToken)
      .ConfigureAwait(false);
    return analysis?.Content;
  }

  [KernelFunction]
  [Description("Places one bet slip per call. One selection is a single bet; multiple selections combine as a parlay on that slip. Call once per slip; you may call multiple times for multiple separate slips. Stake must not exceed current balance.")]
  public Task PlaceBetSlip( 
    decimal stakeAmount,
    [Description("JSON object with property betSelections: an array of selection objects. Each object must have: matchId (int, from GetAvailableMatchesAsync), eventType (string enum name), eventOption (string BettingEventOption enum name). Example: {\"betSelections\":[{\"matchId\":39,\"eventType\":\"bothTeamsToScore\",\"eventOption\":\"bothTeamsToScore_Yes\"}]}")]
    string betSelectionsJson,
    CancellationToken cancellationToken = default) =>
    _bettingPlugin.PlaceBetSlip(stakeAmount, betSelectionsJson, cancellationToken);

  [KernelFunction]
  [Description("Returns pending bet slips, newest first.")]
  public Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAsync(CancellationToken cancellationToken = default) =>
    _bettingPlugin.GetBetSlipsAsync(BetStatus.Pending, cancellationToken);


  [KernelFunction]
  [Description("Lists all saved memories.")]
  public Task<List<MemoryRecordListItem>> GetMemoryRecordsAsync(CancellationToken cancellationToken = default) =>
    _memoriesPlugin.GetMemoryRecordsAsync(cancellationToken);

  [KernelFunction]
  [Description("Loads the full content of a saved memory record.")]
  public Task<string> ReadMemoryAsync(string name, CancellationToken cancellationToken = default) =>
    _memoriesPlugin.ReadAsync(name, cancellationToken);

  [KernelFunction]
  [Description("Replaces the entire memory record with new content. Creates the record if it does not exist. Prefer AppendMemoryAsync or ReplaceMemoryAsync for small changes so you do not drop existing text.")]
  public Task<string> WriteMemoryAsync(string name, string text, CancellationToken cancellationToken = default) =>
    _memoriesPlugin.WriteAsync(name, text, cancellationToken);

  [KernelFunction]
  [Description("Adds text to the end of an existing memory record")]
  public Task<string> AppendMemoryAsync(string name, string text, CancellationToken cancellationToken = default) =>
    _memoriesPlugin.AppendAsync(name, text, cancellationToken);

  [KernelFunction]
  [Description("Finds an exact substring in a memory record and substitutes newText. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.")]
  public Task<string> ReplaceMemoryAsync(
    string name,
    string oldText,
    string? newText,
    bool replaceAll = false,
    CancellationToken cancellationToken = default) =>
    _memoriesPlugin.ReplaceAsync(name, oldText, newText, replaceAll, cancellationToken);

  [KernelFunction]
  [Description("Search for recent news articles and current events.")]
  public Task<IReadOnlyList<SearchNewsArticleDto>> SearchNewsAsync(string query, CancellationToken cancellationToken = default) =>
    _searchPlugin.SearchNewsAsync(query, cancellationToken);

  [KernelFunction]
  [Description("Retrieves high-quality, grounded information chunks from the web. Best for fact-checking, gathering deep context for a complex question, or summarizing a specific topic.")]
  public Task<IReadOnlyList<SearchLlmContextItemDto>> GetWebGroundingAsync(string query, CancellationToken cancellationToken = default) =>
    _searchPlugin.GetWebGroundingAsync(query, cancellationToken);
}
