using NoMoreBets.Application.Leagues;

namespace NoMoreBets.Infrastructure.Scraping.External.Fotmob;

public sealed class FotmobWorldCupGroupDefinitions
{
  public FotmobWorldCupGroupDefinitions(FotmobConstants fotmobConstants)
  {
    Groups = BuildGroups(fotmobConstants);
  }

  public IReadOnlyList<WorldCupGroupDefinition> Groups { get; }

  private static IReadOnlyList<WorldCupGroupDefinition> BuildGroups(FotmobConstants fotmob)
  {
    WorldCupGroupDefinition Group(string code, params FotmobTeam[] teams) =>
      new(
        code,
        $"Grp. {code}",
        teams.Select(t => t.Id).ToArray(),
        teams.Select(t => t.Name).ToArray());

    return
    [
      Group("A", fotmob.Mexico, fotmob.KoreaRepublic, fotmob.Czechia, fotmob.SouthAfrica),
      Group("B", fotmob.Switzerland, fotmob.Canada, fotmob.BosniaHerzegovina, fotmob.Qatar),
      Group("C", fotmob.Brazil, fotmob.Haiti, fotmob.Morocco, fotmob.Scotland),
      Group("D", fotmob.UnitedStates, fotmob.Australia, fotmob.Turkiye, fotmob.Paraguay),
      Group("E", fotmob.Curacao, fotmob.Ecuador, fotmob.Germany, fotmob.CoteDIvoire),
      Group("F", fotmob.Japan, fotmob.Netherlands, fotmob.Sweden, fotmob.Tunisia),
      Group("G", fotmob.Belgium, fotmob.Egypt, fotmob.IRIran, fotmob.NewZealand),
      Group("H", fotmob.CaboVerde, fotmob.SaudiArabia, fotmob.Spain, fotmob.Uruguay),
      Group("I", fotmob.France, fotmob.Iraq, fotmob.Norway, fotmob.Senegal),
      Group("J", fotmob.Algeria, fotmob.Argentina, fotmob.Austria, fotmob.Jordan),
      Group("K", fotmob.Colombia, fotmob.CongoDR, fotmob.Portugal, fotmob.Uzbekistan),
      Group("L", fotmob.Croatia, fotmob.England, fotmob.Ghana, fotmob.Panama),
    ];
  }
}
