namespace NoMoreBets.Infrastructure.Scraping.External.Fotmob;

/// <summary>
/// FotMob league and team constants. Registered as singleton.
/// </summary>
public class FotmobConstants : IFotmobConstants
{
  public FotmobLeague PremierLeague { get; } = new(47, "Premier League", "premier-league");
  public FotmobLeague Ekstraklasa { get; } = new(196, "Ekstraklasa", "ekstraklasa");
  public FotmobLeague Bundesliga { get; } = new(54, "Bundesliga", "bundesliga");
  public FotmobLeague LaLiga { get; } = new(87, "LaLiga", "laliga");
  public FotmobLeague SerieA { get; } = new(55, "Serie A", "serie");
  public FotmobLeague Ligue1 { get; } = new(53, "Ligue 1", "ligue1");

  // Premier League
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

  // Ekstraklasa
  public FotmobTeam ArkaGdynia { get; } = new(8322, "Arka Gdynia");
  public FotmobTeam Cracovia { get; } = new(2186, "Cracovia");
  public FotmobTeam GKSKatowice { get; } = new(4023, "GKS Katowice");
  public FotmobTeam GornikZabrze { get; } = new(8020, "Gornik Zabrze");
  public FotmobTeam JagielloniaBialystok { get; } = new(1957, "Jagiellonia Bialystok");
  public FotmobTeam KoronaKielce { get; } = new(8245, "Korona Kielce");
  public FotmobTeam LechPoznan { get; } = new(2182, "Lech Poznan");
  public FotmobTeam LechiaGdansk { get; } = new(8030, "Lechia Gdansk");
  public FotmobTeam LegiaWarsaw { get; } = new(8673, "Legia Warsaw");
  public FotmobTeam MotorLublin { get; } = new(89466, "Motor Lublin");
  public FotmobTeam Nieciecza { get; } = new(177361, "Nieciecza");
  public FotmobTeam PiastGliwice { get; } = new(8028, "Piast Gliwice");
  public FotmobTeam PogonSzczecin { get; } = new(8023, "Pogon Szczecin");
  public FotmobTeam RadomiakRadom { get; } = new(5769, "Radomiak Radom");
  public FotmobTeam RakowCzestochowa { get; } = new(4024, "Rakow Czestochowa");
  public FotmobTeam WidzewLodz { get; } = new(8024, "Widzew Lodz");
  public FotmobTeam WislaPlock { get; } = new(8243, "Wisla Plock");
  public FotmobTeam ZaglebieLubin { get; } = new(8021, "Zaglebie Lubin");

  // Bundesliga
  public FotmobTeam Augsburg { get; } = new(8406, "Augsburg");
  public FotmobTeam BayerLeverkusen { get; } = new(8178, "Bayer Leverkusen");
  public FotmobTeam BayernMunich { get; } = new(9823, "Bayern Munich");
  public FotmobTeam BorussiaDortmund { get; } = new(9789, "Borussia Dortmund");
  public FotmobTeam BorussiaMGladbach { get; } = new(9788, "Borussia M'gladbach");
  public FotmobTeam EintrachtFrankfurt { get; } = new(9810, "Eintracht Frankfurt");
  public FotmobTeam FCCologne { get; } = new(8722, "FC Cologne");
  public FotmobTeam Freiburg { get; } = new(8358, "Freiburg");
  public FotmobTeam Hamburg { get; } = new(9790, "Hamburg");
  public FotmobTeam Heidenheim { get; } = new(94937, "Heidenheim");
  public FotmobTeam Hoffenheim { get; } = new(8226, "Hoffenheim");
  public FotmobTeam Mainz { get; } = new(9905, "Mainz");
  public FotmobTeam RBLeipzig { get; } = new(178475, "RB Leipzig");
  public FotmobTeam StPauli { get; } = new(8152, "St. Pauli");
  public FotmobTeam Stuttgart { get; } = new(10269, "Stuttgart");
  public FotmobTeam UnionBerlin { get; } = new(8149, "Union Berlin");
  public FotmobTeam WerderBremen { get; } = new(8697, "Werder Bremen");
  public FotmobTeam Wolfsburg { get; } = new(8721, "Wolfsburg");

  // LaLiga
  public FotmobTeam Alaves { get; } = new(4892, "Alaves");
  public FotmobTeam AthleticBilbao { get; } = new(4887, "Athletic Bilbao");
  public FotmobTeam AtleticoMadrid { get; } = new(4882, "Atletico Madrid");
  public FotmobTeam Barcelona { get; } = new(4884, "Barcelona");
  public FotmobTeam CeltaVigo { get; } = new(2911, "Celta Vigo");
  public FotmobTeam Elche { get; } = new(4893, "Elche");
  public FotmobTeam Espanyol { get; } = new(4895, "Espanyol");
  public FotmobTeam Getafe { get; } = new(4891, "Getafe");
  public FotmobTeam Girona { get; } = new(2798, "Girona");
  public FotmobTeam Levante { get; } = new(2914, "Levante");
  public FotmobTeam Mallorca { get; } = new(2802, "Mallorca");
  public FotmobTeam Osasuna { get; } = new(4888, "Osasuna");
  public FotmobTeam RayoVallecano { get; } = new(2910, "Rayo Vallecano");
  public FotmobTeam RealBetis { get; } = new(4886, "Real Betis");
  public FotmobTeam RealMadrid { get; } = new(4883, "Real Madrid");
  public FotmobTeam RealOviedo { get; } = new(4901, "Real Oviedo");
  public FotmobTeam RealSociedad { get; } = new(4885, "Real Sociedad");
  public FotmobTeam Sevilla { get; } = new(3019, "Sevilla");
  public FotmobTeam Valencia { get; } = new(4890, "Valencia");
  public FotmobTeam Villarreal { get; } = new(2907, "Villarreal");

  // Serie A
  public FotmobTeam ACMilan { get; } = new(4407, "AC Milan");
  public FotmobTeam Atalanta { get; } = new(2950, "Atalanta");
  public FotmobTeam Bologna { get; } = new(2820, "Bologna");
  public FotmobTeam Cagliari { get; } = new(4410, "Cagliari");
  public FotmobTeam Como { get; } = new(2975, "Como");
  public FotmobTeam Cremonese { get; } = new(2981, "Cremonese");
  public FotmobTeam Fiorentina { get; } = new(2969, "Fiorentina");
  public FotmobTeam Genoa { get; } = new(4411, "Genoa");
  public FotmobTeam InterMilan { get; } = new(3296, "Inter Milan");
  public FotmobTeam Juventus { get; } = new(4406, "Juventus");
  public FotmobTeam Lazio { get; } = new(3582, "Lazio");
  public FotmobTeam Lecce { get; } = new(2978, "Lecce");
  public FotmobTeam Napoli { get; } = new(2984, "Napoli");
  public FotmobTeam Parma { get; } = new(4409, "Parma");
  public FotmobTeam Pisa { get; } = new(3388, "Pisa");
  public FotmobTeam Roma { get; } = new(3767, "Roma");
  public FotmobTeam Sassuolo { get; } = new(4408, "Sassuolo");
  public FotmobTeam Torino { get; } = new(3062, "Torino");
  public FotmobTeam Udinese { get; } = new(3206, "Udinese");
  public FotmobTeam Verona { get; } = new(2982, "Verona");

  // Ligue 1
  public FotmobTeam Angers { get; } = new(2849, "Angers");
  public FotmobTeam Auxerre { get; } = new(3002, "Auxerre");
  public FotmobTeam Brest { get; } = new(3199, "Brest");
  public FotmobTeam LeHavre { get; } = new(3139, "Le Havre");
  public FotmobTeam Lens { get; } = new(3295, "Lens");
  public FotmobTeam Lille { get; } = new(4229, "Lille");
  public FotmobTeam Lorient { get; } = new(4239, "Lorient");
  public FotmobTeam Lyon { get; } = new(4230, "Lyon");
  public FotmobTeam Marseille { get; } = new(3769, "Marseille");
  public FotmobTeam Metz { get; } = new(4238, "Metz");
  public FotmobTeam Monaco { get; } = new(4234, "Monaco");
  public FotmobTeam Nantes { get; } = new(4232, "Nantes");
  public FotmobTeam Nice { get; } = new(3027, "Nice");
  public FotmobTeam ParisFC { get; } = new(4241, "Paris FC");
  public FotmobTeam PSG { get; } = new(4228, "PSG");
  public FotmobTeam Rennes { get; } = new(3001, "Rennes");
  public FotmobTeam Strasbourg { get; } = new(4231, "Strasbourg");
  public FotmobTeam Toulouse { get; } = new(3129, "Toulouse");

  private readonly FotmobTeam[] _allTeams;
  private readonly Dictionary<string, FotmobLeague> _leaguesBySlug;

  public FotmobConstants()
  {
    _leaguesBySlug = new Dictionary<string, FotmobLeague>(StringComparer.OrdinalIgnoreCase)
    {
      [PremierLeague.Slug] = PremierLeague,
      [Ekstraklasa.Slug] = Ekstraklasa,
      [Bundesliga.Slug] = Bundesliga,
      [LaLiga.Slug] = LaLiga,
      [SerieA.Slug] = SerieA,
      [Ligue1.Slug] = Ligue1,

      // League.Slug in DB (002 seed) where it differs from FotMob path slugs.
      ["serie-a"] = SerieA,
      ["ligue-1"] = Ligue1
    };

    _allTeams =
    [
      // Premier League
      Liverpool, AFCBournemouth, AstonVilla, NewcastleUnited, TottenhamHotspur,
      Burnley, NottinghamForest, Brentford, Sunderland, WestHamUnited,
      BrightonHoveAlbion, Fulham, WolverhamptonWanderers, ManchesterCity, Chelsea,
      CrystalPalace, ManchesterUnited, Arsenal, LeedsUnited, Everton,

      // Ekstraklasa
      ArkaGdynia, Cracovia, GKSKatowice, GornikZabrze, JagielloniaBialystok,
      KoronaKielce, LechPoznan, LechiaGdansk, LegiaWarsaw, MotorLublin,
      Nieciecza, PiastGliwice, PogonSzczecin, RadomiakRadom, RakowCzestochowa,
      WidzewLodz, WislaPlock, ZaglebieLubin,

      // Bundesliga
      Augsburg, BayerLeverkusen, BayernMunich, BorussiaDortmund, BorussiaMGladbach,
      EintrachtFrankfurt, FCCologne, Freiburg, Hamburg, Heidenheim,
      Hoffenheim, Mainz, RBLeipzig, StPauli, Stuttgart,
      UnionBerlin, WerderBremen, Wolfsburg,

      // LaLiga
      Alaves, AthleticBilbao, AtleticoMadrid, Barcelona, CeltaVigo,
      Elche, Espanyol, Getafe, Girona, Levante,
      Mallorca, Osasuna, RayoVallecano, RealBetis,
      RealMadrid, RealOviedo, RealSociedad, Sevilla, Valencia,
      Villarreal,

      // Serie A
      ACMilan, Atalanta, Bologna, Cagliari, Como,
      Cremonese, Fiorentina, Genoa, InterMilan, Juventus,
      Lazio, Lecce, Napoli, Parma, Pisa,
      Roma, Sassuolo, Torino, Udinese, Verona,

      // Ligue 1
      Angers, Auxerre, Brest, LeHavre, Lens,
      Lille, Lorient, Lyon, Marseille, Metz,
      Monaco, Nantes, Nice, ParisFC, PSG,
      Rennes, Strasbourg, Toulouse
    ];
  }

  /// <inheritdoc />
  public FotmobLeague? GetLeagueBySlug(string slug)
  {
    if (string.IsNullOrWhiteSpace(slug))
      return null;

    var normalized = slug.Trim();
    return _leaguesBySlug.GetValueOrDefault(normalized);
  }

  /// <inheritdoc />
  public FotmobTeam? GetTeamById(int id) => _allTeams.FirstOrDefault(t => t.Id == id);

  /// <inheritdoc />
  public FotmobTeam? GetTeamByName(string name) =>
    _allTeams.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
