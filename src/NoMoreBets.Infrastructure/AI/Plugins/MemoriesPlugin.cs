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
  [Description("Lists all saved memory record names.")]
  public async Task<List<string>> GetMemoryFilenamesAsync(CancellationToken cancellationToken = default)
  {
    var names = await _unitOfWork.Memories.GetNamesAsync(cancellationToken).ConfigureAwait(false);
    return names.ToList();
  }

  [KernelFunction("Read")]
  [Description("Loads the full content of a saved memory record by name. Use this before editing so snippets match exactly. Fails if the record is missing.")]
  public async Task<string> ReadAsync(
    [Description("Base name only, no folders or path separators (e.g. STRATEGY.md).")]
    string filename,
    CancellationToken cancellationToken = default)
  {
    var name = NormalizeName(filename);
    var memory = await _unitOfWork.Memories.GetByNameAsync(name, cancellationToken).ConfigureAwait(false);
    if (memory == null)
    {
      throw new KeyNotFoundException($"Memory '{name}' does not exist.");
    }

    return memory.Content;
  }

  [KernelFunction("Write")]
  [Description("Replaces the entire memory record with new content. Creates the record if it does not exist. Prefer Append or Replace for small changes so you do not drop existing text.")]
  public async Task<string> WriteAsync(
    [Description("Base name only, no folders or path separators (e.g. STRATEGY.md).")]
    string filename,
    [Description("Complete new body to persist (overwrites everything previously stored).")]
    string text,
    CancellationToken cancellationToken = default)
  {
    var name = NormalizeName(filename);

    var existing = await _unitOfWork.Memories.GetByNameAsync(name, cancellationToken).ConfigureAwait(false);
    if (existing != null)
    {
      existing.ReplaceContent(text);
    }
    else
    {
      await _unitOfWork.Memories.AddAsync(Memory.Create(name, text), cancellationToken).ConfigureAwait(false);
    }

    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "Strategy updated successfully";
  }

  [KernelFunction("Append")]
  [Description("Adds text to the end of an existing memory record without reading the whole record first in a separate step. Fails if the record does not exist; use Write to create a new record.")]
  public async Task<string> AppendAsync(
    [Description("Base name only, no folders or path separators (e.g. STRATEGY.md).")]
    string filename,
    [Description("Content to add after the current end (e.g. a new section or log line).")]
    string text,
    CancellationToken cancellationToken = default)
  {
    var name = NormalizeName(filename);
    var memory = await _unitOfWork.Memories.GetByNameAsync(name, cancellationToken).ConfigureAwait(false);
    if (memory == null)
    {
      throw new KeyNotFoundException($"Memory '{name}' does not exist.");
    }

    memory.AppendContent(text);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "Text appended successfully";
  }

  [KernelFunction("Replace")]
  [Description("Finds an exact byte-for-byte substring in a memory record and substitutes newText. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.")]
  public async Task<string> ReplaceAsync(
    [Description("Base name only, no folders or path separators (e.g. STRATEGY.md).")]
    string filename,
    [Description("Literal text to find; copy from Read output so spacing and casing match.")]
    string oldText,
    [Description("Replacement for matched text; may be empty to delete the matched segment.")]
    string? newText,
    [Description("True: replace every match. False: replace only if oldText appears once (safer for targeted edits).")]
    bool replaceAll = false,
    CancellationToken cancellationToken = default)
  {
    var name = NormalizeName(filename);
    var memory = await _unitOfWork.Memories.GetByNameAsync(name, cancellationToken).ConfigureAwait(false);
    if (memory == null)
    {
      throw new KeyNotFoundException($"Memory '{name}' does not exist.");
    }

    memory.ReplaceSubstring(oldText, newText, replaceAll);
    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "Replacement applied successfully";
  }
}
