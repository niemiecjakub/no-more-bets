using System.Net;
using Polly;
using Hangfire;
using Hangfire.PostgreSql;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Features.Betclic.Scraping;
using NoMoreBets.Features.Fotmob.Scraping;
using NoMoreBets.Features.MatchAnalysis.MatchMatcher;
using NoMoreBets.Features.MatchAnalysis.Options;
using NoMoreBets.Features.MatchAnalysis.Persistence;
using NoMoreBets.Features.Prediction.Plugins;
using NoMoreBets.Features.Prediction.PredictMatch;
using NoMoreBets.Features.Rotowire.Scraping;
using NoMoreBets.Features.SoccerData;
using NoMoreBets.Infrastructure.Database;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Http;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Features.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContextFactory<AppDbContext>(options =>
  options.UseNpgsql(dbConnectionString, o =>
    {
      o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    }));

builder.Services.AddHealthChecks()
  .AddNpgSql(dbConnectionString, tags: ["db"])
  .AddDbContextCheck<AppDbContext>(tags: ["dbContext"]);

builder.Services.Configure<BaseScraperOptions>(builder.Configuration.GetSection("Scraper"));
builder.Services.Configure<BetclicScraperOptions>(builder.Configuration.GetSection("Scraper:Betclic"));
builder.Services.Configure<SoccerDataOptions>(builder.Configuration.GetSection("SoccerData"));
builder.Services.Configure<MatchAnalysisOptions>(builder.Configuration.GetSection("MatchAnalysis"));
builder.Services.Configure<OpenAiAgentOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddSingleton<IMatchMatcher, MatchMatcher>();
builder.Services.AddSingleton<IMatchAnalysisPersistence, FileMatchAnalysisPersistence>();
builder.Services.AddSingleton<FootballDataPlugin>();
builder.Services.AddSingleton<SquadPlugin>();
builder.Services.AddSingleton<BookmakerPlugin>();
builder.Services.AddSingleton<IPredictMatchAgentOrchestrator, PredictMatchAgentOrchestrator>();
builder.Services.AddSingleton<PlaywrightPageFetcher>();
builder.Services.AddScoped<Initialize>();
builder.Services.AddSingleton<RotowireScraper>();
builder.Services.AddSingleton<BetclicScraper>();
builder.Services.AddSingleton<FotmobScraper>();
builder.Services.AddSingleton<ResiliencePipeline<HttpResponseMessage>>(sp =>
  ResilienceHttpHandler.CreatePipeline(sp.GetService<ILogger<ResilienceHttpHandler>>()));
builder.Services.AddTransient<ResilienceHttpHandler>();
builder.Services.AddHttpClient<SoccerDataClient>()
  .AddHttpMessageHandler<ResilienceHttpHandler>()
  .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
  {
    AutomaticDecompression = DecompressionMethods.All
  });

builder.Services.AddOptions<SoccerDataOptions>()
    .Bind(builder.Configuration.GetSection("SoccerData"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "SoccerData:ApiKey is required")
    .ValidateOnStart();

builder.Services.AddHangfire(config => config
  .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
  .UseSimpleAssemblyNameTypeSerializer()
  .UseRecommendedSerializerSettings()
  .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(dbConnectionString)));
builder.Services.AddHangfireServer();
builder.Services.AddScoped<JobService>();

var app = builder.Build();

DbInitializer.Initialize(dbConnectionString);

app.MapHealthChecks("/health", new HealthCheckOptions()
{
  ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

using (var scope = app.Services.CreateScope())
{
  var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
  // Runs once per day at 18:00
  recurringJobManager.AddOrUpdate<JobService>(
    "update-match-data",
    jobService => jobService.GetUpcommingSoccerdataMatches(SoccerDataConstants.PremierLeagueId, new()),
    "0 18 * * *");

  // Runs once per day at 15:00
  recurringJobManager.AddOrUpdate<JobService>(
    "update-lineups",
    jobService => jobService.GetBetclicGames(new()),
    "0 15 * * *");

  // Runs once per day at 16:00
  recurringJobManager.AddOrUpdate<JobService>(
    "update-lineups",
    jobService => jobService.GetLineups(new()),
    "0 16 * * *");

  // Runs once per day at 10:00
  recurringJobManager.AddOrUpdate<JobService>(
    "update-league-table",
    jobService => jobService.GetLeagueTable(new()),
    "0 10 * * *");

  // Runs hourly at minute 0
  recurringJobManager.AddOrUpdate<JobService>(
    "close-starting-soon-matches",
    jobService => jobService.CloseStartingSoonMatches(new()),
    "0 * * * *");
}

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire");
app.MapControllers();

app.Run();
