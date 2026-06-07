using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Providers;
using NoMoreBets.Infrastructure.AI.Tools;

namespace NoMoreBets.Infrastructure.AI.Phases.InternetResearch;

public sealed class InternetResearchPhase : IAgentPhaseDefinition, IAgentPhaseStep
{
  public AgentSessionPhase Phase => AgentSessionPhase.InternetResearch;
  public IReadOnlyList<AgentPhaseStep> Steps => [new AgentPhaseStep(this, PersistTranscript: true)];

  public string BuildPrompt() => $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.
          
          You are conducting research for upcoming matches for yourself.
          You are not writing for a syndicate or external audience: this is your own prep for your own future betting sessions.
          Structure it so your future self can quickly reuse it in the betting phase.
          Focus on narratives, news, sentiment, context of the game etc.
          Remember to save the research to memory so you can reuse it in later betting and reflection phases.

          You must use the available plugin functions explicitly.

          Goal:
          Produce one (or more) general research brief(s) for upcoming fixtures that your future self can use for later match-level analysis and betting decisions.

          ## Required workflow

          1) Enumerate upcoming fixtures:
          - Call `GetAvailableMatchesAsync` and identify key upcoming matches to monitor

          2) Read memory context:
          - Call `GetMemoryRecordsAsync`
          - Call `ReadMemoryAsync` for relevant records

          3) Gather internet context:
          - Call `SearchNewsAsync` and `GetWebGroundingAsync` as needed to gather match/club information, news, league updates, and related context
          - Prioritize recent, reliable sources and label uncertainty

          4) Persist useful knowledge:

          - Save distilled, reusable insights to memory with `AppendMemoryAsync`, `ReplaceMemoryAsync`, or `WriteMemoryAsync`
          - Avoid raw copy-paste dumps

          ## Guardrails
          - Be evidence-driven and explicit about missing data
          """;

  public IReadOnlyList<AITool> GetTools(IServiceProvider serviceProvider) =>
    serviceProvider.ResolveTools([
      ToolRegistry.Match.GetUpcomingMatches,
    ]);

  public IReadOnlyList<AIContextProvider> GetAIContextProviders(IServiceProvider serviceProvider) =>
  [
    new MemoriesProvider(serviceProvider.GetRequiredService<IUnitOfWork>()),
    new WebSearchProvider(serviceProvider.GetRequiredService<ISearchService>()),
  ];
}
