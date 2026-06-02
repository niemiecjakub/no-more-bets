using NoMoreBets.Domain.Feedback;

namespace NoMoreBets.Infrastructure.Persistence.Repositories;

public sealed class FeedbackRepository(AppDbContext db) : IFeedbackRepository
{
  public async Task AddAsync(Feedback feedback, CancellationToken cancellationToken = default)
  {
    await db.Feedback.AddAsync(feedback, cancellationToken).ConfigureAwait(false);
  }
}
