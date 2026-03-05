using NoMoreBets.Application.Fotmob;

namespace NoMoreBets.Infrastructure.Scraping.External.Fotmob;

/// <summary>
/// FotMob league and team constants (Premier League). Registered as singleton.
/// </summary>
public class FotmobConstants : IFotmobConstants
{
  public FotmobLeague PremierLeague { get; } = new(47, "Premier League", "premier-league");

  public FotmobTeam Liverpool { get; } = new(8650, "Liverpool");
  public FotmobTeam AFCBournemouth { get; } = new(8678, "AFC Bournemouth");
  public FotmobTeam AstonVilla { get; } = new(10252, "Aston Villa");
  public FotmobTeam NewcastleUnited { get; } = new(10261, "Newcastle United");
  public FotmobTeam TottenhamHotspur { get; } = new(8586, "Tottenham Hotspur");
  public FotmobTeam Burnley { get; } = new(8191, "Burnley");
  public FotmobTeam NottinghamForest { get; } = new(10203, "Nottingham Forest");
  public FotmobTeam Brentford { get; } = new(9937, "Brentford");
  public FotmobTeam Sunderland { get; } = new(8472, "Sunderland");
  public FotmobTeam WestHamUnited { get; } = new(8654, "West Ham United");
  public FotmobTeam BrightonHoveAlbion { get; } = new(10204, "Brighton & Hove Albion");
  public FotmobTeam Fulham { get; } = new(9879, "Fulham");
  public FotmobTeam WolverhamptonWanderers { get; } = new(8602, "Wolverhampton Wanderers");
  public FotmobTeam ManchesterCity { get; } = new(8456, "Manchester City");
  public FotmobTeam Chelsea { get; } = new(8455, "Chelsea");
  public FotmobTeam CrystalPalace { get; } = new(9826, "Crystal Palace");
  public FotmobTeam ManchesterUnited { get; } = new(10260, "Manchester United");
  public FotmobTeam Arsenal { get; } = new(9825, "Arsenal");
  public FotmobTeam LeedsUnited { get; } = new(8463, "Leeds United");
  public FotmobTeam Everton { get; } = new(8668, "Everton");

  private readonly FotmobTeam[] _allTeams;

  public FotmobConstants()
  {
    _allTeams =
    [
      Liverpool, AFCBournemouth, AstonVilla, NewcastleUnited, TottenhamHotspur,
      Burnley, NottinghamForest, Brentford, Sunderland, WestHamUnited,
      BrightonHoveAlbion, Fulham, WolverhamptonWanderers, ManchesterCity, Chelsea,
      CrystalPalace, ManchesterUnited, Arsenal, LeedsUnited, Everton
    ];
  }

  /// <inheritdoc />
  public FotmobTeam? GetTeamById(int id) => _allTeams.FirstOrDefault(t => t.Id == id);

  /// <inheritdoc />
  public FotmobTeam? GetTeamByName(string name) =>
    _allTeams.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
