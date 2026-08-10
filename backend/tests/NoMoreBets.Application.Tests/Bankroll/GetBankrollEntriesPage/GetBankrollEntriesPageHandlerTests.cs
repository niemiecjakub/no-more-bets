using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Bankroll.GetBankrollEntriesPage;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Bankroll.GetBankrollEntriesPage;

public class GetBankrollEntriesPageHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IBankrollRepository _bankrollRepository = Substitute.For<IBankrollRepository>();
  private readonly GetBankrollEntriesPageHandler _sut;

  public GetBankrollEntriesPageHandlerTests()
  {
    _unitOfWork.Bankroll.Returns(_bankrollRepository);
    _sut = new GetBankrollEntriesPageHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_PassesEntryNamesToRepository_WhenFilterProvided()
  {
    var createdAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
    var page = new BankrollPage(
      new[]
      {
        new BankrollEntryRow(1, BankrollEntryNames.BetWin, 100m, BankrollFlowExtensions.InCode, createdAt, 9),
      },
      false);
    IReadOnlyCollection<string> entryNames = new[] { BankrollEntryNames.BetWin };
    _bankrollRepository
      .GetEntriesPageAsync(15, null, null, entryNames, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
      .Returns(page);

    var result = await _sut.Handle(
      new GetBankrollEntriesPageQuery(15, null, null, entryNames),
      CancellationToken.None);

    result.Items.Should().HaveCount(1);
    result.Items[0].Name.Should().Be(BankrollEntryNames.BetWin);
    result.HasMore.Should().BeFalse();
    await _bankrollRepository.Received(1).GetEntriesPageAsync(
      15,
      null,
      null,
      entryNames,
      Arg.Any<IReadOnlyList<string>?>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_PassesNullEntryNames_WhenNoFilterProvided()
  {
    var page = new BankrollPage(Array.Empty<BankrollEntryRow>(), false);
    _bankrollRepository
      .GetEntriesPageAsync(15, null, null, null, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
      .Returns(page);

    await _sut.Handle(new GetBankrollEntriesPageQuery(15, null, null), CancellationToken.None);

    await _bankrollRepository.Received(1).GetEntriesPageAsync(
      15,
      null,
      null,
      null,
      Arg.Any<IReadOnlyList<string>?>(),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_PassesSeasonYearsToRepository_WhenFilterProvided()
  {
    var page = new BankrollPage(Array.Empty<BankrollEntryRow>(), false);
    IReadOnlyList<string> seasonYears = new[] { "2025/2026" };
    _bankrollRepository
      .GetEntriesPageAsync(
        Arg.Any<int>(),
        Arg.Any<DateTime?>(),
        Arg.Any<int?>(),
        Arg.Any<IReadOnlyCollection<string>?>(),
        Arg.Any<IReadOnlyList<string>?>(),
        Arg.Any<CancellationToken>())
      .Returns(page);

    await _sut.Handle(
      new GetBankrollEntriesPageQuery(15, null, null, null, seasonYears),
      CancellationToken.None);

    await _bankrollRepository.Received(1).GetEntriesPageAsync(
      Arg.Any<int>(),
      Arg.Any<DateTime?>(),
      Arg.Any<int?>(),
      Arg.Is<IReadOnlyCollection<string>?>(names => names == null),
      Arg.Is<IReadOnlyList<string>?>(years => years != null && years.SequenceEqual(seasonYears)),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_ReturnsHasMore_WhenRepositoryIndicatesMoreFilteredPages()
  {
    var createdAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);
    var page = new BankrollPage(
      new[]
      {
        new BankrollEntryRow(2, BankrollEntryNames.BetStake, 50m, BankrollFlowExtensions.OutCode, createdAt, 8),
      },
      true);
    IReadOnlyCollection<string> entryNames = new[] { BankrollEntryNames.BetStake };
    _bankrollRepository
      .GetEntriesPageAsync(1, null, null, entryNames, Arg.Any<IReadOnlyList<string>?>(), Arg.Any<CancellationToken>())
      .Returns(page);

    var result = await _sut.Handle(
      new GetBankrollEntriesPageQuery(1, null, null, entryNames),
      CancellationToken.None);

    result.HasMore.Should().BeTrue();
    result.NextCursorAt.Should().Be(createdAt);
    result.NextCursorId.Should().Be(2);
  }
}
