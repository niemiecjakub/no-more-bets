using MediatR;

namespace NoMoreBets.Application.Feedback.SubmitFeedback;

public record SubmitFeedbackCommand(string Message, string? Name, string? Email) : IRequest<int>;
