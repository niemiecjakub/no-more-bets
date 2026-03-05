namespace NoMoreBets.Application.Fotmob;

/// <summary>Provides FotMob league and team constants (e.g. Premier League, team ID to name). Implemented by Infrastructure and registered as singleton.</summary>
public interface IFotmobConstants
{
  FotmobLeague PremierLeague { get; }
  FotmobTeam? GetTeamById(int id);
  FotmobTeam? GetTeamByName(string name);
}

public record FotmobTeam(int Id, string Name);
public record FotmobLeague(int Id, string Name, string Slug);
