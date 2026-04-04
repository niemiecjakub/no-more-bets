using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class BankrollPluginTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBankrollRepository _bankrolls = Substitute.For<IBankrollRepository>();
  private readonly BankrollPlugin _sut;

  public BankrollPluginTests()
  {
    _unitOfWork.Bankroll.Returns(_bankrolls);
    _sut = new BankrollPlugin(_unitOfWork);
  }

  [Fact]
  public async Task GetCurrentBalanceAsync_DelegatesToRepository()
  {
    // Arrange
    _bankrolls.GetCurrentBalanceAsync(Arg.Any<CancellationToken>())
      .Returns(123.45m);

    // Act
    var balance = await _sut.GetCurrentBalanceAsync(CancellationToken.None);

    // Assert
    balance.Should().Be(123.45m);
    await _bankrolls.Received(1).GetCurrentBalanceAsync(Arg.Any<CancellationToken>());
  }
}
