using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
  {
    // Register DbContext
    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

    // Register repositories
    //services.AddScoped<IMatchRepository, MatchRepository>();
    //services.AddScoped<IBettingRepository, BettingRepository>();
    //services.AddScoped<IPredictionRepository, PredictionRepository>();

    // Register external providers (scrapers, HTTP clients)
    // services.AddHttpClient<IFotmobClient, FotmobClient>();

    return services;
  }
}