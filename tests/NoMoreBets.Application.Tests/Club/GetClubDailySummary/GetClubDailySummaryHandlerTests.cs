using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Clubs.GetClubDailySummary;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Clubs;

namespace NoMoreBets.Application.Tests.Club.GetClubDailySummary;

public class GetClubDailySummaryHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IClubRepository _clubRepository = Substitute.For<IClubRepository>();
  private readonly GetClubDailySummaryHandler _sut;

  public GetClubDailySummaryHandlerTests()
  {
    _unitOfWork.Clubs.Returns(_clubRepository);
    _sut = new GetClubDailySummaryHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenDateProvided_UsesDateFilterAndReturnsSummary()
  {
    // Arrange
    const int clubId = 10;
    var date = new DateOnly(2026, 3, 15);
    var summary = new ClubDailySummary
    {
      ClubId = clubId,
      Date = new DateOnly(2026, 3, 14),
      Summary = "Strong xG trend."
    };
    _clubRepository.GetDailySummaryAsync(clubId, date, Arg.Any<CancellationToken>())
      .Returns(summary);

    // Act
    var result = await _sut.Handle(new GetClubDailySummaryQuery(clubId, date), CancellationToken.None);

    // Assert
    result.Should().Be(summary.ToString());
  }

  [Fact]
  public async Task Handle_WhenNoSummaryForDateOrEarlier_ReturnsNoSummaryMessage()
  {
    // Arrange
    const int clubId = 11;
    var date = new DateOnly(2026, 1, 1);
    _clubRepository.GetDailySummaryAsync(clubId, date, Arg.Any<CancellationToken>())
      .Returns((ClubDailySummary?)null);

    // Act
    var result = await _sut.Handle(new GetClubDailySummaryQuery(clubId, date), CancellationToken.None);

    // Assert
    result.Should().Be("No daily summary available.");
  }

  [Fact]
  public async Task Handle_WhenDateNotProvided_UsesLatestSummary()
  {
    // Arrange
    const int clubId = 12;
    var summary = new ClubDailySummary
    {
      ClubId = clubId,
      Date = new DateOnly(2026, 3, 20),
      Summary = "Most current summary."
    };
    _clubRepository.GetDailySummaryAsync(clubId, null, Arg.Any<CancellationToken>())
      .Returns(summary);

    // Act
    var result = await _sut.Handle(new GetClubDailySummaryQuery(clubId), CancellationToken.None);

    // Assert
    result.Should().Be(summary.ToString());
  }
}
