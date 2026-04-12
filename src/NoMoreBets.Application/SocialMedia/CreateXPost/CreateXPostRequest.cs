namespace NoMoreBets.Application.SocialMedia.CreateXPost;

public sealed class CreateXPostRequest
{
  public const int MaxTweetTextLength = 280;

  public string Text { get; set; } = "";
}
