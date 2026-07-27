using ModelContextProtocol.Protocol;
using NoMoreBets.Infrastructure.AI.Mcp;
using NoMoreBets.Infrastructure.Mcp;

namespace NoMoreBets.Mcp;

public static class McpHostExtensions
{
  public static WebApplicationBuilder AddNoMoreBetsMcp(this WebApplicationBuilder builder)
  {
    var section = builder.Configuration.GetSection(McpOptions.SectionName);
    builder.Services.Configure<McpOptions>(section);

    var options = section.Get<McpOptions>() ?? new McpOptions();
    if (!options.Enabled)
    {
      return builder;
    }

    builder.Services
      .AddMcpServer(serverOptions =>
      {
        serverOptions.ServerInfo = new Implementation
        {
          Name = options.ServerName,
          Version = options.ServerVersion,
        };
      })
      .WithHttpTransport(options =>
      {
        options.Stateless = true;
      })
      .WithTools<MatchMcpTools>();

    return builder;
  }

  public static WebApplication MapNoMoreBetsMcp(this WebApplication app)
  {
    var options = app.Configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();
    if (!options.Enabled)
    {
      return app;
    }

    app.MapMcp("/mcp");
    return app;
  }
}
