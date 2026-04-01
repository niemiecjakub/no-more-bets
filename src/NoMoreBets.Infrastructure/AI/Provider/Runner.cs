using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Provider;

public sealed class Runner
{
  private Agent? _agent;
  private AgentThread? _thread;
  private AgentInvokeOptions? _options;

  public Runner(AgentBuilder agentBuilder, IPluginFactory pluginFactory)
  {
    var config = agentBuilder.Build();
    _agent ??= config.Agent;
    _thread ??= config.Thread;
    _options ??= config.Options;
    _agent.Kernel.Plugins.AddFromObject(pluginFactory.CreateSearchPlugin());
  }

  public async Task<List<ChatMessageContent>> RunTurnAsync(string userMessage, CancellationToken cancellationToken = default)
  {
    var messages = new List<ChatMessageContent>();
    await foreach (var message in _agent!.InvokeAsync(userMessage, _thread, _options, cancellationToken))
    {
      messages.Add(message.Message);
    }

    return messages;
  }
}
