using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Betting.GetBetSlips;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Memories;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class AgentReflectionPlugin
{
  private readonly BettingPlugin _bettingPlugin;
  private readonly BankrollPlugin _bankrollPlugin;
  private readonly MemoriesPlugin _memoriesPlugin;
  private readonly SearchPlugin _searchPlugin;
  private readonly IUnitOfWork _unitOfWork;

  public AgentReflectionPlugin(
    BettingPlugin bettingPlugin,
    BankrollPlugin bankrollPlugin,
    MemoriesPlugin memoriesPlugin,
    SearchPlugin searchPlugin,
    IUnitOfWork unitOfWork)
  {
    _bettingPlugin = bettingPlugin;
    _bankrollPlugin = bankrollPlugin;
    _memoriesPlugin = memoriesPlugin;
    _searchPlugin = searchPlugin;
    _unitOfWork = unitOfWork;
  }

  [KernelFunction]
  [Description("Lists all saved memory records.")]
  public async Task<List<MemoryRecordListItem>> GetMemoryRecordsAsync(CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.GetMemoryRecordsAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Loads the full content of a saved memory record.")]
  public async Task<string> ReadMemoryAsync(string name, CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.ReadAsync(name, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Replaces the entire memory record with new content. Creates the record if it does not exist. Prefer AppendMemoryAsync or ReplaceMemoryAsync for small changes so you do not drop existing text.")]
  public async Task<string> WriteMemoryAsync(string name, string text, CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.WriteAsync(name, text, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Adds text to the end of an existing memory record")]
  public async Task<string> AppendMemoryAsync(string name, string text, CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.AppendAsync(name, text, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Finds an exact substring in a memory record and substitutes newText. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.")]
  public async Task<string> ReplaceMemoryAsync(
    string name,
    string oldText,
    string? newText,
    bool replaceAll = false,
    CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.ReplaceAsync(name, oldText, newText, replaceAll, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns settled bet slips that still need reflection. Call this first to determine which slips to analyze.")]
  public async Task<IReadOnlyList<BetSlipSummary>> GetBetSlipsAwaitingReflectionAsync(
    CancellationToken cancellationToken = default)
  {
    return await _bettingPlugin.GetBetSlipsAwaitingReflectionAsync(cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Returns the latest stored research analysis text for the match (same source used before betting). Use to compare pre-match thesis to how the bet resolved.")]
  public async Task<string?> GetMatchResearchTextAsync(int matchId, CancellationToken cancellationToken = default)
  {
    var analysis = await _unitOfWork.Matches
      .GetLatestMatchAnalysisByCodeAsync(matchId, MatchAnalysis.ResearchCode, cancellationToken)
      .ConfigureAwait(false);
    return analysis?.Content;
  }

  [KernelFunction]
  [Description("Search for recent news articles and current events.")]
  public async Task<IReadOnlyList<SearchNewsArticleDto>> SearchNewsAsync(
    string query,
    [Description("Optional time window: pd (last 24 hours), pw (last 7 days), pm (last 31 days), py (last year). Omit or null for no freshness filter.")]
    string? freshness = null,
    CancellationToken cancellationToken = default)
  {
    return await _searchPlugin.SearchNewsAsync(query, freshness, cancellationToken).ConfigureAwait(false);
  }

  [KernelFunction]
  [Description("Retrieves high-quality, grounded information chunks from the web. Best for fact-checking or verifying what happened in a match when reflecting on a settled slip.")]
  public async Task<IReadOnlyList<SearchLlmContextItemDto>> GetWebGroundingAsync(
    string query,
    [Description("Optional time window: pd, pw, pm, py. Omit or null for no freshness filter (default).")]
    string? freshness = null,
    CancellationToken cancellationToken = default)
  {
    return await _searchPlugin.GetWebGroundingAsync(query, freshness, cancellationToken).ConfigureAwait(false);
  }
}
