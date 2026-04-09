using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchPrediction;
using NoMoreBets.Infrastructure.AI.Plugins;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.AI;
public static class SemanticKernelProvider
{
  public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services)
  {
    services.AddSingleton<ThreadProvider>();
    services.AddScoped<IPluginFactory, PluginFactory>();
    services.AddScoped<IMatchPrediction, AIGateway>();
    services.AddScoped<ContextBuilder>();
    services.AddScoped<AgentBuilder>();
    services.AddScoped<Runner>();
    services.AddScoped<IAgentPhaseRunner>(sp => sp.GetRequiredService<Runner>());

    return services;
  }
}
