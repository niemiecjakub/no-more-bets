namespace NoMoreBets.Infrastructure.Scraping.External.Fotmob;

/// <summary>
/// Assigns FotMob teams to domestic seasons.
/// League/Season ids are resolved from the domain via <see cref="FotmobLeague.Slug"/> + <see cref="FotmobSeasonClubAssignment.Year"/>.
/// </summary>
public sealed class FotmobSeasonClubCatalog
{
  public FotmobSeasonClubCatalog(FotmobConstants fotmob)
  {
    Seasons = Build(fotmob);
  }

  public IReadOnlyList<FotmobSeasonClubAssignment> Seasons { get; }

  private static IReadOnlyList<FotmobSeasonClubAssignment> Build(FotmobConstants fotmob) =>
  [
    Assign(
      "2025-2026",
      fotmob.PremierLeague,
      fotmob.Liverpool, fotmob.AFCBournemouth, fotmob.AstonVilla, fotmob.NewcastleUnited,
      fotmob.TottenhamHotspur, fotmob.Burnley, fotmob.NottinghamForest, fotmob.Brentford,
      fotmob.Sunderland, fotmob.WestHamUnited, fotmob.BrightonHoveAlbion, fotmob.Fulham,
      fotmob.WolverhamptonWanderers, fotmob.ManchesterCity, fotmob.Chelsea, fotmob.CrystalPalace,
      fotmob.ManchesterUnited, fotmob.Arsenal, fotmob.LeedsUnited, fotmob.Everton),
    Assign(
      "2026-2027",
      fotmob.PremierLeague,
      fotmob.Arsenal, fotmob.AstonVilla, fotmob.AFCBournemouth, fotmob.Brentford,
      fotmob.BrightonHoveAlbion, fotmob.Chelsea, fotmob.CoventryCity, fotmob.CrystalPalace,
      fotmob.Everton, fotmob.Fulham, fotmob.HullCity, fotmob.IpswichTown,
      fotmob.LeedsUnited, fotmob.Liverpool, fotmob.ManchesterCity, fotmob.ManchesterUnited,
      fotmob.NewcastleUnited, fotmob.NottinghamForest, fotmob.Sunderland, fotmob.TottenhamHotspur),
    Assign(
      "2025-2026",
      fotmob.Ekstraklasa,
      fotmob.ArkaGdynia, fotmob.Cracovia, fotmob.GKSKatowice, fotmob.GornikZabrze,
      fotmob.JagielloniaBialystok, fotmob.KoronaKielce, fotmob.LechPoznan, fotmob.LechiaGdansk,
      fotmob.LegiaWarsaw, fotmob.MotorLublin, fotmob.Nieciecza, fotmob.PiastGliwice,
      fotmob.PogonSzczecin, fotmob.RadomiakRadom, fotmob.RakowCzestochowa, fotmob.WidzewLodz,
      fotmob.WislaPlock, fotmob.ZaglebieLubin),
    Assign(
      "2026-2027",
      fotmob.Ekstraklasa,
      fotmob.Cracovia, fotmob.GKSKatowice, fotmob.GornikZabrze, fotmob.JagielloniaBialystok,
      fotmob.KoronaKielce, fotmob.LechPoznan, fotmob.LegiaWarsaw, fotmob.MotorLublin,
      fotmob.PiastGliwice, fotmob.PogonSzczecin, fotmob.RadomiakRadom, fotmob.RakowCzestochowa,
      fotmob.SlaskWroclaw, fotmob.WidzewLodz, fotmob.WieczystaKrakow, fotmob.WislaKrakow,
      fotmob.WislaPlock, fotmob.ZaglebieLubin),
    Assign(
      "2025-2026",
      fotmob.LaLiga,
      fotmob.Alaves, fotmob.AthleticBilbao, fotmob.AtleticoMadrid, fotmob.Barcelona,
      fotmob.CeltaVigo, fotmob.Elche, fotmob.Espanyol, fotmob.Getafe,
      fotmob.Girona, fotmob.Levante, fotmob.Mallorca, fotmob.Osasuna,
      fotmob.RayoVallecano, fotmob.RealBetis, fotmob.RealMadrid, fotmob.RealOviedo,
      fotmob.RealSociedad, fotmob.Sevilla, fotmob.Valencia, fotmob.Villarreal),
    Assign(
      "2026-2027",
      fotmob.LaLiga,
      fotmob.Alaves, fotmob.AthleticBilbao, fotmob.AtleticoMadrid, fotmob.Barcelona,
      fotmob.CeltaVigo, fotmob.DeportivoLaCoruna, fotmob.Elche, fotmob.Espanyol,
      fotmob.Getafe, fotmob.Levante, fotmob.Malaga, fotmob.Osasuna,
      fotmob.RacingSantander, fotmob.RayoVallecano, fotmob.RealBetis, fotmob.RealMadrid,
      fotmob.RealSociedad, fotmob.Sevilla, fotmob.Valencia, fotmob.Villarreal),
    Assign(
      "2025-2026",
      fotmob.Bundesliga,
      fotmob.Augsburg, fotmob.BayerLeverkusen, fotmob.BayernMunich, fotmob.BorussiaDortmund,
      fotmob.BorussiaMGladbach, fotmob.EintrachtFrankfurt, fotmob.FCCologne, fotmob.Freiburg,
      fotmob.Hamburg, fotmob.Heidenheim, fotmob.Hoffenheim, fotmob.Mainz,
      fotmob.RBLeipzig, fotmob.StPauli, fotmob.Stuttgart, fotmob.UnionBerlin,
      fotmob.WerderBremen, fotmob.Wolfsburg),
    Assign(
      "2026-2027",
      fotmob.Bundesliga,
      fotmob.Augsburg, fotmob.BayerLeverkusen, fotmob.BayernMunich, fotmob.BorussiaDortmund,
      fotmob.BorussiaMGladbach, fotmob.EintrachtFrankfurt, fotmob.Elversberg, fotmob.FCCologne,
      fotmob.Freiburg, fotmob.Hamburg, fotmob.Hoffenheim, fotmob.Mainz,
      fotmob.Paderborn, fotmob.RBLeipzig, fotmob.Schalke04, fotmob.Stuttgart,
      fotmob.UnionBerlin, fotmob.WerderBremen),
    Assign(
      "2025-2026",
      fotmob.SerieA,
      fotmob.ACMilan, fotmob.Atalanta, fotmob.Bologna, fotmob.Cagliari,
      fotmob.Como, fotmob.Cremonese, fotmob.Fiorentina, fotmob.Genoa,
      fotmob.InterMilan, fotmob.Juventus, fotmob.Lazio, fotmob.Lecce,
      fotmob.Napoli, fotmob.Parma, fotmob.Pisa, fotmob.Roma,
      fotmob.Sassuolo, fotmob.Torino, fotmob.Udinese, fotmob.Verona),
    Assign(
      "2026-2027",
      fotmob.SerieA,
      fotmob.ACMilan, fotmob.Atalanta, fotmob.Bologna, fotmob.Cagliari,
      fotmob.Como, fotmob.Fiorentina, fotmob.Frosinone, fotmob.Genoa,
      fotmob.InterMilan, fotmob.Juventus, fotmob.Lazio, fotmob.Lecce,
      fotmob.Monza, fotmob.Napoli, fotmob.Parma, fotmob.Roma,
      fotmob.Sassuolo, fotmob.Torino, fotmob.Udinese, fotmob.Venezia),
    Assign(
      "2025-2026",
      fotmob.Ligue1,
      fotmob.Angers, fotmob.Auxerre, fotmob.Brest, fotmob.LeHavre,
      fotmob.Lens, fotmob.Lille, fotmob.Lorient, fotmob.Lyon,
      fotmob.Marseille, fotmob.Metz, fotmob.Monaco, fotmob.Nantes,
      fotmob.Nice, fotmob.ParisFC, fotmob.PSG, fotmob.Rennes,
      fotmob.Strasbourg, fotmob.Toulouse),
    Assign(
      "2026-2027",
      fotmob.Ligue1,
      fotmob.Angers, fotmob.Auxerre, fotmob.Brest, fotmob.LeHavre,
      fotmob.LeMans, fotmob.Lens, fotmob.Lille, fotmob.Lorient,
      fotmob.Lyon, fotmob.Marseille, fotmob.Monaco, fotmob.Nice,
      fotmob.ParisFC, fotmob.PSG, fotmob.Rennes, fotmob.Strasbourg,
      fotmob.Toulouse, fotmob.Troyes)
  ];

  private static FotmobSeasonClubAssignment Assign(
    string year,
    FotmobLeague fotmobLeague,
    params FotmobTeam[] teams) =>
    new(year, fotmobLeague, teams);
}

public sealed record FotmobSeasonClubAssignment(
  string Year,
  FotmobLeague FotmobLeague,
  IReadOnlyList<FotmobTeam> Teams);
