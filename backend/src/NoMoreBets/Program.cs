using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NoMoreBets.Application;
using NoMoreBets.Infrastructure;
using NoMoreBets.Infrastructure.Persistence;
using NoMoreBets.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryObservability();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireConfiguration(builder.Configuration);
builder.Services.AddControllers()
  .AddJsonOptions(options =>
  {
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
  });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});

string[] allowedOrigins = builder.Configuration["AllowedOrigins"]!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowFrontend", policy =>
  {
    policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod();
  });
});

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions()
{
  ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
  Authorization =
  [
    new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
    {
      RequireSsl = false,
      SslRedirect = false,
      Users =
      [
        new BasicAuthAuthorizationUser
        {
          Login = builder.Configuration["Hangfire:Dashboard:Login"],
          PasswordClear = builder.Configuration["Hangfire:Dashboard:Password"]
        }
      ]
    })
  ]
});

app.UseRecurringJobs();

using (var scope = app.Services.CreateScope())
{
  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
  DbInitializer.Initialize(connectionString);
}

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.Run();
