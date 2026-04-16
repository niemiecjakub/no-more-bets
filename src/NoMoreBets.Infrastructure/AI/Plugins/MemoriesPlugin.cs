using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class MemoriesPlugin
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<MemoriesPlugin> _logger;

  public MemoriesPlugin(IUnitOfWork unitOfWork, ILogger<MemoriesPlugin>? logger = null)
  {
    _unitOfWork = unitOfWork;
    _logger = logger ?? NullLogger<MemoriesPlugin>.Instance;
  }

  private static string NormalizeName(string filename)
  {
    var name = Path.GetFileName(filename);
    Memory.ValidateName(name);
    return name;
  }

  [KernelFunction]
  [Description("Lists all saved memory records.")]
  public async Task<List<MemoryRecordListItem>> GetMemoryRecordsAsync(CancellationToken cancellationToken = default)
  {
    var records = await _unitOfWork.Memories.GetRecordsAsync(cancellationToken).ConfigureAwait(false);
    return records.ToList();
  }

  [KernelFunction("Read")]
  [Description("Loads the full content of a saved memory record.")]
  public async Task<string> ReadAsync(
    [Description("Name of the memory record to read.")]
    string name,
    CancellationToken cancellationToken = default)
  {
    var normalizedName = NormalizeName(name);
    var memory = await _unitOfWork.Memories.GetByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
    if (memory == null)
    {
      _logger.LogWarning("Memory record {MemoryName} not found for read operation.", normalizedName);
      throw new KeyNotFoundException($"Memory '{normalizedName}' does not exist.");
    }

    return string.IsNullOrEmpty(memory.Content) ? "*This memory is currently empty*" : memory.Content;
  }

  [KernelFunction("Write")]
  [Description("Replaces the entire memory record with new content. Creates the record if it does not exist. Prefer Append or Replace for small changes so you do not drop existing text.")]
  public async Task<string> WriteAsync(
    [Description("Name of the memory record to update.")]
    string name,
    [Description("Complete new body to persist (overwrites everything previously stored).")]
    string text,
    CancellationToken cancellationToken = default)
  {
    var normalizedName = NormalizeName(name);

    var existing = await _unitOfWork.Memories.GetByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
    if (existing != null)
    {
      existing.ReplaceContent(text);
    }
    else
    {
      await _unitOfWork.Memories.AddAsync(Memory.Create(normalizedName, text), cancellationToken).ConfigureAwait(false);
    }

    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "*Memory updated successfully*";
  }

  [KernelFunction("Append")]
  [Description("Adds text to the end of an existing memory record")]
  public async Task<string> AppendAsync(
        [Description("Name of the memory record to update.")]
    string name,
        [Description("Text to add to the end of the memory record.")]
    string text,
    CancellationToken cancellationToken = default)
  {
    var normalizedName = NormalizeName(name);
    var memory = await _unitOfWork.Memories.GetByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
    if (memory == null)
    {
      _logger.LogWarning("Memory record {MemoryName} not found for append operation.", normalizedName);
      throw new KeyNotFoundException($"Memory '{normalizedName}' does not exist.");
    }

    memory.AppendContent(text);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "*Text appended successfully*";
  }

  [KernelFunction("Replace")]
  [Description("Finds an exact substring in a memory record and substitutes newText. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.")]
  public async Task<string> ReplaceAsync(
    [Description("Name of the memory record to update.")]
    string name,
    [Description("Literal text to find; copy from Read output so spacing and casing match.")]
    string oldText,
    [Description("Replacement for matched text; may be empty to delete the matched segment.")]
    string? newText,
    [Description("True: replace every match. False: replace only if oldText appears once (safer for targeted edits).")]
    bool replaceAll = false,
    CancellationToken cancellationToken = default)
  {
    var normalizedName = NormalizeName(name);
    var memory = await _unitOfWork.Memories.GetByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
    if (memory == null)
    {
      _logger.LogWarning("Memory record {MemoryName} not found for replace operation.", normalizedName);
      throw new KeyNotFoundException($"Memory '{normalizedName}' does not exist.");
    }

    memory.ReplaceSubstring(oldText, newText, replaceAll);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "*Replacement applied successfully*";
  }
}
