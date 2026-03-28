using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Betting;
using NoMoreBets.Domain.Clubs;
using NoMoreBets.Domain.Enums;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<League> League { get; set; }
  public DbSet<Club> Club { get; set; }
  public DbSet<Season> Season { get; set; }
  public DbSet<Stage> Stage { get; set; }
  public DbSet<MatchStatusEntity> MatchStatus { get; set; }
  public DbSet<Match> Match { get; set; }
  public DbSet<Lineup> Lineup { get; set; }
  public DbSet<MatchPreview> MatchPreview { get; set; }
  public DbSet<Head2Head> Head2Head { get; set; }
  public DbSet<LeagueTableSnapshot> LeagueTableSnapshot { get; set; }
  public DbSet<LeagueTableSnapshotRow> LeagueTableSnapshotRow { get; set; }
  public DbSet<BettingEventTypeEntity> BettingEventType { get; set; }
  public DbSet<BettingEventOptionEntity> BettingEventOption { get; set; }
  public DbSet<BettingOddsSnapshot> BettingOddsSnapshot { get; set; }
  public DbSet<BettingOddsSnapshotRow> BettingOddsSnapshotRow { get; set; }
  public DbSet<MatchDetails> MatchDetails { get; set; }
  public DbSet<MatchAnalysis> MatchAnalysis { get; set; }
  public DbSet<ClubDailySummary> ClubDailySummary { get; set; }
  public DbSet<BetStatusEntity> BetStatus { get; set; }
  public DbSet<BetSlip> BetSlip { get; set; }
  public DbSet<BetSelection> BetSelection { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<League>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
      entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
      entity.Property(e => e.SoccerdataId).IsRequired();
      entity.HasIndex(e => e.SoccerdataId).IsUnique();
      entity.HasIndex(e => e.Slug).IsUnique();
    });

    modelBuilder.Entity<Club>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
      entity.Property(e => e.Slug).IsRequired().HasMaxLength(200);
      entity.Property(e => e.LeagueId).IsRequired();
      entity.Property(e => e.SoccerdataId).IsRequired();
      entity.HasIndex(e => e.SoccerdataId).IsUnique();
      entity.HasIndex(e => e.Slug).IsUnique();
      entity.HasOne(e => e.League).WithMany(e => e.Clubs).HasForeignKey(e => e.LeagueId);
    });

    modelBuilder.Entity<Season>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.LeagueId).IsRequired();
      entity.Property(e => e.Year).IsRequired().HasMaxLength(20);
      entity.HasIndex(e => new { e.LeagueId, e.Year }).IsUnique();
      entity.HasOne(e => e.League).WithMany(e => e.Seasons).HasForeignKey(e => e.LeagueId);
    });

    modelBuilder.Entity<Stage>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.SeasonId).IsRequired();
      entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
      entity.HasIndex(e => new { e.SeasonId, e.Name }).IsUnique();
      entity.HasOne(e => e.Season).WithMany(e => e.Stages).HasForeignKey(e => e.SeasonId);
    });

    modelBuilder.Entity<MatchStatusEntity>(entity =>
    {
      entity.ToTable(MatchStatusEntity.TABLE_NAME);
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
      entity.HasData(
        Enum.GetValues(typeof(MatchStatus))
        .Cast<MatchStatus>()
        .Select(e => new MatchStatusEntity()
        {
          Id = (int)e,
          Name = e.ToString()
        }));
    });

    modelBuilder.Entity<Match>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.MatchDate).IsRequired();
      entity.Property(e => e.HomeClubId).IsRequired();
      entity.Property(e => e.AwayClubId).IsRequired();
      entity.Property(e => e.MatchStatusId).IsRequired();
      entity.HasOne(e => e.Stage).WithMany(s => s.Matches).HasForeignKey(e => e.StageId);
      entity.HasOne(e => e.HomeClub).WithMany(c => c.HomeMatches).HasForeignKey(e => e.HomeClubId);
      entity.HasOne(e => e.AwayClub).WithMany(c => c.AwayMatches).HasForeignKey(e => e.AwayClubId);
      entity.HasOne(e => e.MatchStatusEntity)
        .WithMany()
        .HasForeignKey(e => e.MatchStatusId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Lineup>(entity =>
    {
      entity.HasKey(e => e.MatchId);
      entity.Property(e => e.HomeTeamJson).IsRequired().HasColumnType("jsonb");
      entity.Property(e => e.AwayTeamJson).IsRequired().HasColumnType("jsonb");
      entity.Property(e => e.UpdatedAt).IsRequired();
      entity.HasOne(e => e.Match).WithOne(m => m.Lineup).HasForeignKey<Lineup>(e => e.MatchId);
    });

    modelBuilder.Entity<MatchPreview>(entity =>
    {
      entity.HasKey(e => e.MatchId);
      entity.Property(e => e.PreviewContentJson).IsRequired().HasColumnType("jsonb");
      entity.HasOne(e => e.Match).WithOne(m => m.MatchPreview).HasForeignKey<MatchPreview>(e => e.MatchId);
    });

    modelBuilder.Entity<Head2Head>(entity =>
    {
      entity.HasKey(e => new { e.Team1Id, e.Team2Id });
      entity.Property(e => e.Team1Id).IsRequired();
      entity.Property(e => e.Team2Id).IsRequired();
      entity.Property(e => e.Head2HeadJson).IsRequired().HasColumnType("jsonb");
      entity.Property(e => e.UpdatedAt).IsRequired();
      entity.HasOne(e => e.Team1).WithMany().HasForeignKey(e => e.Team1Id);
      entity.HasOne(e => e.Team2).WithMany().HasForeignKey(e => e.Team2Id);
    });

    modelBuilder.Entity<LeagueTableSnapshot>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityByDefaultColumn();
      entity.Property(e => e.LeagueId).IsRequired();
      entity.Property(e => e.SeasonId).IsRequired();
      entity.Property(e => e.SnapshotDate).IsRequired();
      entity.HasIndex(e => new { e.SeasonId, e.SnapshotDate }).IsUnique();
      entity.HasIndex(e => new { e.LeagueId, e.SnapshotDate });
      entity.HasOne(e => e.League).WithMany(l => l.LeagueTableSnapshots).HasForeignKey(e => e.LeagueId).OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(e => e.Season).WithMany(s => s.LeagueTableSnapshots).HasForeignKey(e => e.SeasonId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<LeagueTableSnapshotRow>(entity =>
    {
      entity.HasKey(e => new { e.SnapshotId, e.ClubId });
      entity.Property(e => e.SnapshotId).IsRequired();
      entity.Property(e => e.ClubId).IsRequired();
      entity.Property(e => e.Position).IsRequired();
      entity.Property(e => e.MatchesPlayed).IsRequired();
      entity.Property(e => e.Wins).IsRequired();
      entity.Property(e => e.Draws).IsRequired();
      entity.Property(e => e.Losses).IsRequired();
      entity.Property(e => e.GoalsFor).IsRequired();
      entity.Property(e => e.GoalsAgainst).IsRequired();
      entity.Property(e => e.GoalDifference).IsRequired();
      entity.Property(e => e.Points).IsRequired();
      entity.Property(e => e.Xg).IsRequired().HasPrecision(6, 2);
      entity.Property(e => e.XgDiff).IsRequired().HasPrecision(6, 2);
      entity.Property(e => e.Xga).IsRequired().HasPrecision(6, 2);
      entity.Property(e => e.XgaDiff).IsRequired().HasPrecision(6, 2);
      entity.Property(e => e.Xpts).IsRequired().HasPrecision(6, 2);
      entity.Property(e => e.XptsDiff).IsRequired().HasPrecision(6, 2);
      entity.HasIndex(e => new { e.SnapshotId, e.Position });
      entity.HasIndex(e => e.ClubId);
      entity.HasOne(e => e.Snapshot).WithMany(s => s.Rows).HasForeignKey(e => e.SnapshotId).OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(e => e.Club).WithMany(c => c.LeagueTableSnapshotRows).HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<BettingEventTypeEntity>(entity =>
    {
      entity.ToTable(BettingEventTypeEntity.TABLE_NAME);
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
      entity.HasData(
        Enum.GetValues(typeof(BettingEventType))
          .Cast<BettingEventType>()
          .Select(e => new BettingEventTypeEntity()
          {
            Id = (int)e,
            Name = e.ToString()
          }));
    });

    modelBuilder.Entity<BettingEventOptionEntity>(entity =>
    {
      entity.ToTable(BettingEventOptionEntity.TABLE_NAME);
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Name).IsRequired().HasMaxLength(80);
      entity.HasData(
        Enum.GetValues(typeof(BettingEventOption))
          .Cast<BettingEventOption>()
          .Select(e => new BettingEventOptionEntity()
          {
            Id = (int)e,
            Name = e.ToString()
          }));
    });

    modelBuilder.Entity<BettingOddsSnapshot>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityByDefaultColumn();
      entity.Property(e => e.MatchId).IsRequired();
      entity.Property(e => e.SnapshotTime).IsRequired();
      entity.HasIndex(e => new { e.MatchId, e.SnapshotTime });
      entity.HasOne(e => e.Match).WithMany(m => m.BettingOddsSnapshots).HasForeignKey(e => e.MatchId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<BettingOddsSnapshotRow>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityByDefaultColumn();
      entity.Property(e => e.SnapshotId).IsRequired();
      entity.Property(e => e.EventTypeId).IsRequired();
      entity.Property(e => e.EventOptionId).IsRequired(false);
      entity.Property(e => e.Odds).IsRequired(false).HasPrecision(18, 4);
      entity.HasIndex(e => new { e.SnapshotId, e.EventTypeId });
      entity.HasOne(e => e.Snapshot).WithMany(s => s.Rows).HasForeignKey(e => e.SnapshotId).OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(e => e.EventTypeEntity)
        .WithMany()
        .HasForeignKey(e => e.EventTypeId)
        .OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(e => e.EventOptionEntity)
        .WithMany()
        .HasForeignKey(e => e.EventOptionId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<MatchDetails>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.FotmobUrl).IsRequired(false);
      entity.HasIndex(e => e.FotmobUrl).IsUnique().HasFilter("\"FotmobUrl\" IS NOT NULL");
      entity.Property(e => e.FotmobDetailsJson).IsRequired(false).HasColumnType("jsonb");
      entity.HasIndex(e => e.MatchId).IsUnique().HasFilter("\"MatchId\" IS NOT NULL");
      entity.HasOne(e => e.Match).WithOne(m => m.MatchDetails).HasForeignKey<MatchDetails>(e => e.MatchId).OnDelete(DeleteBehavior.SetNull);
    });

    modelBuilder.Entity<MatchAnalysis>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.MatchId).IsRequired();
      entity.Property(e => e.Code).IsRequired().HasMaxLength(255);
      entity.Property(e => e.Content).IsRequired().HasColumnType("jsonb");
      entity.HasIndex(e => e.MatchId);
      entity.HasOne(e => e.Match).WithMany(m => m.MatchAnalyses).HasForeignKey(e => e.MatchId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<ClubDailySummary>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.ClubId).IsRequired();
      entity.Property(e => e.Date).IsRequired();
      entity.Property(e => e.Summary).IsRequired();
      entity.HasIndex(e => e.ClubId);
      entity.HasIndex(e => new { e.ClubId, e.Date });
      entity.HasOne(e => e.Club).WithMany(c => c.ClubDailySummaries).HasForeignKey(e => e.ClubId).OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<BetStatusEntity>(entity =>
    {
      entity.ToTable(BetStatusEntity.TABLE_NAME);
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
      entity.HasData(
        Enum.GetValues(typeof(BetStatus))
          .Cast<BetStatus>()
          .Select(e => new BetStatusEntity()
          {
            Id = (int)e,
            Name = e.ToString()
          }));
    });

    modelBuilder.Entity<BetSlip>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.StakeAmount).IsRequired().HasPrecision(18, 4);
      entity.Property(e => e.TotalOdds).IsRequired().HasPrecision(18, 4);
      entity.Property(e => e.PotentialPayout).IsRequired().HasPrecision(18, 4);
      entity.Property(e => e.StatusId).IsRequired();
      entity.Property(e => e.CreatedAt).IsRequired();
      entity.HasIndex(e => e.StatusId);
      entity.HasOne(e => e.BetStatusEntity)
        .WithMany()
        .HasForeignKey(e => e.StatusId)
        .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<BetSelection>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.BetSlipId).IsRequired();
      entity.Property(e => e.MatchId).IsRequired();
      entity.Property(e => e.EventTypeId).IsRequired();
      entity.Property(e => e.OutcomeKey).IsRequired().HasMaxLength(255);
      entity.Property(e => e.OddsAtPlacement).IsRequired().HasPrecision(18, 4);
      entity.Property(e => e.StatusId).IsRequired();
      entity.HasIndex(e => e.BetSlipId);
      entity.HasIndex(e => e.MatchId);
      entity.HasIndex(e => e.StatusId);
      entity.HasOne(e => e.BetSlip).WithMany(s => s.Selections).HasForeignKey(e => e.BetSlipId).OnDelete(DeleteBehavior.Cascade);
      entity.HasOne(e => e.Match).WithMany(m => m.BetSelections).HasForeignKey(e => e.MatchId).OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(e => e.EventTypeEntity)
        .WithMany()
        .HasForeignKey(e => e.EventTypeId)
        .OnDelete(DeleteBehavior.Restrict);
      entity.HasOne(e => e.BetStatusEntity)
        .WithMany()
        .HasForeignKey(e => e.StatusId)
        .OnDelete(DeleteBehavior.Restrict);
    });
  }
}
