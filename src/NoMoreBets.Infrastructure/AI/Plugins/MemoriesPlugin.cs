using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public class MemoriesPlugin
{
  private readonly IUnitOfWork _unitOfWork;

  public MemoriesPlugin(IUnitOfWork unitOfWork)
  {
    _unitOfWork = unitOfWork;
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
    string name,
    CancellationToken cancellationToken = default)
  {
    var normalizedName = NormalizeName(name);
    var memory = await _unitOfWork.Memories.GetByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
    if (memory == null)
    {
      throw new KeyNotFoundException($"Memory '{normalizedName}' does not exist.");
    }

    return memory.Content;
  }

  [KernelFunction("Write")]
  [Description("Replaces the entire memory record with new content. Creates the record if it does not exist. Prefer Append or Replace for small changes so you do not drop existing text.")]
  public async Task<string> WriteAsync(
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
    return "Strategy updated successfully";
  }

  [KernelFunction("Append")]
  [Description("Adds text to the end of an existing memory record")]
  public async Task<string> AppendAsync(
    string name,
    string text,
    CancellationToken cancellationToken = default)
  {
    var normalizedName = NormalizeName(name);
    var memory = await _unitOfWork.Memories.GetByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
    if (memory == null)
    {
      throw new KeyNotFoundException($"Memory '{normalizedName}' does not exist.");
    }

    memory.AppendContent(text);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "Text appended successfully";
  }

  [KernelFunction("Replace")]
  [Description("Finds an exact substring in a memory record and substitutes newText. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.")]
  public async Task<string> ReplaceAsync(
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
      throw new KeyNotFoundException($"Memory '{normalizedName}' does not exist.");
    }

    memory.ReplaceSubstring(oldText, newText, replaceAll);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "Replacement applied successfully";
  }
}
