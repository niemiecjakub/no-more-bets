using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Infrastructure.AI.Providers.Memories;

public sealed class MemoriesProvider : AIContextProvider
{
  private const string GetRecordsToolName = "Memories_GetRecords";
  private const string ReadToolName = "Memories_Read";
  private const string WriteToolName = "Memories_Write";
  private const string AppendToolName = "Memories_Append";
  private const string ReplaceToolName = "Memories_Replace";
  private const string DeleteToolName = "Memories_Delete";

  private static readonly string Instructions =
      $$"""
        ## Memories
        You have access to persistent memory records.

        Use these tools to manage memories:
        - Use {{GetRecordsToolName}} to list all saved memory records.
        - Use {{ReadToolName}} to load the full content of a saved memory record.
        - Use {{WriteToolName}} to replace an entire memory record with new content. Creates the record if it does not exist. Prefer Append or Replace for small changes so you do not drop existing text.
        - Use {{AppendToolName}} to add text to the end of an existing memory record.
        - Use {{ReplaceToolName}} to find an exact substring in a memory record and substitute new text. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.
        - Use {{DeleteToolName}} to permanently delete a named memory record. Use only when the entire record is obsolete.
        """;

  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<MemoriesProvider> _logger;

  public MemoriesProvider(IUnitOfWork unitOfWork, ILogger<MemoriesProvider>? logger = null)
  {
    _unitOfWork = unitOfWork;
    _logger = logger ?? NullLogger<MemoriesProvider>.Instance;
  }

  protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    var aiContext = new AIContext
    {
      Instructions = Instructions,
      Tools = CreateTools(),
    };

    return ValueTask.FromResult(aiContext);
  }

  private AITool[] CreateTools()
  {
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;

    return
    [
      AIFunctionFactory.Create(
        GetMemoryRecordsAsync,
        new AIFunctionFactoryOptions
        {
          Name = GetRecordsToolName,
          Description = "Lists all saved memory records.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        ReadAsync,
        new AIFunctionFactoryOptions
        {
          Name = ReadToolName,
          Description = "Loads the full content of a saved memory record.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        WriteAsync,
        new AIFunctionFactoryOptions
        {
          Name = WriteToolName,
          Description = "Replaces the entire memory record with new content. Creates the record if it does not exist. Prefer Append or Replace for small changes so you do not drop existing text.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        AppendAsync,
        new AIFunctionFactoryOptions
        {
          Name = AppendToolName,
          Description = "Adds text to the end of an existing memory record.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        ReplaceAsync,
        new AIFunctionFactoryOptions
        {
          Name = ReplaceToolName,
          Description = "Finds an exact substring in a memory record and substitutes newText. Matching is case-sensitive and does not ignore whitespace. If replaceAll is false, oldText must occur exactly once or the call fails.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        DeleteMemoryAsync,
        new AIFunctionFactoryOptions
        {
          Name = DeleteToolName,
          Description = "Permanently deletes a named memory record. Use only when the entire record is obsolete.",
          SerializerOptions = serializerOptions,
        }),
    ];
  }

  private static string NormalizeName(string name)
  {
    Memory.ValidateName(name);
    return name;
  }

  private async Task<List<MemoryRecordListItem>> GetMemoryRecordsAsync(CancellationToken cancellationToken = default)
  {
    var records = await _unitOfWork.Memories.GetRecordsAsync(cancellationToken).ConfigureAwait(false);
    return records.ToList();
  }

  private async Task<string> ReadAsync(
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

  private async Task<string> WriteAsync(
    [Description("Name of the memory record to update.")]
    string name,
    [Description("Complete new body to persist (overwrites everything previously stored).")]
    string text,
    [Description("Short label or summary for the record. When updating an existing record, omit or pass null to leave the description unchanged; pass an empty string to clear it.")]
    string? description = null,
    CancellationToken cancellationToken = default)
  {
    var normalizedName = NormalizeName(name);

    var existing = await _unitOfWork.Memories.GetByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
    if (existing != null)
    {
      existing.ReplaceContent(text);
      if (description is not null)
      {
        existing.SetDescription(description);
      }
    }
    else
    {
      await _unitOfWork.Memories.AddAsync(
        Memory.Create(normalizedName, text, description),
        cancellationToken).ConfigureAwait(false);
    }

    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "*Memory updated successfully*";
  }

  private async Task<string> AppendAsync(
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

  private async Task<string> ReplaceAsync(
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

  private async Task<string> DeleteMemoryAsync(
    [Description("Name of the memory record to delete (same naming as other memory tools).")]
    string name,
    CancellationToken cancellationToken = default)
  {
    Memory.ValidateName(name);
    var removed = await _unitOfWork.Memories.SoftDeleteByNameAsync(name, cancellationToken).ConfigureAwait(false);
    if (!removed)
    {
      _logger.LogWarning("Memory record {MemoryName} not found for delete operation.", name);
      throw new KeyNotFoundException($"Memory '{name}' does not exist.");
    }

    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return "*Memory record deleted*";
  }
}
