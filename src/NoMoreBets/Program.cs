using System.Net;
using Hangfire;
using Hangfire.PostgreSql;
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
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Storage;
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

builder.Services.Configure<JsonCacheOptions>(builder.Configuration.GetSection("StorageCache:JsonCache"));
builder.Services.Configure<HtmlCacheOptions>(builder.Configuration.GetSection("StorageCache:HtmlCache"));
builder.Services.Configure<BaseScraperOptions>(builder.Configuration.GetSection("Scraper"));
builder.Services.Configure<BetclicScraperOptions>(builder.Configuration.GetSection("Scraper:Betclic"));
builder.Services.Configure<FotmobScraperOptions>(builder.Configuration.GetSection("Scraper:Fotmob"));
builder.Services.Configure<SoccerDataOptions>(builder.Configuration.GetSection("SoccerData"));
builder.Services.Configure<MatchAnalysisOptions>(builder.Configuration.GetSection("MatchAnalysis"));
builder.Services.Configure<OpenAiAgentOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddSingleton<IMatchMatcher, MatchMatcher>();
builder.Services.AddSingleton<IMatchAnalysisPersistence, FileMatchAnalysisPersistence>();
builder.Services.AddSingleton<FootballDataPlugin>();
builder.Services.AddSingleton<SquadPlugin>();
builder.Services.AddSingleton<BookmakerPlugin>();
builder.Services.AddSingleton<IPredictMatchAgentOrchestrator, PredictMatchAgentOrchestrator>();
builder.Services.AddSingleton<IJsonCache, JsonCache>();
builder.Services.AddSingleton<IHtmlCache, HtmlCache>();
builder.Services.AddSingleton<PlaywrightPageFetcher>();
builder.Services.AddScoped<Initialize>();
builder.Services.AddSingleton<IPageFetcher>(sp => sp.GetRequiredService<PlaywrightPageFetcher>());
builder.Services.AddSingleton<IInteractivePageFetcher>(sp => sp.GetRequiredService<PlaywrightPageFetcher>());
builder.Services.AddSingleton<IRotowireScraper, RotowireScraper>();
builder.Services.AddSingleton<IBetclicScraper, BetclicScraper>();
builder.Services.AddSingleton<IFotmobScraper, FotmobScraper>();
builder.Services.AddHttpClient<ISoccerDataClient, SoccerDataClient>()
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

using (var scope = app.Services.CreateScope())
{
  var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
  //recurringJobManager.AddOrUpdate<JobService>(
  //  "update-match-data",
  //  jobService => jobService.GetUpcommingSoccerdataMatches(SoccerDataConstants.PremierLeagueId, new()),
  //  "*/10 * * * *");

  //recurringJobManager.AddOrUpdate<JobService>(
  //  "update-lineups",
  //  jobService => jobService.GetLineups(new()),
  //  "*/1 * * * *");

  //recurringJobManager.AddOrUpdate<JobService>(
  //  "update-lineups",
  //  jobService => jobService.GetLeagueTable(new()),
  //  "*/1 * * * *");
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
