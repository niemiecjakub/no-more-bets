using FluentAssertions;
using NoMoreBets.Infrastructure.Scraping.External.Fotmob;

namespace NoMoreBets.Infrastructure.Tests.Scraping.External.Fotmob;

public class FotmobConstantsTests
{
  private readonly FotmobConstants _sut = new();

  [Theory]
  [InlineData("premier-league", 47)]
  [InlineData("ekstraklasa", 196)]
  [InlineData("laliga", 87)]
  [InlineData("bundesliga", 54)]
  [InlineData("serie", 55)]
  [InlineData("serie-a", 55)]
  [InlineData("ligue1", 53)]
  [InlineData("ligue-1", 53)]
  public void GetLeagueBySlug_ForCanonicalAndDbSeedSlugs_ReturnsExpectedFotmobId(string slug, int expectedLeagueId)
  {
    var league = _sut.GetLeagueBySlug(slug);

    league.Should().NotBeNull();
    league!.Id.Should().Be(expectedLeagueId);
  }

  [Fact]
  public void GetLeagueBySlug_WhenUnknown_ReturnsNull()
  {
    _sut.GetLeagueBySlug("not-a-league").Should().BeNull();
  }
}
