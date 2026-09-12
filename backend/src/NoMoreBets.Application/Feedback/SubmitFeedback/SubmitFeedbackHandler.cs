using MediatR;
using NoMoreBets.Domain.Feedback;
using NoMoreBets.Application.Common;
using DomainFeedback = NoMoreBets.Domain.Feedback.Feedback;

namespace NoMoreBets.Application.Feedback.SubmitFeedback;

public sealed class SubmitFeedbackHandler(IFeedbackRepository feedback, IUnitOfWork unitOfWork)
  : IRequestHandler<SubmitFeedbackCommand, int>
{
  public async Task<int> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
  {
    var entry = DomainFeedback.Create(request.Message, request.Name, request.Email);
    await feedback.AddAsync(entry, cancellationToken).ConfigureAwait(false);
    await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    return entry.Id;
  }
}
