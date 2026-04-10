using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Bankroll.GetBankrollDashboard;
using NoMoreBets.Application.Bankroll.GetDaysUntilPayday;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Enums;
using DomainBankroll = NoMoreBets.Domain.Bankrolls.Bankroll;

namespace NoMoreBets.Application.Tests.Bankroll.GetBankrollDashboard;

public class GetBankrollDashboardHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBankrollRepository _bankrollRepository = Substitute.For<IBankrollRepository>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly GetBankrollDashboardHandler _sut;

  public GetBankrollDashboardHandlerTests()
  {
    _unitOfWork.Bankroll.Returns(_bankrollRepository);
    _sut = new GetBankrollDashboardHandler(_unitOfWork, _mediator);
  }

  [Fact]
  public async Task Handle_ReturnsBalanceDaysAndRecords_PreservingRepositoryOrder()
  {
    var inEntry = DomainBankroll.Create("Salary", 100m, BankrollFlow.In);
    var outEntry = DomainBankroll.Create("Bet stake", 25m, BankrollFlow.Out, betId: 7);
    _bankrollRepository.GetCurrentBalanceAsync(Arg.Any<CancellationToken>()).Returns(75m);
    IReadOnlyList<DomainBankroll> list = new List<DomainBankroll> { outEntry, inEntry };
    _bankrollRepository.GetAllOrderedByCreatedAtDescAsync(Arg.Any<CancellationToken>())
      .Returns(Task.FromResult(list));
    _mediator.Send(Arg.Any<GetDaysUntilPaydayQuery>(), Arg.Any<CancellationToken>()).Returns(3);

    var result = await _sut.Handle(new GetBankrollDashboardQuery(), CancellationToken.None);

    result.CurrentBalance.Should().Be(75m);
    result.DaysUntilPayday.Should().Be(3);
    result.Records.Should().HaveCount(2);
    result.Records[0].Name.Should().Be("Bet stake");
    result.Records[0].Flow.Should().Be(nameof(BankrollFlow.Out));
    result.Records[0].BetId.Should().Be(7);
    result.Records[1].Flow.Should().Be(nameof(BankrollFlow.In));
    await _mediator.Received(1).Send(Arg.Any<GetDaysUntilPaydayQuery>(), Arg.Any<CancellationToken>());
  }
}
