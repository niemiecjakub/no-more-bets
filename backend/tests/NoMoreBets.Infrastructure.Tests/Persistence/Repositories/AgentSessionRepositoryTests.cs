using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.AgentSessions;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence.Repositories;

namespace NoMoreBets.Infrastructure.Tests.Persistence.Repositories;

public class AgentSessionRepositoryTests
{
  private const string CurrentSeasonYear = "2025/2026";
  private const string PreviousSeasonYear = "2024/2025";

  [Fact]
  public async Task ApplySeasonYearFilter_WithSeasonYear_IncludesResearchBettingReflectionInternetAndCleanup()
  {
    // Arrange
    await using var db = CreateFilterDb();
    var startedAt = DateTime.UtcNow;

    var currentSeason = new Season { Id = 1, LeagueId = 1, Year = CurrentSeasonYear };
    var previousSeason = new Season { Id = 2, LeagueId = 1, Year = PreviousSeasonYear };
    var currentStage = new Stage { Id = 1, SeasonId = 1, Name = "Regular" };
    var previousStage = new Stage { Id = 2, SeasonId = 2, Name = "Regular" };
    var currentMatch = new Match
    {
      Id = 1,
      StageId = 1,
      HomeClubId = 1,
      AwayClubId = 2,
      MatchDate = startedAt,
      MatchStatusId = 1,
    };
    var previousMatch = new Match
    {
      Id = 2,
      StageId = 2,
      HomeClubId = 1,
      AwayClubId = 2,
      MatchDate = startedAt,
      MatchStatusId = 1,
    };

    var researchSession = new AgentSession { Id = 1, Phase = AgentSessionPhase.Research, StartedAt = startedAt };
    var bettingSession = new AgentSession { Id = 2, Phase = AgentSessionPhase.Betting, StartedAt = startedAt.AddMinutes(-1) };
    var reflectionSession = new AgentSession { Id = 3, Phase = AgentSessionPhase.Reflection, StartedAt = startedAt.AddMinutes(-2) };
    var internetSession = new AgentSession { Id = 4, Phase = AgentSessionPhase.InternetResearch, StartedAt = startedAt.AddMinutes(-3) };
    var cleanupSession = new AgentSession { Id = 5, Phase = AgentSessionPhase.MemoryCleanup, StartedAt = startedAt.AddMinutes(-4) };
    var previousResearchSession = new AgentSession { Id = 6, Phase = AgentSessionPhase.Research, StartedAt = startedAt.AddMinutes(-5) };

    var bettingSlip = new BetSlip
    {
      Id = 1,
      AgentSessionId = 2,
      StakeAmount = 10m,
      TotalOdds = 2m,
      PotentialPayout = 20m,
      StatusId = 1,
      CreatedAt = startedAt,
    };
    var reflectedSlip = new BetSlip
    {
      Id = 2,
      AgentSessionReflectedId = 3,
      StakeAmount = 10m,
      TotalOdds = 2m,
      PotentialPayout = 20m,
      StatusId = 1,
      CreatedAt = startedAt,
    };

    db.Seasons.AddRange(currentSeason, previousSeason);
    db.Stages.AddRange(currentStage, previousStage);
    db.Matches.AddRange(currentMatch, previousMatch);
    db.AgentSessions.AddRange(
      researchSession,
      bettingSession,
      reflectionSession,
      internetSession,
      cleanupSession,
      previousResearchSession);
    db.MatchAnalyses.AddRange(
      new MatchAnalysis
      {
        Id = 1,
        MatchId = 1,
        AgentSessionId = 1,
        Code = MatchAnalysis.ResearchCode,
        Content = "{}",
      },
      new MatchAnalysis
      {
        Id = 2,
        MatchId = 2,
        AgentSessionId = 6,
        Code = MatchAnalysis.ResearchCode,
        Content = "{}",
      });
    db.BetSlips.AddRange(bettingSlip, reflectedSlip);
    db.BetSelections.AddRange(
      CreateSelection(1, 1, 1),
      CreateSelection(2, 2, 1));
    await db.SaveChangesAsync();

    // Act
    var filteredIds = await AgentSessionRepository.ApplySeasonYearFilter(
        db.AgentSessions.AsNoTracking(),
        [CurrentSeasonYear])
      .Select(s => s.Id)
      .ToListAsync();

    // Assert
    filteredIds.Should().BeEquivalentTo([1, 2, 3, 4, 5]);
  }

  [Fact]
  public async Task ApplySeasonYearFilter_WithEmptySeasonYears_ReturnsAllSessions()
  {
    // Arrange
    await using var db = CreateFilterDb();
    db.AgentSessions.AddRange(
      new AgentSession { Id = 1, Phase = AgentSessionPhase.InternetResearch, StartedAt = DateTime.UtcNow },
      new AgentSession { Id = 2, Phase = AgentSessionPhase.Research, StartedAt = DateTime.UtcNow });
    await db.SaveChangesAsync();

    // Act
    var filteredIds = await AgentSessionRepository.ApplySeasonYearFilter(
        db.AgentSessions.AsNoTracking(),
        [])
      .Select(s => s.Id)
      .ToListAsync();

    // Assert
    filteredIds.Should().BeEquivalentTo([1, 2]);
  }

  private static BetSelection CreateSelection(int id, int betSlipId, int matchId)
  {
    var selection = new BetSelection
    {
      Id = id,
      BetSlipId = betSlipId,
      MatchId = matchId,
      EventTypeId = 1,
      EventOptionId = 1,
      OddsAtPlacement = 2m,
    };
    selection.SetStatus(BetStatus.Pending);
    return selection;
  }

  private static SeasonFilterTestDbContext CreateFilterDb()
  {
    var options = new DbContextOptionsBuilder<SeasonFilterTestDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new SeasonFilterTestDbContext(options);
  }

  private sealed class SeasonFilterTestDbContext(DbContextOptions<SeasonFilterTestDbContext> options) : DbContext(options)
  {
    public DbSet<AgentSession> AgentSessions => Set<AgentSession>();
    public DbSet<MatchAnalysis> MatchAnalyses => Set<MatchAnalysis>();
    public DbSet<BetSlip> BetSlips => Set<BetSlip>();
    public DbSet<BetSelection> BetSelections => Set<BetSelection>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<Season> Seasons => Set<Season>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<AgentSession>(e =>
      {
        e.HasKey(s => s.Id);
        e.Property(s => s.Phase).HasConversion<int>();
        e.HasMany(s => s.MatchAnalyses).WithOne(a => a.AgentSession).HasForeignKey(a => a.AgentSessionId);
        e.HasMany(s => s.BetSlips).WithOne(slip => slip.AgentSession).HasForeignKey(slip => slip.AgentSessionId);
        e.HasMany(s => s.ReflectedBetSlips).WithOne(slip => slip.AgentSessionReflected).HasForeignKey(slip => slip.AgentSessionReflectedId);
        e.Ignore(s => s.Messages);
      });

      modelBuilder.Entity<Season>(e =>
      {
        e.HasKey(s => s.Id);
        e.Ignore(s => s.League);
        e.Ignore(s => s.ClubSeasons);
        e.Ignore(s => s.Stages);
        e.Ignore(s => s.LeagueTableSnapshots);
      });

      modelBuilder.Entity<Stage>(e =>
      {
        e.HasKey(s => s.Id);
        e.HasOne(s => s.Season).WithMany().HasForeignKey(s => s.SeasonId);
        e.Ignore(s => s.Matches);
      });

      modelBuilder.Entity<Match>(e =>
      {
        e.HasKey(m => m.Id);
        e.HasOne(m => m.Stage).WithMany().HasForeignKey(m => m.StageId);
        e.Ignore(m => m.HomeClub);
        e.Ignore(m => m.AwayClub);
        e.Ignore(m => m.MatchStatusEntity);
        e.Ignore(m => m.Lineup);
        e.Ignore(m => m.MatchPreview);
        e.Ignore(m => m.MatchDetails);
        e.Ignore(m => m.MatchEvents);
        e.Ignore(m => m.MatchAnalyses);
        e.Ignore(m => m.BettingOddsSnapshots);
        e.Ignore(m => m.BetSelections);
      });

      modelBuilder.Entity<MatchAnalysis>(e =>
      {
        e.HasKey(a => a.Id);
        e.HasOne(a => a.Match).WithMany().HasForeignKey(a => a.MatchId);
      });

      modelBuilder.Entity<BetSlip>(e =>
      {
        e.HasKey(s => s.Id);
        e.HasMany(s => s.Selections).WithOne(sel => sel.BetSlip).HasForeignKey(sel => sel.BetSlipId);
        e.Ignore(s => s.BetStatusEntity);
        e.Ignore(s => s.Bankrolls);
      });

      modelBuilder.Entity<BetSelection>(e =>
      {
        e.HasKey(s => s.Id);
        e.HasOne(s => s.Match).WithMany().HasForeignKey(s => s.MatchId);
        e.Ignore(s => s.EventTypeEntity);
        e.Ignore(s => s.EventOptionEntity);
        e.Ignore(s => s.BetStatusEntity);
      });
    }
  }
}
