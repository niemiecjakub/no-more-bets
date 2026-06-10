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
  public FotmobLeague Ligue1 { get; } = new(53, "Ligue 1", "ligue-1");
  public FotmobLeague WorldCup { get; } = new(77, "FIFA World Cup", "world-cup");

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
  public FotmobTeam Alaves { get; } = new(9866, "Alaves");
  public FotmobTeam AthleticBilbao { get; } = new(8315, "Athletic Bilbao");
  public FotmobTeam AtleticoMadrid { get; } = new(9906, "Atletico Madrid");
  public FotmobTeam Barcelona { get; } = new(8634, "Barcelona");
  public FotmobTeam CeltaVigo { get; } = new(9910, "Celta Vigo");
  public FotmobTeam Elche { get; } = new(10268, "Elche");
  public FotmobTeam Espanyol { get; } = new(8558, "Espanyol");
  public FotmobTeam Getafe { get; } = new(8305, "Getafe");
  public FotmobTeam Girona { get; } = new(7732, "Girona");
  public FotmobTeam Levante { get; } = new(8581, "Levante");
  public FotmobTeam Mallorca { get; } = new(8661, "Mallorca");
  public FotmobTeam Osasuna { get; } = new(8371, "Osasuna");
  public FotmobTeam RayoVallecano { get; } = new(8370, "Rayo Vallecano");
  public FotmobTeam RealBetis { get; } = new(8603, "Real Betis");
  public FotmobTeam RealMadrid { get; } = new(8633, "Real Madrid");
  public FotmobTeam RealOviedo { get; } = new(8670, "Real Oviedo");
  public FotmobTeam RealSociedad { get; } = new(8560, "Real Sociedad");
  public FotmobTeam Sevilla { get; } = new(8302, "Sevilla");
  public FotmobTeam Valencia { get; } = new(10267, "Valencia");
  public FotmobTeam Villarreal { get; } = new(10205, "Villarreal");

  // Serie A
  public FotmobTeam ACMilan { get; } = new(8564, "AC Milan");
  public FotmobTeam Atalanta { get; } = new(8524, "Atalanta");
  public FotmobTeam Bologna { get; } = new(9857, "Bologna");
  public FotmobTeam Cagliari { get; } = new(8529, "Cagliari");
  public FotmobTeam Como { get; } = new(10171, "Como");
  public FotmobTeam Cremonese { get; } = new(7801, "Cremonese");
  public FotmobTeam Fiorentina { get; } = new(8535, "Fiorentina");
  public FotmobTeam Genoa { get; } = new(10233, "Genoa");
  public FotmobTeam InterMilan { get; } = new(8636, "Inter Milan");
  public FotmobTeam Juventus { get; } = new(9885, "Juventus");
  public FotmobTeam Lazio { get; } = new(8543, "Lazio");
  public FotmobTeam Lecce { get; } = new(9888, "Lecce");
  public FotmobTeam Napoli { get; } = new(9875, "Napoli");
  public FotmobTeam Parma { get; } = new(10167, "Parma");
  public FotmobTeam Pisa { get; } = new(6479, "Pisa");
  public FotmobTeam Roma { get; } = new(8686, "Roma");
  public FotmobTeam Sassuolo { get; } = new(7943, "Sassuolo");
  public FotmobTeam Torino { get; } = new(9804, "Torino");
  public FotmobTeam Udinese { get; } = new(8600, "Udinese");
  public FotmobTeam Verona { get; } = new(9876, "Verona");

  // Ligue 1
  public FotmobTeam Angers { get; } = new(8121, "Angers");
  public FotmobTeam Auxerre { get; } = new(8583, "Auxerre");
  public FotmobTeam Brest { get; } = new(8521, "Brest");
  public FotmobTeam LeHavre { get; } = new(9746, "Le Havre");
  public FotmobTeam Lens { get; } = new(8588, "Lens");
  public FotmobTeam Lille { get; } = new(8639, "Lille");
  public FotmobTeam Lorient { get; } = new(8689, "Lorient");
  public FotmobTeam Lyon { get; } = new(9748, "Lyon");
  public FotmobTeam Marseille { get; } = new(8592, "Marseille");
  public FotmobTeam Metz { get; } = new(8550, "Metz");
  public FotmobTeam Monaco { get; } = new(9829, "Monaco");
  public FotmobTeam Nantes { get; } = new(9830, "Nantes");
  public FotmobTeam Nice { get; } = new(9831, "Nice");
  public FotmobTeam ParisFC { get; } = new(6379, "Paris FC");
  public FotmobTeam PSG { get; } = new(9847, "PSG");
  public FotmobTeam Rennes { get; } = new(9851, "Rennes");
  public FotmobTeam Strasbourg { get; } = new(9848, "Strasbourg");
  public FotmobTeam Toulouse { get; } = new(9941, "Toulouse");

  // FIFA World Cup
  public FotmobTeam Algeria { get; } = new(6317, "Algeria");
  public FotmobTeam Argentina { get; } = new(6706, "Argentina");
  public FotmobTeam Australia { get; } = new(6716, "Australia");
  public FotmobTeam Austria { get; } = new(8255, "Austria");
  public FotmobTeam Belgium { get; } = new(8263, "Belgium");
  public FotmobTeam BosniaHerzegovina { get; } = new(10106, "Bosnia-Herzegovina");
  public FotmobTeam Brazil { get; } = new(8256, "Brazil");
  public FotmobTeam CaboVerde { get; } = new(5888, "Cabo Verde");
  public FotmobTeam Canada { get; } = new(5810, "Canada");
  public FotmobTeam Colombia { get; } = new(8258, "Colombia");
  public FotmobTeam CongoDR { get; } = new(6321, "Congo DR");
  public FotmobTeam CoteDIvoire { get; } = new(6709, "Cote d'Ivoire");
  public FotmobTeam Croatia { get; } = new(10155, "Croatia");
  public FotmobTeam Curacao { get; } = new(287981, "Curacao");
  public FotmobTeam Czechia { get; } = new(8496, "Czechia");
  public FotmobTeam Ecuador { get; } = new(6707, "Ecuador");
  public FotmobTeam Egypt { get; } = new(10255, "Egypt");
  public FotmobTeam England { get; } = new(8491, "England");
  public FotmobTeam France { get; } = new(6723, "France");
  public FotmobTeam Germany { get; } = new(8570, "Germany");
  public FotmobTeam Ghana { get; } = new(6714, "Ghana");
  public FotmobTeam Haiti { get; } = new(5934, "Haiti");
  public FotmobTeam IRIran { get; } = new(6711, "IR Iran");
  public FotmobTeam Iraq { get; } = new(5819, "Iraq");
  public FotmobTeam Japan { get; } = new(6715, "Japan");
  public FotmobTeam Jordan { get; } = new(5816, "Jordan");
  public FotmobTeam KoreaRepublic { get; } = new(7804, "Korea Republic");
  public FotmobTeam Mexico { get; } = new(6710, "Mexico");
  public FotmobTeam Morocco { get; } = new(6262, "Morocco");
  public FotmobTeam Netherlands { get; } = new(6708, "Netherlands");
  public FotmobTeam NewZealand { get; } = new(5820, "New Zealand");
  public FotmobTeam Norway { get; } = new(8492, "Norway");
  public FotmobTeam Panama { get; } = new(5922, "Panama");
  public FotmobTeam Paraguay { get; } = new(6724, "Paraguay");
  public FotmobTeam Portugal { get; } = new(8361, "Portugal");
  public FotmobTeam Qatar { get; } = new(5902, "Qatar");
  public FotmobTeam SaudiArabia { get; } = new(7795, "Saudi Arabia");
  public FotmobTeam Scotland { get; } = new(8498, "Scotland");
  public FotmobTeam Senegal { get; } = new(6395, "Senegal");
  public FotmobTeam SouthAfrica { get; } = new(6316, "South Africa");
  public FotmobTeam Spain { get; } = new(6720, "Spain");
  public FotmobTeam Sweden { get; } = new(8520, "Sweden");
  public FotmobTeam Switzerland { get; } = new(6717, "Switzerland");
  public FotmobTeam Tunisia { get; } = new(6719, "Tunisia");
  public FotmobTeam Turkiye { get; } = new(6595, "Turkiye");
  public FotmobTeam UnitedStates { get; } = new(6713, "United States");
  public FotmobTeam Uruguay { get; } = new(5796, "Uruguay");
  public FotmobTeam Uzbekistan { get; } = new(8700, "Uzbekistan");

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
      [WorldCup.Slug] = WorldCup,

      // League.Slug in DB seed where it differs from FotMob path slugs.
      ["serie-a"] = SerieA,
      ["fifa-world-cup"] = WorldCup
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
      Rennes, Strasbourg, Toulouse,

      // FIFA World Cup
      Algeria, Argentina, Australia, Austria, Belgium,
      BosniaHerzegovina, Brazil, CaboVerde, Canada, Colombia,
      CongoDR, CoteDIvoire, Croatia, Curacao, Czechia,
      Ecuador, Egypt, England, France, Germany,
      Ghana, Haiti, IRIran, Iraq, Japan,
      Jordan, KoreaRepublic, Mexico, Morocco, Netherlands,
      NewZealand, Norway, Panama, Paraguay, Portugal,
      Qatar, SaudiArabia, Scotland, Senegal, SouthAfrica,
      Spain, Sweden, Switzerland, Tunisia, Turkiye,
      UnitedStates, Uruguay, Uzbekistan
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
  public FotmobTeam? GetTeamByName(string name) =>
    _allTeams.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
}
