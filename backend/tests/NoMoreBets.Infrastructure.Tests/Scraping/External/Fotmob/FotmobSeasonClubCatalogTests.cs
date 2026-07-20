using FluentAssertions;
using NoMoreBets.Infrastructure.Scraping.External.Fotmob;

namespace NoMoreBets.Infrastructure.Tests.Scraping.External.Fotmob;

public class FotmobSeasonClubCatalogTests
{
  [Fact]
  public void Constructor_WithExistingMappings_AssignsDomesticSeasons()
  {
    // Arrange
    var fotmob = new FotmobConstants();

    // Act
    var catalog = new FotmobSeasonClubCatalog(fotmob);

    // Assert
    catalog.Seasons.Count(entry => entry.Year == "2025-2026").Should().Be(6);
    catalog.Seasons.Count(entry => entry.Year == "2026-2027").Should().Be(6);
    catalog.Seasons.Where(entry => entry.Year == "2025-2026").Sum(entry => entry.Teams.Count)
      .Should().Be(114);
  }

  [Fact]
  public void Constructor_PremierLeague2026_2027_HasPromotedClubsAndTwentyTeams()
  {
    // Arrange
    var catalog = new FotmobSeasonClubCatalog(new FotmobConstants());

    // Act
    var premierLeague = catalog.Seasons.Single(entry =>
      entry.Year == "2026-2027" && entry.FotmobLeague.Slug == "premier-league");

    // Assert
    premierLeague.Teams.Should().HaveCount(20);
    premierLeague.Teams.Select(team => team.Name).Should().BeEquivalentTo(
      "Arsenal",
      "Aston Villa",
      "AFC Bournemouth",
      "Brentford",
      "Brighton & Hove Albion",
      "Chelsea",
      "Coventry City",
      "Crystal Palace",
      "Everton",
      "Fulham",
      "Hull City",
      "Ipswich Town",
      "Leeds United",
      "Liverpool",
      "Manchester City",
      "Manchester United",
      "Newcastle United",
      "Nottingham Forest",
      "Sunderland",
      "Tottenham Hotspur");
    premierLeague.Teams.Single(team => team.Name == "Coventry City").Id.Should().Be(8669);
    premierLeague.Teams.Single(team => team.Name == "Hull City").Id.Should().Be(8667);
    premierLeague.Teams.Single(team => team.Name == "Ipswich Town").Id.Should().Be(9902);
  }

  [Fact]
  public void Constructor_SerieA2026_2027_HasPromotedClubsAndTwentyTeams()
  {
    // Arrange
    var catalog = new FotmobSeasonClubCatalog(new FotmobConstants());

    // Act
    var serieA = catalog.Seasons.Single(entry =>
      entry.Year == "2026-2027" && entry.FotmobLeague.Slug == "serie");

    // Assert
    serieA.Teams.Should().HaveCount(20);
    serieA.Teams.Select(team => team.Name).Should().BeEquivalentTo(
      "AC Milan",
      "Atalanta",
      "Bologna",
      "Cagliari",
      "Como",
      "Fiorentina",
      "Frosinone",
      "Genoa",
      "Inter Milan",
      "Juventus",
      "Lazio",
      "Lecce",
      "Monza",
      "Napoli",
      "Parma",
      "Roma",
      "Sassuolo",
      "Torino",
      "Udinese",
      "Venezia");
    serieA.Teams.Single(team => team.Name == "Frosinone").Id.Should().Be(9891);
    serieA.Teams.Single(team => team.Name == "Monza").Id.Should().Be(6504);
    serieA.Teams.Single(team => team.Name == "Venezia").Id.Should().Be(7881);
  }

  [Fact]
  public void Constructor_Ekstraklasa2026_2027_HasPromotedClubsAndEighteenTeams()
  {
    // Arrange
    var catalog = new FotmobSeasonClubCatalog(new FotmobConstants());

    // Act
    var ekstraklasa = catalog.Seasons.Single(entry =>
      entry.Year == "2026-2027" && entry.FotmobLeague.Slug == "ekstraklasa");

    // Assert
    ekstraklasa.Teams.Should().HaveCount(18);
    ekstraklasa.Teams.Select(team => team.Name).Should().BeEquivalentTo(
      "Cracovia",
      "GKS Katowice",
      "Gornik Zabrze",
      "Jagiellonia Bialystok",
      "Korona Kielce",
      "Lech Poznan",
      "Legia Warsaw",
      "Motor Lublin",
      "Piast Gliwice",
      "Pogon Szczecin",
      "Radomiak Radom",
      "Rakow Czestochowa",
      "Slask Wroclaw",
      "Widzew Lodz",
      "Wieczysta Krakow",
      "Wisla Krakow",
      "Wisla Plock",
      "Zaglebie Lubin");
    ekstraklasa.Teams.Single(team => team.Name == "Slask Wroclaw").Id.Should().Be(8025);
    ekstraklasa.Teams.Single(team => team.Name == "Wieczysta Krakow").Id.Should().Be(1286895);
    ekstraklasa.Teams.Single(team => team.Name == "Wisla Krakow").Id.Should().Be(10265);
  }

  [Fact]
  public void Constructor_Bundesliga2026_2027_ReplacesRelegatedWithPromotedClubs()
  {
    // Arrange
    var catalog = new FotmobSeasonClubCatalog(new FotmobConstants());

    // Act
    var bundesliga = catalog.Seasons.Single(entry =>
      entry.Year == "2026-2027" && entry.FotmobLeague.Slug == "bundesliga");

    // Assert
    bundesliga.Teams.Should().HaveCount(18);
    bundesliga.Teams.Select(team => team.Name).Should().BeEquivalentTo(
      "Augsburg",
      "Bayer Leverkusen",
      "Bayern Munich",
      "Borussia Dortmund",
      "Borussia M'gladbach",
      "Eintracht Frankfurt",
      "Elversberg",
      "FC Cologne",
      "Freiburg",
      "Hamburg",
      "Hoffenheim",
      "Mainz",
      "Paderborn",
      "RB Leipzig",
      "Schalke 04",
      "Stuttgart",
      "Union Berlin",
      "Werder Bremen");
    bundesliga.Teams.Select(team => team.Name).Should().NotContain(
      ["Heidenheim", "St. Pauli", "Wolfsburg"]);
    bundesliga.Teams.Single(team => team.Name == "Schalke 04").Id.Should().Be(10189);
    bundesliga.Teams.Single(team => team.Name == "Elversberg").Id.Should().Be(8232);
    bundesliga.Teams.Single(team => team.Name == "Paderborn").Id.Should().Be(8460);
  }

  [Fact]
  public void Constructor_LaLiga2026_2027_ReplacesRelegatedWithPromotedClubs()
  {
    // Arrange
    var catalog = new FotmobSeasonClubCatalog(new FotmobConstants());

    // Act
    var laliga = catalog.Seasons.Single(entry =>
      entry.Year == "2026-2027" && entry.FotmobLeague.Slug == "laliga");

    // Assert
    laliga.Teams.Should().HaveCount(20);
    laliga.Teams.Select(team => team.Name).Should().BeEquivalentTo(
      "Alaves",
      "Athletic Bilbao",
      "Atletico Madrid",
      "Barcelona",
      "Celta Vigo",
      "Deportivo La Coruna",
      "Elche",
      "Espanyol",
      "Getafe",
      "Levante",
      "Malaga",
      "Osasuna",
      "Racing Santander",
      "Rayo Vallecano",
      "Real Betis",
      "Real Madrid",
      "Real Sociedad",
      "Sevilla",
      "Valencia",
      "Villarreal");
    laliga.Teams.Select(team => team.Name).Should().NotContain(
      ["Real Oviedo", "Girona", "Mallorca"]);
    laliga.Teams.Single(team => team.Name == "Racing Santander").Id.Should().Be(8696);
    laliga.Teams.Single(team => team.Name == "Deportivo La Coruna").Id.Should().Be(9783);
    laliga.Teams.Single(team => team.Name == "Malaga").Id.Should().Be(9864);
  }

  [Fact]
  public void Constructor_Ligue12026_2027_ReplacesRelegatedWithPromotedClubs()
  {
    // Arrange
    var catalog = new FotmobSeasonClubCatalog(new FotmobConstants());

    // Act
    var ligue1 = catalog.Seasons.Single(entry =>
      entry.Year == "2026-2027" && entry.FotmobLeague.Slug == "ligue-1");

    // Assert
    ligue1.Teams.Should().HaveCount(18);
    ligue1.Teams.Select(team => team.Name).Should().BeEquivalentTo(
      "Angers",
      "Auxerre",
      "Brest",
      "Le Havre",
      "Le Mans",
      "Lens",
      "Lille",
      "Lorient",
      "Lyon",
      "Marseille",
      "Monaco",
      "Nice",
      "Paris FC",
      "PSG",
      "Rennes",
      "Strasbourg",
      "Toulouse",
      "Troyes");
    ligue1.Teams.Select(team => team.Name).Should().NotContain(["Metz", "Nantes"]);
    ligue1.Teams.Single(team => team.Name == "Le Mans").Id.Should().Be(8682);
    ligue1.Teams.Single(team => team.Name == "Troyes").Id.Should().Be(10242);
  }
}
