using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.AgentDashboard.GetAgentDashboardBankroll;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using MediatR;

namespace NoMoreBets.Application.Tests.AgentDashboard.GetAgentDashboardBankroll;

public class GetAgentDashboardBankrollHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBankrollRepository _bankrollRepository = Substitute.For<IBankrollRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly GetAgentDashboardBankrollHandler _sut;

  public GetAgentDashboardBankrollHandlerTests()
  {
    _unitOfWork.Bankroll.Returns(_bankrollRepository);
    _sut = new GetAgentDashboardBankrollHandler(_unitOfWork, _mediator);
  }

  [Fact]
  public async Task Handle_ForwardsSeasonYearsToBettingBalance()
  {
    // Arrange
    var seasonYears = new[] { "2025/2026" };
    _bankrollRepository.GetTotalValueAsync(Arg.Any<CancellationToken>()).Returns(1000m);
    _bankrollRepository
      .GetBettingBalanceAsync(Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
      .Returns(-50m);
    _mediator.Send(Arg.Any<GetDaysUntilPaydayQuery>(), Arg.Any<CancellationToken>()).Returns(3);

    // Act
    var result = await _sut.Handle(
      new GetAgentDashboardBankrollQuery(seasonYears),
      CancellationToken.None);

    // Assert
    result.TotalValue.Should().Be(1000m);
    result.Balance.Should().Be(-50m);
    result.DaysUntilPayday.Should().Be(3);
    await _bankrollRepository.Received(1).GetBettingBalanceAsync(
      Arg.Is<IReadOnlyList<string>?>(years => years != null && years.SequenceEqual(seasonYears)),
      Arg.Any<CancellationToken>());
  }
}
