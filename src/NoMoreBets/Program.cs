using Hangfire;
using Hangfire.Dashboard;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NoMoreBets.Application;
using NoMoreBets.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHangfireConfiguration(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
  c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
});

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions()
{
  ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
  Authorization = [new AllowAllDashboardAuthorizationFilter()]
});

HangfireConfiguration.UseRecurringJobs();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public class AllowAllDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
  public bool Authorize(DashboardContext context)
  {
    return true;
  }
}
