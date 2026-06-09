namespace NoMoreBets.Domain.Feedback;

public interface IFeedbackRepository
{
  Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default);
}
