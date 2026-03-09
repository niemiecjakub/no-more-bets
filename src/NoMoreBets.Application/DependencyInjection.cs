using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Application.Common.MatchMatcher;

namespace NoMoreBets.Application;

public static class DependencyInjection
{
  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
    services.AddSingleton<IMatchMatcher, MatchMatcher>();

    return services;
  }
}
