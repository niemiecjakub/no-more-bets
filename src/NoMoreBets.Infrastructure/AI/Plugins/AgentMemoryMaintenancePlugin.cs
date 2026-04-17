using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Infrastructure.AI.Plugins;

/// <summary>
/// Memory + search tools for scheduled memory cleanup (same surface as <see cref="AgentPluginBase"/>).
/// </summary>
public sealed class AgentMemoryMaintenancePlugin : AgentPluginBase
{
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<AgentMemoryMaintenancePlugin> _logger;

  public AgentMemoryMaintenancePlugin(
    MemoriesPlugin memoriesPlugin,
    InternetSearchPlugin searchPlugin,
    IUnitOfWork unitOfWork,
    ILogger<AgentMemoryMaintenancePlugin>? logger = null)
    : base(memoriesPlugin, searchPlugin)
  {
    _unitOfWork = unitOfWork;
    _logger = logger ?? NullLogger<AgentMemoryMaintenancePlugin>.Instance;
  }

  [KernelFunction]
  [Description("Permanently deletes a named memory record. Use only when the entire record is obsolete.")]
  public async Task<string> DeleteMemoryAsync(
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
