using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Matches;
using NoMoreBets.Infrastructure.Persistence.Repositories;

namespace NoMoreBets.Infrastructure.Tests.Persistence.Repositories;

public class LeagueRepositoryTests
{
  [Fact]
  public void SelectLatestSeasonIds_WithMultipleSeasonsPerLeague_ReturnsLatestByStartDateThenId()
  {
    // Arrange
    IReadOnlyList<(int Id, int LeagueId, DateOnly? StartDate)> seasons =
    [
      (1, 1, new DateOnly(2025, 8, 15)),
      (9, 1, new DateOnly(2026, 8, 21)),
      (2, 2, new DateOnly(2025, 7, 18)),
      (10, 2, new DateOnly(2026, 7, 24))
    ];

    // Act
    var latestSeasonIds = LeagueRepository.SelectLatestSeasonIds(seasons);

    // Assert
    latestSeasonIds.Should().BeEquivalentTo([9, 10]);
  }

  [Fact]
  public void SelectLatestSeasonIds_WhenStartDatesEqual_PrefersHigherId()
  {
    // Arrange
    IReadOnlyList<(int Id, int LeagueId, DateOnly? StartDate)> seasons =
    [
      (1, 1, new DateOnly(2025, 8, 15)),
      (2, 1, new DateOnly(2025, 8, 15))
    ];

    // Act
    var latestSeasonIds = LeagueRepository.SelectLatestSeasonIds(seasons);

    // Assert
    latestSeasonIds.Should().Equal(2);
  }

  [Fact]
  public async Task ClubQuery_WithLatestSeasonFilter_ExcludesOlderSeasonOnlyClubs()
  {
    // Arrange
    await using var db = CreateFilterDb();
    db.Clubs.AddRange(
      new Club { Id = 1, Name = "Arsenal", Slug = "arsenal", SoccerdataId = 3068 },
      new Club { Id = 2, Name = "Burnley", Slug = "burnley", SoccerdataId = 3104 });
    db.ClubSeasons.AddRange(
      new ClubSeason { ClubId = 1, SeasonId = 9 },
      new ClubSeason { ClubId = 2, SeasonId = 1 });
    var kickoff = DateTime.UtcNow.AddDays(3);
    db.Matches.AddRange(
      new Match
      {
        Id = 1,
        StageId = 1,
        HomeClubId = 1,
        AwayClubId = 99,
        MatchDate = kickoff,
        MatchStatusId = (int)MatchStatus.Upcomming
      },
      new Match
      {
        Id = 2,
        StageId = 1,
        HomeClubId = 2,
        AwayClubId = 98,
        MatchDate = kickoff,
        MatchStatusId = (int)MatchStatus.Upcomming
      });
    await db.SaveChangesAsync();

    var latestSeasonIds = LeagueRepository.SelectLatestSeasonIds(
    [
      (1, 1, new DateOnly(2025, 8, 15)),
      (9, 1, new DateOnly(2026, 8, 21))
    ]);
    var utcNow = DateTime.UtcNow;
    var kickoffWithinTenDaysEnd = utcNow.AddDays(10);

    // Act
    var clubs = await db.Clubs
      .Where(c => c.ClubSeasons.Any(cs => latestSeasonIds.Contains(cs.SeasonId)))
      .Where(c => db.Matches.Any(m =>
        m.MatchStatusId == (int)MatchStatus.Upcomming
        && m.MatchDate > utcNow
        && m.MatchDate <= kickoffWithinTenDaysEnd
        && (m.HomeClubId == c.Id || m.AwayClubId == c.Id)))
      .Select(c => c.Id)
      .ToListAsync();

    // Assert
    clubs.Should().Equal(1);
  }

  private static FilterTestDbContext CreateFilterDb()
  {
    var options = new DbContextOptionsBuilder<FilterTestDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .Options;
    return new FilterTestDbContext(options);
  }

  private sealed class FilterTestDbContext(DbContextOptions<FilterTestDbContext> options) : DbContext(options)
  {
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<ClubSeason> ClubSeasons => Set<ClubSeason>();
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Club>(e =>
      {
        e.HasKey(c => c.Id);
        e.Ignore(c => c.HomeMatches);
        e.Ignore(c => c.AwayMatches);
        e.Ignore(c => c.LeagueTableSnapshotRows);
        e.Ignore(c => c.ClubDailySummaries);
      });
      modelBuilder.Entity<ClubSeason>(e =>
      {
        e.HasKey(cs => new { cs.ClubId, cs.SeasonId });
        e.HasOne(cs => cs.Club).WithMany(c => c.ClubSeasons).HasForeignKey(cs => cs.ClubId);
        e.Ignore(cs => cs.Season);
      });
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
        e.Ignore(m => m.MatchEvents);
        e.Ignore(m => m.MatchAnalyses);
        e.Ignore(m => m.BettingOddsSnapshots);
        e.Ignore(m => m.BetSelections);
      });
    }
  }
}
