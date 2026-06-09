using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NoMoreBets.Infrastructure.AI.Providers.Todo;
internal sealed class TodoItemInput
{
  /// <summary>
  /// Gets or sets the title of the todo item to create.
  /// </summary>
  [JsonPropertyName("title")]
  public string Title { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets an optional description providing additional details about the todo item.
  /// </summary>
  [JsonPropertyName("description")]
  public string? Description { get; set; }
}
