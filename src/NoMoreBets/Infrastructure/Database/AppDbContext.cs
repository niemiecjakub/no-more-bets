using Microsoft.EntityFrameworkCore;
using NoMoreBets.Domain.Entity;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Infrastructure.Database;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<League> League { get; set; }
  public DbSet<Club> Club { get; set; }
  public DbSet<Season> Season { get; set; }
  public DbSet<Stage> Stage { get; set; }
  public DbSet<Match> Game { get; set; }
  public DbSet<MatchStatusEntity> MatchStatus { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<League>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
      entity.Property(e => e.SoccerdataId).IsRequired();
      entity.HasIndex(e => e.SoccerdataId).IsUnique();
    });

    modelBuilder.Entity<Club>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Id).UseIdentityAlwaysColumn();
      entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
      entity.Property(e => e.LeagueId).IsRequired();
      entity.Property(e => e.SoccerdataId).IsRequired();
      entity.HasIndex(e => e.SoccerdataId).IsUnique();
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
      entity.Property(e => e.StageId).IsRequired();
      entity.Property(e => e.MatchDate).IsRequired();
      entity.Property(e => e.HomeClubId).IsRequired();
      entity.Property(e => e.AwayClubId).IsRequired();
      entity.Property(e => e.SoccerdataId).IsRequired();
      entity.Property(e => e.MatchStatusId).IsRequired();
      entity.HasOne(e => e.Stage).WithMany(e => e.Matches).HasForeignKey(e => e.StageId);
      entity.HasOne(e => e.HomeClub).WithMany(e => e.HomeMatches).HasForeignKey(e => e.HomeClubId);
      entity.HasOne(e => e.AwayClub).WithMany(e => e.AwayMatches).HasForeignKey(e => e.AwayClubId);
      entity.HasOne(m => m.MatchStatusEntity)
        .WithMany()
        .HasForeignKey(m => m.MatchStatusId)
        .OnDelete(DeleteBehavior.Restrict);
    });
  }
}
