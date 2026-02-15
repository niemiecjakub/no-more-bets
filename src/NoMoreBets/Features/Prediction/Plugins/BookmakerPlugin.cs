using System.ComponentModel;
using System.Text.Json;
using MediatR;
using Microsoft.SemanticKernel;
using NoMoreBets.Features.Betclic.GetBetclicMatchEvents;
using NoMoreBets.Features.Betclic.Model;

namespace NoMoreBets.Features.Prediction.Plugins;

/// <summary>
/// Plugin exposing bookmaker markets/events for a match.
/// </summary>
public sealed class BookmakerPlugin(IMediator mediator)
{
  [KernelFunction("get_match_bookmaker_events")]
  [Description("Fetches bookmaker events/markets for a specific match URL.")]
  public async Task<IEnumerable<BookmakerEvent>> GetMatchBookmakerEventsAsync(
      [Description("Betclic match URL.")] string gameUrl,
      [Description("Whether to expand hidden sections before parsing.")] bool expand = true,
      CancellationToken cancellationToken = default)
  {
    return await mediator.Send(new GetBetclicMatchEventsQuery(gameUrl, expand), cancellationToken);
  }
}
