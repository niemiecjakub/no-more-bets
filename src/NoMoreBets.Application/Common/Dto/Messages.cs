namespace NoMoreBets.Application.Common.Dto;
public abstract class BaseMessage
{
  public string Text { get; }
  public BaseMessage(string text)
  {
    Text = text;
  }
}
public class Message : BaseMessage
{
  public Message(string text) : base(text) { }
}
public class ReasoningMessage : BaseMessage
{
  public ReasoningMessage(string text) : base(text) { }
}

