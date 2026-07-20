using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Clubs;
using NoMoreBets.Application.Clubs.UpdateDailySummary;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Tests.Club.UpdateDailySummary;

public class UpdateDailySummaryHandlerTests
{
  private readonly IClubOverviewProvider _clubOverviewProvider;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<UpdateDailySummaryHandler> _logger;
  private readonly UpdateDailySummaryHandler _sut;

  public UpdateDailySummaryHandlerTests()
  {
    _clubOverviewProvider = Substitute.For<IClubOverviewProvider>();
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _logger = Substitute.For<ILogger<UpdateDailySummaryHandler>>();
    _sut = new UpdateDailySummaryHandler(_clubOverviewProvider, _unitOfWork, _logger);
  }

  [Fact]
  public async Task Handle_WhenClubNotFound_ReturnsWithoutSaving()
  {
    _unitOfWork.Clubs.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<NoMoreBets.Domain.Clubs.Club?>(null));

    var result = await _sut.Handle(new UpdateDailySummaryCommand(42, "Summary text"), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.Clubs.DidNotReceive().AddDailySummaryAsync(Arg.Any<ClubDailySummary>(), Arg.Any<CancellationToken>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenLatestSummarySameAsRequest_SkipsInsert()
  {
    var club = new NoMoreBets.Domain.Clubs.Club { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    _unitOfWork.Clubs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<NoMoreBets.Domain.Clubs.Club?>(club));
    var latestSummary = new ClubDailySummary { Id = 1, ClubId = 1, Summary = "Same summary" };
    _unitOfWork.Clubs.GetDailySummaryAsync(1, null, Arg.Any<CancellationToken>()).Returns(latestSummary);

    var result = await _sut.Handle(new UpdateDailySummaryCommand(1, "Same summary"), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.Clubs.DidNotReceive().AddDailySummaryAsync(Arg.Any<ClubDailySummary>(), Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenSummaryDifferent_AddsDailySummaryAndSaveChanges()
  {
    var club = new NoMoreBets.Domain.Clubs.Club { Id = 1, Name = "Arsenal", SoccerdataId = 1 };
    _unitOfWork.Clubs.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult<NoMoreBets.Domain.Clubs.Club?>(club));
    _unitOfWork.Clubs.GetDailySummaryAsync(1, null, Arg.Any<CancellationToken>()).Returns((ClubDailySummary?)null);

    await _sut.Handle(new UpdateDailySummaryCommand(1, "New summary"), CancellationToken.None);

    await _unitOfWork.Clubs.Received(1).AddDailySummaryAsync(
      Arg.Is<ClubDailySummary>(s => s.ClubId == 1 && s.Summary == "New summary"),
      Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
