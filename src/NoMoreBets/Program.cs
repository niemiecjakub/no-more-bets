using NoMoreBets.Features.Betclic.Scraping;
using NoMoreBets.Features.Fotmob.Scraping;
using NoMoreBets.Features.Rotowire.Scraping;
using NoMoreBets.Infrastructure.Fetching;
using NoMoreBets.Infrastructure.Scraping;
using NoMoreBets.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JsonCacheOptions>(builder.Configuration.GetSection("StorageCache:JsonCache"));
builder.Services.Configure<HtmlCacheOptions>(builder.Configuration.GetSection("StorageCache:HtmlCache"));
builder.Services.Configure<BaseScraperOptions>(builder.Configuration.GetSection("Scraper"));
builder.Services.Configure<BetclicScraperOptions>(builder.Configuration.GetSection("Scraper:Betclic"));
builder.Services.Configure<FotmobScraperOptions>(builder.Configuration.GetSection("Scraper:Fotmob"));
builder.Services.AddSingleton<IJsonCache, JsonCache>();
builder.Services.AddSingleton<IHtmlCache, HtmlCache>();
builder.Services.AddSingleton<PlaywrightPageFetcher>();
builder.Services.AddSingleton<IPageFetcher>(sp => sp.GetRequiredService<PlaywrightPageFetcher>());
builder.Services.AddSingleton<IInteractivePageFetcher>(sp => sp.GetRequiredService<PlaywrightPageFetcher>());
builder.Services.AddSingleton<IRotowireScraper, RotowireScraper>();
builder.Services.AddSingleton<IBetclicScraper, BetclicScraper>();
builder.Services.AddSingleton<IFotmobScraper, FotmobScraper>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
