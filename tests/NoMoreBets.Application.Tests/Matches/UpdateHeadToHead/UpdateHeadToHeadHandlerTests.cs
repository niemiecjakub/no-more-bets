using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches;
using NoMoreBets.Application.Matches.UpdateHeadToHead;
using NoMoreBets.Domain.Matches;
using ClubEntity = NoMoreBets.Domain.Clubs.Club;
using NoMoreBets.Domain.Matches.Dto;

namespace NoMoreBets.Application.Tests.Matches.UpdateHeadToHead;

public class UpdateHeadToHeadHandlerTests
{
  private readonly IHeadToHeadProvider _headToHeadProvider;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<UpdateHeadToHeadHandler> _logger;
  private readonly UpdateHeadToHeadHandler _sut;

  public UpdateHeadToHeadHandlerTests()
  {
    _headToHeadProvider = Substitute.For<IHeadToHeadProvider>();
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _logger = Substitute.For<ILogger<UpdateHeadToHeadHandler>>();
    _sut = new UpdateHeadToHeadHandler(_headToHeadProvider, _unitOfWork, _logger);
  }

  private static HeadToHead CreateHeadToHeadDto() =>
    new()
    {
      Team1 = new TeamInfo { Id = 1, Name = "Arsenal" },
      Team2 = new TeamInfo { Id = 2, Name = "Chelsea" },
      Stats = new HeadToHeadStats
      {
        Overall = new OverallStats(),
        Team1AtHome = new Team1AtHomeStats(),
        Team2AtHome = new Team2AtHomeStats()
      }
    };

  [Fact]
  public async Task Handle_WhenOneClubMissingInDb_ReturnsWithoutSaving()
  {
    _headToHeadProvider.GetHeadToHeadAsync(1, 2, Arg.Any<CancellationToken>()).Returns(CreateHeadToHeadDto());
    _unitOfWork.Clubs.GetBySoccerdataId(Arg.Any<IEnumerable<int>>()).Returns(new List<ClubEntity>
    {
      new() { Id = 1, SoccerdataId = 1, Name = "Arsenal", LeagueId = 1 }
    });

    var result = await _sut.Handle(new UpdateHeadToHeadCommand(1, 2), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.Clubs.DidNotReceive().AddHead2Head(Arg.Any<Head2Head>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenBothClubsFound_NoExistingHead2Head_AddsAndSaveChanges()
  {
    _headToHeadProvider.GetHeadToHeadAsync(1, 2, Arg.Any<CancellationToken>()).Returns(CreateHeadToHeadDto());
    _unitOfWork.Clubs.GetBySoccerdataId(Arg.Any<IEnumerable<int>>()).Returns(new List<ClubEntity>
    {
      new() { Id = 1, SoccerdataId = 1, Name = "Arsenal", LeagueId = 1 },
      new() { Id = 2, SoccerdataId = 2, Name = "Chelsea", LeagueId = 1 }
    });
    _unitOfWork.Matches.GetHeadToHead(1, 2).Returns((Head2Head?)null);

    await _sut.Handle(new UpdateHeadToHeadCommand(1, 2), CancellationToken.None);

    await _unitOfWork.Clubs.Received(1).AddHead2Head(Arg.Is<Head2Head>(h => h.Team1Id == 1 && h.Team2Id == 2 && !string.IsNullOrEmpty(h.Head2HeadJson)));
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenBothClubsFound_ExistingHead2Head_UpdatesJsonAndSaveChanges()
  {
    _headToHeadProvider.GetHeadToHeadAsync(1, 2, Arg.Any<CancellationToken>()).Returns(CreateHeadToHeadDto());
    _unitOfWork.Clubs.GetBySoccerdataId(Arg.Any<IEnumerable<int>>()).Returns(new List<ClubEntity>
    {
      new() { Id = 1, SoccerdataId = 1, Name = "Arsenal", LeagueId = 1 },
      new() { Id = 2, SoccerdataId = 2, Name = "Chelsea", LeagueId = 1 }
    });
    var existing = new Head2Head { Team1Id = 1, Team2Id = 2, Head2HeadJson = "old" };
    _unitOfWork.Matches.GetHeadToHead(1, 2).Returns(existing);

    await _sut.Handle(new UpdateHeadToHeadCommand(1, 2), CancellationToken.None);

    existing.Head2HeadJson.Should().NotBe("old");
    await _unitOfWork.Clubs.DidNotReceive().AddHead2Head(Arg.Any<Head2Head>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
