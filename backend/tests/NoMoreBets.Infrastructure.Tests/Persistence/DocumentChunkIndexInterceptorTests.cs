using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.Tests.Persistence;

public class DocumentChunkIndexInterceptorTests
{
  [Fact]
  public async Task SavedChangesAsync_MatchAdded_EnqueuesMatchIndex()
  {
    // Arrange
    var scheduler = Substitute.For<IDocumentChunkIndexScheduler>();
    var interceptor = new DocumentChunkIndexInterceptor(scheduler);
    await using var db = CreateDb(interceptor);

    // Act
    db.Matches.Add(new Match
    {
      Id = 1,
      MatchDate = DateTime.UtcNow,
      HomeClubId = 1,
      AwayClubId = 2,
      MatchStatusId = 1
    });
    await db.SaveChangesAsync();

    // Assert
    scheduler.Received(1).Enqueue(DocumentChunkSourceType.Match, 1);
  }

  [Fact]
  public async Task SavedChangesAsync_MatchAnalysisAndLineup_EnqueuesBothSourcesOnce()
  {
    // Arrange
    var scheduler = Substitute.For<IDocumentChunkIndexScheduler>();
    var interceptor = new DocumentChunkIndexInterceptor(scheduler);
    await using var db = CreateDb(interceptor);

    db.Matches.Add(new Match
    {
      Id = 10,
      MatchDate = DateTime.UtcNow,
      HomeClubId = 1,
      AwayClubId = 2,
      MatchStatusId = 1
    });
    await db.SaveChangesAsync();
    scheduler.ClearReceivedCalls();

    // Act
    db.Analyses.Add(new MatchAnalysis
    {
      Id = 5,
      MatchId = 10,
      Code = MatchAnalysis.ResearchCode,
      Content = """{"text":"hello"}"""
    });
    db.Lineups.Add(new Lineup
    {
      MatchId = 10,
      HomeTeamJson = """{"players":[]}""",
      AwayTeamJson = """{"players":[]}""",
      UpdatedAt = DateTime.UtcNow
    });
    await db.SaveChangesAsync();

    // Assert
    scheduler.Received(1).Enqueue(DocumentChunkSourceType.Match, 10);
    scheduler.Received(1).Enqueue(DocumentChunkSourceType.MatchAnalysis, 5);
  }

  private static InterceptorTestDbContext CreateDb(DocumentChunkIndexInterceptor interceptor)
  {
    var options = new DbContextOptionsBuilder<InterceptorTestDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .AddInterceptors(interceptor)
      .Options;
    return new InterceptorTestDbContext(options);
  }

  private sealed class InterceptorTestDbContext(DbContextOptions<InterceptorTestDbContext> options) : DbContext(options)
  {
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchAnalysis> Analyses => Set<MatchAnalysis>();
    public DbSet<Lineup> Lineups => Set<Lineup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Match>(e =>
      {
        e.HasKey(m => m.Id);
        e.Ignore(m => m.Stage);
        e.Ignore(m => m.HomeClub);
        e.Ignore(m => m.AwayClub);
        e.Ignore(m => m.MatchStatusEntity);
        e.Ignore(m => m.Lineup);
        e.Ignore(m => m.MatchPreview);
        e.Ignore(m => m.MatchDetails);
        e.Ignore(m => m.MatchAnalyses);
        e.Ignore(m => m.BettingOddsSnapshots);
        e.Ignore(m => m.BetSelections);
        e.Ignore(m => m.MatchEvents);
      });
      modelBuilder.Entity<MatchAnalysis>(e =>
      {
        e.HasKey(a => a.Id);
        e.Ignore(a => a.Match);
        e.Ignore(a => a.AgentSession);
      });
      modelBuilder.Entity<Lineup>(e =>
      {
        e.HasKey(l => l.MatchId);
        e.Ignore(l => l.Match);
      });
    }
  }
}
