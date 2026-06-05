using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Application.SocialMedia.CreateXPost;

namespace NoMoreBets.Infrastructure.AI.Plugins;

public sealed class SocialMediaPlugin
{
  private readonly IXApiService _xApiService;
  private readonly ILogger<SocialMediaPlugin> _logger;

  public SocialMediaPlugin(IXApiService xApiService, ILogger<SocialMediaPlugin>? logger = null)
  {
    _xApiService = xApiService;
    _logger = logger ?? NullLogger<SocialMediaPlugin>.Instance;
  }

  [AgentTool("CreateXPost")]
  [Description("Creates a public post on X (Twitter). Text must be non-empty and at most 280 characters.")]
  public async Task<CreateXPostResult> CreateXPostAsync(
    [Description("The full post body to publish on X.")]
    string text,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      _logger.LogError("Attempted to create X post with empty text.");
    }

    return await _xApiService
      .CreateXPostAsync(new CreateXPostRequest { Text = text }, cancellationToken)
      .ConfigureAwait(false);
  }
}
