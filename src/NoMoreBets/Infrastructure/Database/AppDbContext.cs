using Microsoft.EntityFrameworkCore;

namespace NoMoreBets.Infrastructure.Database;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

}
