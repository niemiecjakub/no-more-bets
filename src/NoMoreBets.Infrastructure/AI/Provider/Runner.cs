using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Infrastructure.AI.Provider;

public sealed class Runner
{
  private Agent _agent;
  private AgentThread _thread;
  private AgentInvokeOptions _options;
  private ThreadProvider _threadProvider;

  public Runner(AgentBuilder agentBuilder, ThreadProvider threadProvider, IPluginFactory pluginFactory)
  {
    _threadProvider = threadProvider;
    var config = agentBuilder.Build();
    _agent ??= config.Agent;
    _thread ??= config.Thread;
    _options ??= config.Options;
    _agent.Kernel.Plugins.AddFromObject(pluginFactory.CreateSearchPlugin());
  }

  public async Task<List<ChatMessageContent>> RunTurnAsync(string userMessage, CancellationToken cancellationToken = default)
  {
    var messages = new List<ChatMessageContent>();
    await foreach (var message in _agent.InvokeAsync(userMessage, _thread, _options, cancellationToken))
    {
      messages.Add(message.Message);
      _threadProvider.ThreadId = message.Thread.Id;
    }

    return messages;
  }
}
