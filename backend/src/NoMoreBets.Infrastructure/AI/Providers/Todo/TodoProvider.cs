using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using NoMoreBets.Application.AgentTools;
using System.Runtime.CompilerServices;
using System.Text;

namespace NoMoreBets.Infrastructure.AI.Providers.Todo;

public sealed class TodoProvider : AIContextProvider, IDisposable
{
  private static readonly string Instructions =
      $$"""
        # Todo Items

        You have access to a todo list for tracking work items.
        While planning, break down complex tasks into manageable todo items and add them to the list.
        During execution, use the todo list to keep track of what needs to be done, mark items as complete when finished, and remove any items that are no longer needed.
        Update the todo list when requirements change by removing irrelevant items or adding new ones as needed.

        Use these tools to manage your tasks:
        - Use {{AgentToolCatalog.Todo.Add.Name}} to break down complex work into trackable items (supports adding one or many at once).
        - Use {{AgentToolCatalog.Todo.Complete.Name}} to mark items as done when finished (supports one or many at once). Include a reason describing how the items were completed.
        - Use {{AgentToolCatalog.Todo.GetRemaining.Name}} to check what work is still pending.
        - Use {{AgentToolCatalog.Todo.GetAll.Name}} to review the full list including completed items.
        - Use {{AgentToolCatalog.Todo.Remove.Name}} to remove items that are no longer needed (supports one or many at once).
        """;

  private readonly ProviderSessionState<TodoState> _sessionState;
  private readonly ConditionalWeakTable<AgentSession, SemaphoreSlim> _sessionLocks = new();
  private readonly SemaphoreSlim _nullSessionLock = new(1, 1);

  public TodoProvider()
  {
    _sessionState = new ProviderSessionState<TodoState>(
        _ => new TodoState(),
        GetType().Name,
        AgentAbstractionsJsonUtilities.DefaultOptions);
  }

  /// <inheritdoc />
  public void Dispose()
  {
    _nullSessionLock.Dispose();
  }

  /// <inheritdoc />
  protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    SemaphoreSlim sessionLock = GetSessionLock(context.Session);
    await sessionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
    List<TodoItem> currentItems;
    try
    {
      TodoState state = _sessionState.GetOrInitializeState(context.Session);
      currentItems = state.Items.ToList();
    }
    finally
    {
      sessionLock.Release();
    }

    var aiContext = new AIContext
    {
      Instructions = Instructions,
      Tools = CreateTools(context.Session),
      Messages =
      [
        new ChatMessage(ChatRole.User, FormatTodoListMessage(currentItems)),
      ],
    };

    return aiContext;
  }

  private SemaphoreSlim GetSessionLock(AgentSession? session)
  {
    if (session is null)
    {
      return _nullSessionLock;
    }

    return _sessionLocks.GetValue(session, _ => new SemaphoreSlim(1, 1));
  }

  private async Task<List<TodoItem>> AddTodosAsync(AgentSession? session, List<TodoItemInput> todos)
  {
    SemaphoreSlim sessionLock = GetSessionLock(session);
    await sessionLock.WaitAsync().ConfigureAwait(false);
    try
    {
      TodoState state = _sessionState.GetOrInitializeState(session);
      var created = new List<TodoItem>();
      foreach (TodoItemInput input in todos)
      {
        var item = new TodoItem
        {
          Id = state.NextId++,
          Title = input.Title.Trim(),
        };
        state.Items.Add(item);
        created.Add(item);
      }

      _sessionState.SaveState(session, state);
      return created;
    }
    finally
    {
      sessionLock.Release();
    }
  }

  private async Task<int> CompleteTodosAsync(AgentSession? session, List<TodoCompleteInput> items)
  {
    SemaphoreSlim sessionLock = GetSessionLock(session);
    await sessionLock.WaitAsync().ConfigureAwait(false);
    try
    {
      TodoState state = _sessionState.GetOrInitializeState(session);
      var idSet = new HashSet<int>(items.Select(i => i.Id));
      int completed = 0;
      foreach (TodoItem item in state.Items)
      {
        if (!item.IsComplete && idSet.Contains(item.Id))
        {
          item.IsComplete = true;
          completed++;
        }
      }

      if (completed > 0)
      {
        _sessionState.SaveState(session, state);
      }

      return completed;
    }
    finally
    {
      sessionLock.Release();
    }
  }

  private async Task<int> RemoveTodosAsync(AgentSession? session, List<int> ids)
  {
    SemaphoreSlim sessionLock = GetSessionLock(session);
    await sessionLock.WaitAsync().ConfigureAwait(false);
    try
    {
      TodoState state = _sessionState.GetOrInitializeState(session);
      var idSet = new HashSet<int>(ids);
      int removed = state.Items.RemoveAll(t => idSet.Contains(t.Id));

      if (removed > 0)
      {
        _sessionState.SaveState(session, state);
      }

      return removed;
    }
    finally
    {
      sessionLock.Release();
    }
  }

  private async Task<List<TodoItem>> GetRemainingTodosAsync(AgentSession? session)
  {
    SemaphoreSlim sessionLock = GetSessionLock(session);
    await sessionLock.WaitAsync().ConfigureAwait(false);
    try
    {
      TodoState state = _sessionState.GetOrInitializeState(session);
      return state.Items.Where(t => !t.IsComplete).ToList();
    }
    finally
    {
      sessionLock.Release();
    }
  }

  private async Task<List<TodoItem>> GetAllTodosAsync(AgentSession? session)
  {
    SemaphoreSlim sessionLock = GetSessionLock(session);
    await sessionLock.WaitAsync().ConfigureAwait(false);
    try
    {
      TodoState state = _sessionState.GetOrInitializeState(session);
      return state.Items.ToList();
    }
    finally
    {
      sessionLock.Release();
    }
  }

  private AITool[] CreateTools(AgentSession? session)
  {
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;

    return
    [
      AIFunctionFactory.Create(
        (List<TodoItemInput> todos) => AddTodosAsync(session, todos),
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Todo.Add.Name,
          Description = "Add one or more todo items. Each item has a title. Returns the list of created todo items.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        (List<TodoCompleteInput> items) => CompleteTodosAsync(session, items),
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Todo.Complete.Name,
          Description = "Mark one or more todo items as complete. Each entry has an ID and a reason describing how/why the item was completed. Returns the number of items that were found and marked complete.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        (List<int> ids) => RemoveTodosAsync(session, ids),
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Todo.Remove.Name,
          Description = "Remove one or more todo items by their IDs. Returns the number of items that were found and removed.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        () => GetRemainingTodosAsync(session),
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Todo.GetRemaining.Name,
          Description = "Retrieve the list of incomplete todo items.",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        () => GetAllTodosAsync(session),
        new AIFunctionFactoryOptions
        {
          Name = AgentToolCatalog.Todo.GetAll.Name,
          Description = "Retrieve the full list of todo items, both complete and incomplete.",
          SerializerOptions = serializerOptions,
        }),
    ];
  }

  private static string FormatTodoListMessage(List<TodoItem> items)
  {
    if (items.Count == 0)
    {
      return "### Current todo list\n- none yet";
    }

    var sb = new StringBuilder("### Current todo list\n");
    foreach (TodoItem item in items)
    {
      string status = item.IsComplete ? "done" : "open";
      sb.AppendLine($"- {item.Id} [{status}] {item.Title}");
    }

    return sb.ToString().TrimEnd();
  }
}
