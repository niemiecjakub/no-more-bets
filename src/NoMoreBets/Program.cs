using NoMoreBets.Infrastructure.ExternalClients;
using NoMoreBets.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<JsonCacheOptions>(builder.Configuration.GetSection("StorageCache:JsonCache"));
builder.Services.Configure<HtmlCacheOptions>(builder.Configuration.GetSection("StorageCache:HtmlCache"));
builder.Services.Configure<BaseScraperOptions>(builder.Configuration.GetSection("Scraper"));
builder.Services.AddSingleton<IJsonCache, JsonCache>();
builder.Services.AddSingleton<IHtmlCache, HtmlCache>();
builder.Services.AddSingleton<IPageFetcher, PlaywrightPageFetcher>();
builder.Services.AddSingleton<IRotowireScraper, RotowireScraper>();

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
