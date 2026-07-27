using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using NoMoreBets.Infrastructure.Mcp.Tools;

namespace NoMoreBets.Infrastructure.Mcp;

/// <summary>Lists MCP tool metadata from <see cref="McpServerToolAttribute"/> on tool types.</summary>
public static class McpToolCatalog
{
  private static readonly (Type Type, string Id, string Label, string Description)[] Groups =
  [
    (
      typeof(MatchesMcpTools),
      "matches",
      "Matches",
      "Search fixtures and pull research, lineups, odds, and events."),
    (
      typeof(ClubsMcpTools),
      "clubs",
      "Clubs",
      "Resolve clubs and inspect form, fixtures, and rolling performance."),
    (
      typeof(LeaguesMcpTools),
      "leagues",
      "Leagues",
      "Standings and club-level league statistics."),
  ];

  public static IReadOnlyList<McpToolGroupDto> ListGroups()
  {
    return Groups
      .Select(group => new McpToolGroupDto(
        group.Id,
        group.Label,
        group.Description,
        ListTools(group.Type)))
      .ToList();
  }

  private static IReadOnlyList<McpToolDto> ListTools(Type toolType)
  {
    return toolType
      .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
      .Select(method => (Method: method, Attr: method.GetCustomAttribute<McpServerToolAttribute>()))
      .Where(x => x.Attr != null)
      .Select(x => new McpToolDto(
        x.Attr!.Name ?? x.Method.Name,
        x.Attr.Title ?? x.Attr.Name ?? x.Method.Name,
        x.Method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty))
      .ToList();
  }
}

public sealed record McpToolDto(string Name, string Title, string Description);

public sealed record McpToolGroupDto(
  string Id,
  string Label,
  string Description,
  IReadOnlyList<McpToolDto> Tools);
