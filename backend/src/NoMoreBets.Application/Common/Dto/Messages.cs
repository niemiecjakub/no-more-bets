namespace NoMoreBets.Application.Common.Dto;
public interface IMessage { }
public record Message(string Text) : IMessage;
public record ReasoningMessage(string Text) : IMessage;
public record FunctionMessage(string Name, List<FunctionArgument>? Arguments, string? Metadata = null) : IMessage;
public record FunctionArgument(string Name, string? Value);

