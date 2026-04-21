using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NoMoreBets.Application.Betting;
using NoMoreBets.Application.Clubs;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Fotmob;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Application.Matches.BackfillRecentFotmobMatchDetails;
using NoMoreBets.Application.Matches;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Domain.Memories;
using NoMoreBets.Infrastructure.AI;
using NoMoreBets.Infrastructure.BackgroundJobs;
using NoMoreBets.Infrastructure.Http;
using NoMoreBets.Infrastructure.Persistence;
using NoMoreBets.Infrastructure.Persistence.Repositories;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Scraping.BrowserAutomation;
using NoMoreBets.Infrastructure.Scraping.External.Betclic;
using NoMoreBets.Infrastructure.Scraping.External.Fotmob;
using NoMoreBets.Infrastructure.Scraping.External.Rotowire;
using NoMoreBets.Infrastructure.Scraping.External.SoccerData;
using NoMoreBets.Infrastructure.Search;
using NoMoreBets.Infrastructure.XApi;
using Polly;
using System.Net;
using System.Net.Http.Headers;

namespace NoMoreBets.Infrastructure;

public static class DependencyInjection
{
  public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    // DbContext
    services.AddDbContext<AppDbContext>(options =>
      options.UseNpgsql(connectionString, o =>
      {
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
      }));

    // Health checks
    services.AddHealthChecks()
      .AddNpgSql(connectionString, tags: ["db"])
      .AddDbContextCheck<AppDbContext>(tags: ["dbContext"]);

    // Options (Infrastructure)
    services.Configure<BaseScraperOptions>(configuration.GetSection(BaseScraperOptions.SectionName));
    services.Configure<BetclicScraperOptions>(configuration.GetSection(BetclicScraperOptions.SectionName));
    services.Configure<SoccerDataOptions>(configuration.GetSection(SoccerDataOptions.SectionName));
    services.Configure<ProxyOptions>(configuration.GetSection(ProxyOptions.SectionName));
    services.Configure<BraveSearchOptions>(configuration.GetSection(BraveSearchOptions.SectionName));
    services.Configure<BackfillFotmobMatchDetailsOptions>(configuration.GetSection(BackfillFotmobMatchDetailsOptions.SectionName));
    services.Configure<XApiOptions>(configuration.GetSection(XApiOptions.SectionName));
    services.Configure<OpenAIOptions>(configuration.GetSection(OpenAIOptions.SectionName));

    services.AddOptions<SoccerDataOptions>()
      .Bind(configuration.GetSection(SoccerDataOptions.SectionName))
      .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "SoccerData:ApiKey is required")
      .ValidateOnStart();

    // Repositories & Unit of Work
    services.AddScoped<IBettingRepository, BettingRepository>();
    services.AddScoped<IMatchRepository, MatchRepository>();
    services.AddScoped<IClubRepository, ClubRepository>();
    services.AddScoped<ILeagueRepository, LeagueRepository>();
    services.AddScoped<IMemoryRepository, MemoryRepository>();
    services.AddScoped<IBankrollRepository, BankrollRepository>();
    services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();
    services.AddScoped<IUnitOfWork, UnitOfWork>();

    services.AddSemanticKernelServices();

    // Browser automation
    services.AddSingleton<PlaywrightBrowserService>();
    services.AddTransient<PlaywrightPageFetcher>();

    //Jobs
    services.AddScoped<PreKickoffDataSyncJobService>();
    services.AddScoped<BankrollJobService>();
    services.AddScoped<LeagueTableJobService>();
    services.AddScoped<LineupJobService>();
    services.AddScoped<ClubDailyBriefJobService>();
    services.AddScoped<MatchLifecycleJobService>();
    services.AddScoped<BookmakerListingSyncJobService>();
    services.AddScoped<FinishedMatchScoreJobService>();
    services.AddScoped<MatchPredictionJobService>();
    services.AddScoped<BettingAgentCronService>();
    services.AddScoped<UpcomingMatchesInternetResearchCronService>();
    services.AddScoped<MemoryCleanupCronService>();

    //HTTP resilience & external clients
    services.AddSingleton<ResiliencePipeline<HttpResponseMessage>>(sp =>
      ResilienceHttpHandler.CreatePipeline(sp.GetService<ILogger<ResilienceHttpHandler>>()));
    services.AddTransient<ResilienceHttpHandler>();
    services.AddTransient<XApiOAuth1MessageHandler>();
    services.AddHttpClient<SoccerDataClient>()
      .AddHttpMessageHandler<ResilienceHttpHandler>()
      .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
      {
        AutomaticDecompression = DecompressionMethods.All
      });

    // Scrapers
    services.AddSingleton<IFotmobConstants, FotmobConstants>();
    services.AddSingleton<IFotmobTeamLookup>(sp => (IFotmobTeamLookup)sp.GetRequiredService<IFotmobConstants>());
    services.AddSingleton<RotowireScraper>();
    services.AddSingleton<BetclicScraper>();
    services.AddSingleton<FotmobScraper>();

    // Provider interfaces
    services.AddTransient<IUpcommingMatchProvider>(sp => sp.GetRequiredService<SoccerDataClient>());
    services.AddTransient<IMatchPreviewProvider>(sp => sp.GetRequiredService<SoccerDataClient>());
    services.AddTransient<IHeadToHeadProvider>(sp => sp.GetRequiredService<SoccerDataClient>());
    services.AddTransient<ILineupProvider>(sp => sp.GetRequiredService<RotowireScraper>());
    services.AddTransient<ILeagueProvider>(sp => sp.GetRequiredService<FotmobScraper>());
    services.AddTransient<IClubOverviewProvider>(sp => sp.GetRequiredService<FotmobScraper>());
    services.AddTransient<IMatchDetailsProvider>(sp => sp.GetRequiredService<FotmobScraper>());
    services.AddTransient<IBookmakerMatchesProvider>(sp => sp.GetRequiredService<BetclicScraper>());
    services.AddTransient<IBetEventsProvider>(sp => sp.GetRequiredService<BetclicScraper>());

    services.AddHttpClient<ISearchService, BraveSearch>((serviceProvider, client) =>
    {
      var options = serviceProvider.GetRequiredService<IOptions<BraveSearchOptions>>().Value;
      client.BaseAddress = new Uri("https://api.search.brave.com");
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      client.DefaultRequestHeaders.TryAddWithoutValidation("X-Subscription-Token", options.ApiKey);
    })
    .AddHttpMessageHandler<ResilienceHttpHandler>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
      AutomaticDecompression = DecompressionMethods.All
    });

    services.AddHttpClient<IXApiService, XApiClient>((_, client) =>
    {
      client.BaseAddress = new Uri("https://api.x.com");
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    })
    .AddHttpMessageHandler<XApiOAuth1MessageHandler>()
    .AddHttpMessageHandler<ResilienceHttpHandler>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
      AutomaticDecompression = DecompressionMethods.All
    });

    return services;
  }
}
