using DbUp;
using System.Reflection;

namespace NoMoreBets.Infrastructure.Database;

public static class DbInitializer
{
  public static void Initialize(string connectionString)
  {
    EnsureDatabase.For.PostgresqlDatabase(connectionString);

    var upgrader = DeployChanges.To
      .PostgresqlDatabase(connectionString)
      .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
      .WithTransaction()
      .LogToConsole()
      .Build();

    upgrader.PerformUpgrade();
  }
}

