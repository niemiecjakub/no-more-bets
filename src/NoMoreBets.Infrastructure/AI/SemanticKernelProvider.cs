using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Plugins;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.AI;
public static class SemanticKernelProvider
{
  public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services)
  {
    services.AddSingleton<ThreadProvider>();
    services.AddScoped<IPluginFactory, PluginFactory>();
    services.AddScoped<Kernel>(sp =>
    {
      var builder = Kernel.CreateBuilder();
      var openAi = sp.GetRequiredService<IOptions<OpenAIOptions>>().Value;
      string modelId = openAi.ModelId;
      string apiKey = openAi.ApiKey;
      builder.AddOpenAIChatCompletion(modelId, apiKey);
      return builder.Build();
    });

    services.AddScoped<ContextBuilder>();
    services.AddScoped<AgentBuilder>();
    services.AddScoped<Runner>();

    return services;
  }
}
