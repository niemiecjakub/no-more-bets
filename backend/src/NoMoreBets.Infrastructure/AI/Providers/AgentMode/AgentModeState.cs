using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NoMoreBets.Infrastructure.AI.Providers.AgentMode;
internal sealed class AgentModeState
{
  /// <summary>
  /// Gets or sets the current operating mode of the agent.
  /// </summary>
  [JsonPropertyName("currentMode")]
  public string CurrentMode { get; set; } = "plan";
}