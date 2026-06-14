using System.Linq;
using System.Text.Json;
using NoMoreBets.Application.Common.Dto;
using NoMoreBets.Domain.AgentSessions;

namespace NoMoreBets.Infrastructure.AI.Common;

internal static class AgentSessionTranscriptMapper
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

  public static IReadOnlyList<AgentSessionMessage> ToEntities(IReadOnlyList<IMessage> messages)
  {
    var rows = new List<AgentSessionMessage>(messages.Count);
    for (var i = 0; i < messages.Count; i++)
    {
      rows.Add(Map(messages[i], i));
    }

    return rows;
  }

  private static AgentSessionMessage Map(IMessage message, int ordinal)
  {
    return message switch
    {
      Message m => new AgentSessionMessage
      {
        Ordinal = ordinal,
        Kind = AgentSessionMessageKind.Message,
        Text = m.Text
      },
      ReasoningMessage r => new AgentSessionMessage
      {
        Ordinal = ordinal,
        Kind = AgentSessionMessageKind.Reasoning,
        Text = r.Text
      },
      FunctionMessage f => new AgentSessionMessage
      {
        Ordinal = ordinal,
        Kind = AgentSessionMessageKind.FunctionCall,
        Text = SerializeFunctionCall(f),
        Metadata = f.Metadata
      },
      _ => throw new ArgumentOutOfRangeException(nameof(message), message.GetType().FullName, null)
    };
  }

  private static string SerializeFunctionCall(FunctionMessage f)
  {
    var arguments = f.Arguments?.Select(a => new FunctionArgumentDto(a.Name, a.Value)).ToList();
    return JsonSerializer.Serialize(new FunctionCallPayload(f.Name, arguments), JsonOptions);
  }

  private sealed record FunctionArgumentDto(string Name, string? Value);

  private sealed record FunctionCallPayload(string Name, List<FunctionArgumentDto>? Arguments);
}
