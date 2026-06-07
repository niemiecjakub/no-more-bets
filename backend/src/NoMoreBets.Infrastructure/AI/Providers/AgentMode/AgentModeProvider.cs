using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Text;

namespace NoMoreBets.Infrastructure.AI.Providers.AgentMode;

public sealed class AgentModeProvider : AIContextProvider
{
  private const string PlanMode = "plan";
  private const string ExecuteMode = "execute";
  private const string ModeGetToolName = "mode_get";
  private const string ModeSetToolName = "mode_set";

  private static readonly string Instructions =
      $$"""
        # Agent Mode

        - You can operate in different modes. Depending on the mode you are in, you will be required to follow different processes.
        - You must check the current mode after any user input, since the user may have changed the mode themselves,
          e.g. the user may have switched to '{{PlanMode}}' mode after a previous research task finished in '{{ExecuteMode}}' mode, meaning they want to review a plan first before execution.

        Use these tools to manage your mode:
        - Use {{ModeGetToolName}} to check your current operating mode.
        - Use {{ModeSetToolName}} to switch between modes as your work progresses. Only use {{ModeSetToolName}} if the user explicitly instructs/allows you to change modes.

        You are currently operating in the {current_mode} mode.

        ### Mandatory Mode based Workflow

        For every new substantive user request, including short factual questions, your behavior is determined by the mode you are in.

        {available_modes}
        """;

  private sealed record AgentMode(string Name, string Description);

  private static readonly IReadOnlyList<AgentMode> _modes =
  [
      new(
            PlanMode,
            $"""
            Use this mode when analyzing requirements, breaking down tasks, and creating plans.

            Process to follow when in plan mode:
            1. Analyze the request with the purpose of building a research plan.
            2. Create a list of todo items.
            3. If needed, use the provided tools to do exploratory checks to help build the plan.
            4. Resolve any ambiguity using your best judgment and note assumptions in the plan.
            5. When the plan is complete, switch to {ExecuteMode} mode (using the `{ModeSetToolName}` tool), and follow the steps for *{ExecuteMode} mode*.
            """),
        new(
            ExecuteMode,
            $"""
            Use this mode when carrying out approved plans. Work autonomously using your best judgment — do not ask the user questions or wait for feedback.
            
            Process to follow when in {ExecuteMode} mode:
            1. If you don't have a plan or tasks yet, analyze the user request and create tasks and a plan. (**Skip this step if you came from {PlanMode} mode**)
            2. Work autonomously — use your best judgment to make decisions and keep progressing without asking the user questions. The goal is to have a complete, useful result ready when the user returns.
            3. If you encounter ambiguity or an unexpected situation during execution, choose the most reasonable option, note your choice, and keep going.
            4. Mark tasks as completed as you finish them.
            5. Continue working, thinking and calling tools until you have the research result for the user.
            """),
    ];

  private readonly ProviderSessionState<AgentModeState> _sessionState;
  private readonly string _defaultMode;
  private readonly HashSet<string> _validModeNames;
  private readonly string _modeNamesDisplay;

  /// <summary>
  /// Initializes a new instance of the <see cref="AgentModeProvider"/> class.
  /// </summary>
  /// <param name="options">Optional settings that control provider behavior. When <see langword="null"/>, defaults are used.</param>
  public AgentModeProvider(AgentModeProviderOptions? options = null)
  {
    if (_modes.Count == 0)
    {
      throw new ArgumentException("At least one mode must be configured.", nameof(options));
    }

    _validModeNames = new HashSet<string>(_modes.Select(m => m.Name), StringComparer.Ordinal);
    _modeNamesDisplay = string.Join("\", \"", _modes.Select(m => m.Name));
    _defaultMode = options?.DefaultMode ?? _modes[0].Name;

    if (!_validModeNames.Contains(_defaultMode))
    {
      throw new ArgumentException($"Default mode \"{_defaultMode}\" is not in the configured modes list.", nameof(options));
    }

_sessionState = new ProviderSessionState<AgentModeState>(
        _ => new AgentModeState { CurrentMode = _defaultMode },
GetType().Name,
        AgentAbstractionsJsonUtilities.DefaultOptions);
  }

  /// <inheritdoc />
  protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
  {
    AgentModeState state = _sessionState.GetOrInitializeState(context.Session);

    string instructions = BuildInstructions(state.CurrentMode);

    var aiContext = new AIContext
    {
      Instructions = instructions,
      Tools = CreateTools(state, context.Session),
    };

    return new ValueTask<AIContext>(aiContext);
  }

  private string BuildInstructions(string currentMode)
  {
    var modesListBuilder = new StringBuilder();
    foreach (var mode in _modes)
    {
      modesListBuilder.AppendLine($"#### {mode.Name}");
      modesListBuilder.AppendLine();
      modesListBuilder.AppendLine(mode.Description.TrimEnd());
      modesListBuilder.AppendLine();
    }

    var modesListText = modesListBuilder.ToString();

    return new StringBuilder(Instructions)
        .Replace("{available_modes}", modesListText)
        .Replace("{current_mode}", currentMode)
        .ToString();
  }

  private void ValidateMode(string mode)
  {
    if (!_validModeNames.Contains(mode))
    {
      throw new ArgumentException($"Invalid mode: \"{mode}\". Supported modes are: \"{_modeNamesDisplay}\".", nameof(mode));
    }
  }

  private string GetCurrentMode(AgentModeState state)
  {
    return state.CurrentMode;
  }

  private string SetMode(AgentModeState state, AgentSession? session, string mode)
  {
    ValidateMode(mode);
    state.CurrentMode = mode;
    _sessionState.SaveState(session, state);
    return $"Mode changed to \"{mode}\".";
  }

  private AITool[] CreateTools(AgentModeState state, AgentSession? session)
  {
    var serializerOptions = AgentAbstractionsJsonUtilities.DefaultOptions;

    return
    [
      AIFunctionFactory.Create(
        (string mode) => SetMode(state, session, mode),
        new AIFunctionFactoryOptions
        {
          Name = ModeSetToolName,
          Description = $"Switch the agent's operating mode. Supported modes: \"{_modeNamesDisplay}\".",
          SerializerOptions = serializerOptions,
        }),

      AIFunctionFactory.Create(
        () => GetCurrentMode(state),
        new AIFunctionFactoryOptions
        {
          Name = ModeGetToolName,
          Description = "Get the agent's current operating mode.",
          SerializerOptions = serializerOptions,
        }),
    ];
  }
}