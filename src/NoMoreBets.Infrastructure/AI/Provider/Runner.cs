using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.AI.Provider;

public sealed class Runner
{
  private Agent _agent;
  private AgentThread _thread;
  private AgentInvokeOptions _options;
  private ThreadProvider _threadProvider;
  private readonly IPluginFactory _pluginFactory;

  public Runner(AgentBuilder agentBuilder, ThreadProvider threadProvider, IPluginFactory pluginFactory)
  {
    _threadProvider = threadProvider;
    var config = agentBuilder.Build();
    _agent ??= config.Agent;
    _thread ??= config.Thread;
    _options ??= config.Options;

    _pluginFactory = pluginFactory;
  }

  public async Task<List<ChatMessageContent>> Chat(string userMessage, CancellationToken cancellationToken = default)
  {
    _agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateMemoriesPlugin());
    _agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateSearchPlugin());
    
    var messages = new List<ChatMessageContent>();
    await foreach (var message in _agent.InvokeAsync(userMessage, _thread, _options, cancellationToken))
    {
      messages.Add(message.Message);
      _threadProvider.ThreadId = message.Thread.Id;
    }

    return messages;
  }

  public async Task<List<ChatMessageContent>> Act(CancellationToken cancellationToken = default)
  {
    _agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateMemoriesPlugin());
    _agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateBettingPlugin());
    _agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateBankrollPlugin());
    _agent.Kernel.Plugins.AddFromObject(_pluginFactory.CreateSearchPlugin());

    var messages = new List<ChatMessageContent>();
    //await foreach (var message in _agent.InvokeAsync(userMessage, _thread, _options, cancellationToken))
    //{
    //  messages.Add(message.Message);
    //  _threadProvider.ThreadId = message.Thread.Id;
    //}

    return messages;
  }
}
