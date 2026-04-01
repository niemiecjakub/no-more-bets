using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.Common;
using NoMoreBets.Infrastructure.AI.Plugins;
using NoMoreBets.Infrastructure.AI.Provider;

namespace NoMoreBets.Infrastructure.AI;
public static class SemanticKernelProvider
{
  public static IServiceCollection AddSemanticKernelServices(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddSingleton<IPluginFactory, PluginFactory>();
    services.AddScoped<Kernel>(sp =>
    {
      var builder = Kernel.CreateBuilder();
      var config = sp.GetRequiredService<IConfiguration>();
      string modelId = config["OpenAI:ModelId"] ?? throw new ArgumentNullException("OpenAI ModelId is missing");
      string apiKey = config["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI ApiKey is missing");
      builder.AddOpenAIChatCompletion(modelId, apiKey);
      return builder.Build();
    });

    services.AddSingleton<ContextBuilder>();
    services.AddSingleton<AgentBuilder>();
    services.AddSingleton<Runner>();

    return services;
  }
}
