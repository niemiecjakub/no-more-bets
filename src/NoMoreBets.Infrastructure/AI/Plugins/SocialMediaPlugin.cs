using System.ComponentModel;
using Microsoft.SemanticKernel;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Application.SocialMedia.CreateXPost;

namespace NoMoreBets.Infrastructure.AI.Plugins;

[Description("Publishes content to social networks when configured (X/Twitter via OAuth 1.0a).")]
public sealed class SocialMediaPlugin
{
  private readonly IXApiService _xApiService;

  public SocialMediaPlugin(IXApiService xApiService)
  {
    _xApiService = xApiService;
  }

  [KernelFunction("CreateXPost")]
  [Description("Creates a public post on X (Twitter). Text must be non-empty and at most 280 characters.")]
  public async Task<CreateXPostResult> CreateXPostAsync(
    [Description("The full post body to publish on X.")]
    string text,
    CancellationToken cancellationToken = default)
  {
    return await _xApiService
      .CreateXPostAsync(new CreateXPostRequest { Text = text }, cancellationToken)
      .ConfigureAwait(false);
  }
}
