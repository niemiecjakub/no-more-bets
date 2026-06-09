using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NoMoreBets.Infrastructure.AI.Providers.Todo;
internal sealed class TodoState
{
  /// <summary>
  /// Gets the list of todo items.
  /// </summary>
  [JsonPropertyName("items")]
  public List<TodoItem> Items { get; set; } = [];

  /// <summary>
  /// Gets or sets the next ID to assign to a new todo item.
  /// </summary>
  [JsonPropertyName("nextId")]
  public int NextId { get; set; } = 1;
}