using System.ComponentModel;
using NoMoreBets.Domain.Memories;
using NoMoreBets.Infrastructure.AI.Plugins.Models;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public abstract class AgentPluginBase
{
  private readonly MemoriesPlugin _memoriesPlugin;
  private readonly InternetSearchPlugin _searchPlugin;
  private readonly BankrollPlugin _bankrollPlugin;

  protected AgentPluginBase(
    MemoriesPlugin memoriesPlugin,
    InternetSearchPlugin searchPlugin,
    BankrollPlugin bankrollPlugin)
  {
    _memoriesPlugin = memoriesPlugin;
    _searchPlugin = searchPlugin;
    _bankrollPlugin = bankrollPlugin;
  }

  [AgentTool]
  [Description("Lists all saved memory records.")]
  public async Task<List<MemoryRecordListItem>> GetMemoryRecordsAsync(CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.GetMemoryRecordsAsync(cancellationToken).ConfigureAwait(false);
  }

  [AgentTool]
  [Description("Loads the full content of a saved memory record.")]
  public async Task<string> ReadMemoryAsync(string name, CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.ReadAsync(name, cancellationToken).ConfigureAwait(false);
  }

  [AgentTool]
  [Description("Replaces the entire memory record with new content. Creates the record if it does not exist. Prefer AppendMemoryAsync or ReplaceMemoryAsync for small changes so you do not drop existing text.")]
  public async Task<string> WriteMemoryAsync(
    string name,
    string text,
    [Description("Optional short label for the record. When updating, null leaves description unchanged; empty string clears it.")]
    string? description = null,
    CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.WriteAsync(name, text, description, cancellationToken).ConfigureAwait(false);
  }

  [AgentTool]
  [Description("Adds text to the end of an existing memory record")]
  public async Task<string> AppendMemoryAsync(string name, string text, CancellationToken cancellationToken = default)
  {
    return await _memoriesPlugin.AppendAsync(name, text, cancellationToken).ConfigureAwait(false);
  }

  [AgentTool]
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

  [AgentTool]
  [Description("Search for recent news articles and current events.")]
  public async Task<IReadOnlyList<SearchNewsArticleDto>> SearchNewsAsync(
    string query,
    [Description("Optional time window: pd (last 24 hours), pw (last 7 days), pm (last 31 days), py (last year). Omit or null for no freshness filter.")]
    string? freshness = null,
    CancellationToken cancellationToken = default)
  {
    return await _searchPlugin.SearchNewsAsync(query, freshness, cancellationToken).ConfigureAwait(false);
  }

  [AgentTool]
  [Description("Retrieves high-quality, grounded information chunks from the web. Best for fact-checking, gathering deep context, summarizing a topic, or verifying what happened (e.g. when reflecting on a settled bet slip).")]
  public async Task<SearchLlmContextItemDto> GetWebGroundingAsync(
    string query,
    [Description("Optional time window: pd, pw, pm, py. Omit or null for no freshness filter (default).")]
    string? freshness = null,
    CancellationToken cancellationToken = default)
  {
    return await _searchPlugin.GetWebGroundingAsync(query, freshness, cancellationToken).ConfigureAwait(false);
  }

  [AgentTool("GetCurrentBalance")]
  [Description("Returns bank account balance")]
  public Task<decimal> GetCurrentBalanceAsync(CancellationToken cancellationToken = default) =>
    _bankrollPlugin.GetCurrentBalanceAsync(cancellationToken);

  [AgentTool("GetDaysUntilPayday")]
  [Description("Returns number of days untill next payday.")]
  public Task<int> GetDaysUntilPaydayAsync(CancellationToken cancellationToken = default) =>
    _bankrollPlugin.GetDaysUntilPaydayAsync(cancellationToken);
}
