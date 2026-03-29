using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

namespace NoMoreBets.Infrastructure.AI.Agent;

public sealed class Runner
{
  private readonly Kernel _kernel;
  private readonly AgentBuilder _agentBuilder;
  private readonly AgentThreadState _threadState;
  private OpenAIAssistantAgent? _agent;
  private AgentThread? _thread;

  public Runner(Kernel kernel, AgentBuilder agentBuilder, AgentThreadState threadState)
  {
    _kernel = kernel;
    _agentBuilder = agentBuilder;
    _threadState = threadState;
  }

  public async Task<ChatMessageContent> RunTurnAsync(string userMessage, CancellationToken cancellationToken = default)
  {
    _agent ??= await _agentBuilder.BuildAsync(cancellationToken).ConfigureAwait(false);

    if (_thread is null)
    {
      var storedThreadId = _threadState.ThreadId;
      if (!string.IsNullOrEmpty(storedThreadId))
      {
        _thread = new OpenAIAssistantAgentThread(_agent.Client, storedThreadId);
      }
    }

    var messages = new List<ChatMessageContent> { new(AuthorRole.User, userMessage) };
    var options = new AgentInvokeOptions { Kernel = _kernel };

    ChatMessageContent? lastMessage = null;
    await foreach (var item in _agent.InvokeAsync(messages, _thread, options, cancellationToken).ConfigureAwait(false))
    {
      lastMessage = item.Message;
      _thread = item.Thread;
    }

    if (!string.IsNullOrEmpty(_thread?.Id))
    {
      _threadState.ThreadId = _thread.Id;
    }

    return lastMessage ?? throw new InvalidOperationException("Assistant returned no message.");
  }
}
