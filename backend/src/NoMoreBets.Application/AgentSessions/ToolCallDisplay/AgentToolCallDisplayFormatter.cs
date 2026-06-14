using System.Text.Json;
using System.Text.RegularExpressions;
using NoMoreBets.Application.AgentSessions.GetAgentSessionMessages;
using NoMoreBets.Application.AgentTools;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using DomainMatch = NoMoreBets.Domain.Matches.Match;

namespace NoMoreBets.Application.AgentSessions.ToolCallDisplay;

public sealed class AgentToolCallDisplayFormatter(IUnitOfWork unitOfWork)
{
  private static readonly IReadOnlyDictionary<string, AgentToolDefinition> ToolByName =
    AgentToolCatalog.All.ToDictionary(t => t.Name, StringComparer.Ordinal);

  public async Task<IReadOnlyDictionary<int, ToolCallDisplayDto>> BuildDisplayByMessageIdAsync(
    int sessionId,
    IReadOnlyList<AgentSessionMessage> messages,
    CancellationToken cancellationToken)
  {
    var functionMessages = messages
      .Where(m => m.Kind == AgentSessionMessageKind.FunctionCall)
      .ToList();

    if (functionMessages.Count == 0)
      return new Dictionary<int, ToolCallDisplayDto>();

    var context = await BuildContextAsync(sessionId, functionMessages, cancellationToken).ConfigureAwait(false);
    var result = new Dictionary<int, ToolCallDisplayDto>(functionMessages.Count);

    foreach (var message in functionMessages)
    {
      if (!FunctionCallPayloadParser.TryParse(message.Text, out var payload))
        continue;

      result[message.Id] = Format(payload, context, message.Metadata);
    }

    return result;
  }

  private async Task<ToolCallDisplayContext> BuildContextAsync(
    int sessionId,
    IReadOnlyList<AgentSessionMessage> functionMessages,
    CancellationToken cancellationToken)
  {
    var matchIds = new HashSet<int>();
    foreach (var message in functionMessages)
    {
      if (!FunctionCallPayloadParser.TryParse(message.Text, out var payload))
        continue;

      CollectMatchIds(payload, matchIds);
    }

    var sessionMatchIds = await unitOfWork.AgentSessions
      .GetMatchIdsBySessionIdsAsync([sessionId], cancellationToken)
      .ConfigureAwait(false);

    sessionMatchIds.TryGetValue(sessionId, out var sessionMatchId);
    if (sessionMatchId > 0)
      matchIds.Add(sessionMatchId);

    var betSlips = await unitOfWork.Betting
      .GetBetSlipsByAgentSessionIdAsync(sessionId, cancellationToken)
      .ConfigureAwait(false);

    foreach (var slip in betSlips)
    {
      foreach (var selection in slip.Selections)
        matchIds.Add(selection.MatchId);
    }

    var matches = matchIds.Count > 0
      ? await unitOfWork.Matches.GetMatchesByIdsAsync(matchIds.ToList(), cancellationToken).ConfigureAwait(false)
      : [];

    return ToolCallDisplayContext.Create(sessionMatchId, betSlips, matches);
  }

  private static void CollectMatchIds(FunctionCallPayload payload, ISet<int> matchIds)
  {
    var matchId = FunctionCallPayloadParser.ParsePositiveInt(
      FunctionCallPayloadParser.GetArgumentValue(payload, "matchId"));
    if (matchId is > 0)
      matchIds.Add(matchId.Value);

    foreach (var selection in ParseBetSelections(
      FunctionCallPayloadParser.GetArgumentValue(payload, "betSelectionsJson")))
    {
      if (selection.MatchId is > 0)
        matchIds.Add(selection.MatchId.Value);
    }
  }

  internal static ToolCallDisplayDto Format(
    FunctionCallPayload payload,
    ToolCallDisplayContext context,
    string? metadata = null)
  {
    var toolDef = ToolByName.GetValueOrDefault(payload.Name);
    var label = toolDef?.DisplayName ?? payload.Name;
    var category = toolDef?.Category.ToSlug() ?? "unknown";
    var details = FormatDetails(payload, toolDef, context);
    var toolMetadata = FormatMetadata(toolDef, metadata);

    return new ToolCallDisplayDto(label, category, details, toolMetadata);
  }

  private static IReadOnlyList<ToolCallMetadataDto>? FormatMetadata(
    AgentToolDefinition? toolDef,
    string? metadata)
  {
    if (toolDef?.Category != AgentToolCategory.WebSearch)
      return null;

    var sources = WebSearchToolMetadataParser.Parse(metadata)
      .Select(source => new WebSearchSourceLinkDto(source.Title, source.Hostname, source.Url))
      .ToList();

    return sources.Count > 0
      ? [new WebSearchSourcesToolCallMetadataDto(sources)]
      : null;
  }

  private static IReadOnlyList<string>? FormatDetails(
    FunctionCallPayload payload,
    AgentToolDefinition? toolDef,
    ToolCallDisplayContext context)
  {
    var lines = new List<string>();

    var matchId = FunctionCallPayloadParser.ParsePositiveInt(
      FunctionCallPayloadParser.GetArgumentValue(payload, "matchId"));
    if (matchId is > 0)
      lines.Add(context.ResolveMatchLabel(matchId.Value));

    var clubId = FunctionCallPayloadParser.ParsePositiveInt(
      FunctionCallPayloadParser.GetArgumentValue(payload, "clubId"));
    if (clubId is > 0)
      lines.Add(context.ResolveClubLabel(clubId.Value));

    var stakeAmount = FunctionCallPayloadParser.GetArgumentValue(payload, "stakeAmount");
    if (stakeAmount is not null)
    {
      var stakeText = FunctionCallPayloadParser.ParseString(stakeAmount);
      if (!string.IsNullOrWhiteSpace(stakeText))
        lines.Add($"Stake: {stakeText}");
    }

    var betSelections = ParseBetSelections(
      FunctionCallPayloadParser.GetArgumentValue(payload, "betSelectionsJson"));
    if (betSelections.Count > 0)
      lines.AddRange(FormatBetSelectionLines(betSelections, context));

    foreach (var argumentName in new[] { "query", "name" })
    {
      var value = FunctionCallPayloadParser.ParseString(
        FunctionCallPayloadParser.GetArgumentValue(payload, argumentName));
      if (!string.IsNullOrWhiteSpace(value))
        lines.Add(value);
    }

    foreach (var argumentName in new[] { "text", "content" })
    {
      var value = FunctionCallPayloadParser.ParseString(
        FunctionCallPayloadParser.GetArgumentValue(payload, argumentName));
      if (!string.IsNullOrWhiteSpace(value))
        lines.Add(TruncateText(value));
    }

    var status = FormatBetStatus(FunctionCallPayloadParser.GetArgumentValue(payload, "status"));
    if (status is not null)
      lines.Add($"Status: {status}");

    if (FunctionCallPayloadParser.ParseBooleanTrue(
      FunctionCallPayloadParser.GetArgumentValue(payload, "includeExoticMarkets")))
    {
      lines.Add("Including exotic markets");
    }

    if (toolDef?.UsesSessionMatch == true && lines.Count == 0)
    {
      var sessionMatch = context.SessionMatchLabel;
      if (sessionMatch is not null)
        lines.Add(sessionMatch);
    }

    if (string.Equals(payload.Name, "researchbet_placeBetSlip", StringComparison.Ordinal)
      && betSelections.Count > 0)
    {
      lines.Add("Paper stake (research only)");
    }

    return lines.Count > 0 ? lines : null;
  }

  private static IReadOnlyList<string> FormatBetSelectionLines(
    IReadOnlyList<ParsedBetSelection> selections,
    ToolCallDisplayContext context)
  {
    var lines = new List<string>(selections.Count);
    foreach (var selection in selections)
    {
      var parts = new List<string>();
      if (selection.MatchId is > 0)
        parts.Add(context.ResolveMatchLabel(selection.MatchId.Value));

      var marketParts = new[] { selection.EventType, selection.EventOption }
        .Where(part => !string.IsNullOrWhiteSpace(part))
        .Select(HumanizeEnumLabel)
        .ToList();

      if (marketParts.Count > 0)
        parts.Add(string.Join(" · ", marketParts));

      lines.Add(parts.Count > 0 ? string.Join(" — ", parts) : "Selection");
    }

    return lines;
  }

  private static string? FormatBetStatus(JsonElement? value)
  {
    if (value is null)
      return null;

    if (value.Value.ValueKind == JsonValueKind.String)
    {
      var text = value.Value.GetString()?.Trim();
      if (string.IsNullOrWhiteSpace(text))
        return null;

      return Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
    }

    if (value.Value.ValueKind == JsonValueKind.Number && value.Value.TryGetInt32(out var statusId))
    {
      return statusId switch
      {
        1 => "Pending",
        2 => "Won",
        3 => "Lost",
        4 => "Canceled",
        _ => statusId.ToString(),
      };
    }

    return value.Value.GetRawText();
  }

  private static IReadOnlyList<ParsedBetSelection> ParseBetSelections(JsonElement? value)
  {
    if (value is not { } element)
      return [];

    JsonElement root = element;
    if (element.ValueKind == JsonValueKind.String)
    {
      var raw = element.GetString();
      if (string.IsNullOrWhiteSpace(raw))
        return [];

      try
      {
        using var document = JsonDocument.Parse(raw);
        root = document.RootElement.Clone();
      }
      catch (JsonException)
      {
        return [];
      }
    }

    if (root.ValueKind != JsonValueKind.Object)
      return [];

    if (!root.TryGetProperty("betSelections", out var selectionsElement)
      && !root.TryGetProperty("BetSelections", out selectionsElement)
      || selectionsElement.ValueKind != JsonValueKind.Array)
    {
      return [];
    }

    var selections = new List<ParsedBetSelection>();
    foreach (var item in selectionsElement.EnumerateArray())
    {
      if (item.ValueKind != JsonValueKind.Object)
        continue;

      int? matchId = null;
      if (item.TryGetProperty("matchId", out var matchIdElement)
        || item.TryGetProperty("MatchId", out matchIdElement))
      {
        matchId = FunctionCallPayloadParser.ParsePositiveInt(matchIdElement);
      }

      string? eventType = null;
      if (item.TryGetProperty("eventType", out var eventTypeElement)
        || item.TryGetProperty("EventType", out eventTypeElement))
      {
        eventType = FunctionCallPayloadParser.ParseString(eventTypeElement);
      }

      string? eventOption = null;
      if (item.TryGetProperty("eventOption", out var eventOptionElement)
        || item.TryGetProperty("EventOption", out eventOptionElement))
      {
        eventOption = FunctionCallPayloadParser.ParseString(eventOptionElement);
      }

      if (matchId is null && eventType is null && eventOption is null)
        continue;

      selections.Add(new ParsedBetSelection(matchId, eventType, eventOption));
    }

    return selections;
  }

  private static string HumanizeEnumLabel(string value) =>
    Regex.Replace(
      Regex.Replace(value.Replace('_', ' '), "([a-z])([A-Z])", "$1 $2"),
      "\\s+",
      " ").Trim();

  private static string TruncateText(string text, int max = 120)
  {
    var trimmed = text.Trim();
    if (trimmed.Length <= max)
      return trimmed;

    return trimmed[..max].TrimEnd() + "…";
  }

  private sealed record ParsedBetSelection(int? MatchId, string? EventType, string? EventOption);

  internal sealed class ToolCallDisplayContext
  {
    private readonly Dictionary<int, string> _matchLabels = new();
    private readonly Dictionary<int, string> _clubLabels = new();

    public string? SessionMatchLabel { get; private set; }

    public static ToolCallDisplayContext Create(
      int sessionMatchId,
      IReadOnlyList<BetSlip> betSlips,
      IReadOnlyList<DomainMatch> matches)
    {
      var context = new ToolCallDisplayContext();

      foreach (var match in matches)
        context.AddMatch(match);

      foreach (var slip in betSlips)
      {
        foreach (var selection in slip.Selections)
        {
          var match = selection.Match;
          if (match?.HomeClub is null || match.AwayClub is null)
            continue;

          context.AddMatchInfo(
            selection.MatchId,
            match.HomeClubId,
            match.AwayClubId,
            match.HomeClub.Name,
            match.AwayClub.Name);
        }
      }

      if (sessionMatchId > 0 && context._matchLabels.TryGetValue(sessionMatchId, out var sessionLabel))
        context.SessionMatchLabel = sessionLabel;

      return context;
    }

    private void AddMatch(DomainMatch match)
    {
      if (match.HomeClub is null || match.AwayClub is null)
        return;

      AddMatchInfo(match.Id, match.HomeClubId, match.AwayClubId, match.HomeClub.Name, match.AwayClub.Name);
    }

    private void AddMatchInfo(
      int matchId,
      int homeClubId,
      int awayClubId,
      string homeClubName,
      string awayClubName)
    {
      if (!_matchLabels.ContainsKey(matchId))
        _matchLabels[matchId] = FormatMatchTeamLabel(homeClubName, awayClubName, matchId);

      if (!_clubLabels.ContainsKey(homeClubId) && !string.IsNullOrWhiteSpace(homeClubName))
        _clubLabels[homeClubId] = homeClubName.Trim();

      if (!_clubLabels.ContainsKey(awayClubId) && !string.IsNullOrWhiteSpace(awayClubName))
        _clubLabels[awayClubId] = awayClubName.Trim();
    }

    public string ResolveMatchLabel(int matchId) =>
      _matchLabels.GetValueOrDefault(matchId) ?? $"Match #{matchId}";

    public string ResolveClubLabel(int clubId) =>
      _clubLabels.GetValueOrDefault(clubId) ?? $"Club #{clubId}";

    private static string FormatMatchTeamLabel(string homeClubName, string awayClubName, int matchId)
    {
      var home = homeClubName.Trim();
      var away = awayClubName.Trim();
      return home.Length > 0 && away.Length > 0 ? $"{home} vs {away}" : $"Match #{matchId}";
    }
  }
}
