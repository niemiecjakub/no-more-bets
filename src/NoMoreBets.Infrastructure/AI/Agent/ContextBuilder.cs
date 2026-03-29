using Microsoft.SemanticKernel.ChatCompletion;

namespace NoMoreBets.Infrastructure.AI.Agent;

public sealed class ContextBuilder
{
  private readonly string _systemMessage;

  public ContextBuilder()
  {
    var workspace = Path.Combine(AppContext.BaseDirectory, "AI", "Agent");
    var path = Path.Combine(workspace, "SOUL.md");
    _systemMessage = File.Exists(path)
      ? $"# SOUL\n\n{File.ReadAllText(path)}"
      : string.Empty;
  }

  public string Instructions => _systemMessage;
}
