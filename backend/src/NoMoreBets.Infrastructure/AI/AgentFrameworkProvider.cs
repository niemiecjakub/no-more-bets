using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Phases.Betting;
using NoMoreBets.Infrastructure.AI.Phases.InternetResearch;
using NoMoreBets.Infrastructure.AI.Phases.MemoryCleanup;
using NoMoreBets.Infrastructure.AI.Phases.Reflection;
using NoMoreBets.Infrastructure.AI.Phases.Research;
using NoMoreBets.Infrastructure.AI.Phases.Test;
using NoMoreBets.Infrastructure.AI.Tools.Implementations;

namespace NoMoreBets.Infrastructure.AI;

public static class AgentFrameworkProvider
{
  public static IServiceCollection AddAgentFrameworkServices(this IServiceCollection services)
  {
    services.AddScoped<MatchTool>();
    services.AddScoped<BettingTool>();
    services.AddScoped<SocialMediaTool>();
    services.AddScoped<AgentBuilder>();
    services.AddScoped<AgentSessionContext>();
    services.AddScoped<AgentPhaseExecutor>();
    services.AddScoped<InternetResearchPhase>();
    services.AddScoped<MemoryCleanupPhase>();
    services.AddScoped<ReflectionPhase>();
    services.AddScoped<ResearchPhaseRunner>();
    services.AddScoped<InternetResearchPhaseRunner>();
    services.AddScoped<MemoryCleanupPhaseRunner>();
    services.AddScoped<ReflectionPhaseRunner>();
    services.AddScoped<TestPhaseRunner>();
    services.AddScoped<BettingPhaseRunner>();
    services.AddScoped<AgentPhaseRunner>();
    services.AddScoped<IAgentPhaseRunner>(sp => sp.GetRequiredService<AgentPhaseRunner>());

    return services;
  }
}
