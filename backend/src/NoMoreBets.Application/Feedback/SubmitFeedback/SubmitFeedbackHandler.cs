using MediatR;
using NoMoreBets.Application.Common;
using DomainFeedback = NoMoreBets.Domain.Feedback.Feedback;

namespace NoMoreBets.Application.Feedback.SubmitFeedback;

public sealed class SubmitFeedbackHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<SubmitFeedbackCommand, int>
{
  public async Task<int> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
  {
    var feedback = DomainFeedback.Create(request.Message, request.Name, request.Email);
    await unitOfWork.Feedback.AddAsync(feedback, cancellationToken).ConfigureAwait(false);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return feedback.Id;
  }
}
