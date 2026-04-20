namespace NoMoreBets.Infrastructure.Scraping.External.Fotmob;

/// <summary>Provides FotMob league and team constants </summary>
public interface IFotmobConstants
{
  FotmobLeague PremierLeague { get; }
  FotmobLeague Ekstraklasa { get; }
  FotmobLeague Bundesliga { get; }
  FotmobLeague LaLiga { get; }
  FotmobLeague SerieA { get; }
  FotmobLeague Ligue1 { get; }
  FotmobLeague? GetLeagueBySlug(string slug);
  FotmobTeam? GetTeamById(int id);
  FotmobTeam? GetTeamByName(string name);
}

public record FotmobTeam(int Id, string Name);
public record FotmobLeague(int Id, string Name, string Slug);
