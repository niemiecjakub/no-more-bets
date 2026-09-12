using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence;

namespace NoMoreBets.Infrastructure.Tests.Persistence;

public class DocumentChunkIndexInterceptorTests
{
  [Fact]
  public async Task SavedChangesAsync_MatchAdded_EnqueuesMatchIndex()
  {
    var jobClient = Substitute.For<IBackgroundJobClient>();
    jobClient.Create(Arg.Any<Job>(), Arg.Any<IState>()).Returns("job-1");
    var interceptor = new DocumentChunkIndexInterceptor(jobClient);
    await using var db = CreateDb(interceptor);

    db.Matches.Add(new Match
    {
      Id = 1,
      MatchDate = DateTime.UtcNow,
      HomeClubId = 1,
      AwayClubId = 2,
      MatchStatusId = 1
    });
    await db.SaveChangesAsync();

    jobClient.Received(1).Create(
      Arg.Is<Job>(j =>
        j.Type == typeof(NoMoreBets.Infrastructure.BackgroundJobs.DocumentChunkIndexJobService)
        && j.Method.Name == nameof(NoMoreBets.Infrastructure.BackgroundJobs.DocumentChunkIndexJobService.IndexAsync)
        && (string)j.Args[0]! == DocumentChunkSourceType.Match
        && (int)j.Args[1]! == 1),
      Arg.Any<IState>());
  }

  [Fact]
  public async Task SavedChangesAsync_MatchAnalysisAndLineup_EnqueuesBothSourcesOnce()
  {
    var jobClient = Substitute.For<IBackgroundJobClient>();
    jobClient.Create(Arg.Any<Job>(), Arg.Any<IState>()).Returns("job-1");
    var interceptor = new DocumentChunkIndexInterceptor(jobClient);
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
    jobClient.ClearReceivedCalls();

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

    jobClient.Received(1).Create(
      Arg.Is<Job>(j =>
        (string)j.Args[0]! == DocumentChunkSourceType.Match && (int)j.Args[1]! == 10),
      Arg.Any<IState>());
    jobClient.Received(1).Create(
      Arg.Is<Job>(j =>
        (string)j.Args[0]! == DocumentChunkSourceType.MatchAnalysis && (int)j.Args[1]! == 5),
      Arg.Any<IState>());
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
