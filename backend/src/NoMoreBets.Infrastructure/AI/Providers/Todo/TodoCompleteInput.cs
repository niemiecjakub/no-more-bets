using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NoMoreBets.Infrastructure.AI.Providers.Todo;
internal sealed class TodoCompleteInput
{
  /// <summary>
  /// Gets or sets the ID of the todo item to mark as complete.
  /// </summary>
  [JsonPropertyName("id")]
  public int Id { get; set; }

  /// <summary>
  /// Gets or sets the reason describing how or why the item was completed.
  /// </summary>
  [JsonPropertyName("reason")]
  public string Reason { get; set; } = string.Empty;
}
