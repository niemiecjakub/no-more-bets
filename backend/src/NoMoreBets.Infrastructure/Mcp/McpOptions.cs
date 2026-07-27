namespace NoMoreBets.Infrastructure.Mcp;

public sealed class McpOptions
{
  public const string SectionName = "Mcp";

  /// <summary>When false, MCP services and endpoint are not registered.</summary>
  public bool Enabled { get; set; } = true;

  public string ServerName { get; set; } = "no-more-bets";

  public string ServerVersion { get; set; } = "1.0.0";
}
