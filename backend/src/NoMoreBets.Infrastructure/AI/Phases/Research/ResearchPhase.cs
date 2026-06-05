using Microsoft.Extensions.AI;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.AI.Common;

namespace NoMoreBets.Infrastructure.AI.Phases.Research;

public sealed class ResearchPhaseDefinition : IAgentPhaseDefinition
{
  private ResearchPhaseDefinition(Match match)
  {
    Steps =
    [
      new AgentPhaseStep(new ResearchPrimaryStep(match), PersistTranscript: true),
      new AgentPhaseStep(new PaperBetFollowUpStep(match.Id), PersistTranscript: false),
    ];
  }

  public AgentSessionPhase Phase => AgentSessionPhase.Research;
  public IReadOnlyList<AgentPhaseStep> Steps { get; }

  public static ResearchPhaseDefinition ForMatch(Match match)
    => new(match);

  private sealed class ResearchPrimaryStep(Match match) : IAgentPhaseStep
  {
    public string BuildPrompt() => $"""
          Today is {DateOnly.FromDateTime(DateTime.UtcNow)}.
          You are a long-running betting agent with persistent memory.
          
          You are now conducting match research for yourself, to support your own later betting decision on this match:
          - Match ID: {match.Id}
          - Fixture: {match.HomeClub.Name} (ID: {match.HomeClub.Id}) vs {match.AwayClub.Name} (ID: {match.AwayClub.Id})
          - Kickoff (UTC): {match.MatchDate:yyyy-MM-dd HH:mm}
          
          Important context:
          You are NOT reacting directly to live betting market movements or line shifts during this research phase.
          Because of this, you should assume you do NOT have a timing-based market edge (no late line movement advantage, no sharp market reaction signals).
          Your edge must come only from structural, statistical, tactical, or contextual analysis—not from market positioning or timing.
          
          Goal:
          Create complete, decision-oriented research for this specific match that you will later use in your own betting phase.
          This is your personal prep work: your future self in the betting phase should be able to read this and decide whether to bet or pass.
          
          You must use the available AgentResearchPlugin functions explicitly.

          ## Required workflow (execute in order)

          1) Read memory context first:
          - Call `GetMemoryRecordsAsync`
          - Call `ReadMemoryAsync` for relevant records before new analysis

          2) Build core match intelligence:
          - `GetLineupsAsync`
          - `GetInjuriesAsync`
          - `GetHead2HeadStatsAsync`
          - `GetMatchBettingOddsHistoryAsync`
          - `GetLeagueTableAsync`

          3) Build team-level context for both clubs (home and away):
          - `GetClubStatistics`
          - `GetClubRollingPerformanceAsync`
          - `GetClubRecentGamesAsync`
          - `GetClubDailySummaryAsync`

          4) Build news and sentiment context:
          - If needed, call `SearchNewsAsync` to gather the latest news for both the home club and the away club.
          - If additional validation or deeper tactical/context insight is required, you may call `GetWebGroundingAsync` to verify key claims.
          - Focus on separating meaningful signals from noise, assessing source reliability, and spotting potential market overreactions or underreactions.

          5) Synthesize decision-oriented research output stating clear betting implications, including potential value angles and confidence drivers

          6) Save learnings for your future self:
          - Persist reusable insights, patterns, and hypotheses using `AppendMemoryAsync`, `ReplaceMemoryAsync`, or `WriteMemoryAsync`
          - Keep memories concise, structured, and directly useful for your own future research and betting decisions
          - Do not store raw noisy dumps; store distilled insights
          - If needed create new memories with `WriteMemoryAsync`

          7) Completion gate (mandatory):
          - Create one complete final report text for this match - it must be brief and scannable - try to keep it under 500 words.
          - Call `SaveMatchAnalysisAsync` with this match id and the final report content
          - Do not terminate until `SaveMatchAnalysisAsync` succeeds

          8) Finish with short summary of key insights and betting implications.

          ## Quality constraints
          - Be analytical and evidence-driven
          - Cross-check important claims
          - If data is missing, state it explicitly and continue with best-effort reasoning
          - Do not skip required steps

          ### Guardrails
          - Focus on delivering the research output as if for a human analyst, not on describing your own process.
          """;

    public IReadOnlyList<AITool> GetTools(IPluginFactory plugins) =>
      AgentToolFactory.CreateFromObject(plugins.CreateAgentResearchPlugin());
  }

  private sealed class PaperBetFollowUpStep(int matchId) : IAgentPhaseStep
  {
    public string BuildPrompt() => """
          Your next step is to create a **paper (fictional) prediction slip** for this match.

          Purpose:
          - This is a **research artifact**, not a real bet.
          - It does NOT affect bankroll in any way.
          - Odds are intentionally unavailable — ignore pricing completely.
          - Your only goal is to **maximize correctness of predictions**.

          Core Instructions:
          - Base all selections strictly on your prior research.
          - Single selections are ok but multiple selections (parlays) are preferred.

          STRICT Tool Flow (must follow exactly):
          1) Call `GetMatchBasicInfo` - to get home/away club ids and names for this match
          2) Call `GetMatchEvents` - to get the available markets and outcome option names
          3) Call `PlaceBetSlip` - to place the slip

          Selection Rules (VERY IMPORTANT):
          - Do NOT include **contradictory or overlapping selections**.
          - Avoid combining markets that express the same dimension in conflicting ways.
          - You cannot select multiple options from the same market.
          """;

    public IReadOnlyList<AITool> GetTools(IPluginFactory plugins) =>
      AgentToolFactory.CreateFromObject(plugins.CreateResearchBetPlugin(matchId));
  }
}
