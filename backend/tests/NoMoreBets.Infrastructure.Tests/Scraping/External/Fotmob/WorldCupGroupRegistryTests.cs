using FluentAssertions;
using NoMoreBets.Application.Leagues;
using NoMoreBets.Domain.Leagues;
using NoMoreBets.Infrastructure.Scraping.External.Fotmob;

namespace NoMoreBets.Infrastructure.Tests.Scraping.External.Fotmob;

public class WorldCupGroupRegistryTests
{
  private readonly WorldCupGroupRegistry _sut = new(new FotmobWorldCupGroupDefinitions(new FotmobConstants()).Groups);

  [Fact]
  public void Groups_ContainsTwelveGroupsWithFourTeamsEach()
  {
    _sut.Groups.Should().HaveCount(12);
    _sut.Groups.Should().OnlyContain(g => g.FotmobTeamIds.Count == 4);
    _sut.Groups.SelectMany(g => g.FotmobTeamIds).Should().OnlyHaveUniqueItems();
    _sut.Groups.SelectMany(g => g.FotmobTeamIds).Should().HaveCount(48);
  }

  [Theory]
  [InlineData("Mexico", "A")]
  [InlineData("Korea Republic", "A")]
  [InlineData("South Korea", "A")]
  [InlineData("England", "L")]
  [InlineData("Cote d'Ivoire", "E")]
  [InlineData("Ivory Coast", "E")]
  [InlineData("Bosnia-Herzegovina", "B")]
  [InlineData("United States", "D")]
  [InlineData("USA", "D")]
  public void GetGroupForClubName_ReturnsExpectedGroup(string clubName, string expectedCode)
  {
    var group = _sut.GetGroupForClubName(clubName);

    group.Should().NotBeNull();
    group!.Code.Should().Be(expectedCode);
    group.Label.Should().Be($"Grp. {expectedCode}");
  }

  [Fact]
  public void IsWorldCupLeagueSlug_RecognizesFifaWorldCupSlug()
  {
    _sut.IsWorldCupLeagueSlug(League.FifaWorldCupSlug).Should().BeTrue();
    _sut.IsWorldCupLeagueSlug("premier-league").Should().BeFalse();
  }

  [Fact]
  public void IsClubInGroup_MatchesResolvedClubName()
  {
    _sut.IsClubInGroup("Mexico", "A").Should().BeTrue();
    _sut.IsClubInGroup("Mexico", "B").Should().BeFalse();
  }
}
