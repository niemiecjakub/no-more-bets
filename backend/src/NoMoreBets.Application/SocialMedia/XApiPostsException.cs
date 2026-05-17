namespace NoMoreBets.Application.SocialMedia;

public sealed class XApiPostsException : Exception
{
  public int StatusCode { get; }

  public XApiPostsException(int statusCode, string message, Exception? innerException = null)
    : base(message, innerException)
  {
    StatusCode = statusCode;
  }
}
