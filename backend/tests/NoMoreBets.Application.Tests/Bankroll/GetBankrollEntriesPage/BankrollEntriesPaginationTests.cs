using FluentAssertions;
using NoMoreBets.Application.Bankroll.GetBankrollEntriesPage;
using NoMoreBets.Domain.Bankrolls;
using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Application.Tests.Bankroll.GetBankrollEntriesPage;

public class BankrollEntriesPaginationTests
{
  [Fact]
  public void MapRows_MapsFlowAndDelta_ForInAndOutEntries()
  {
    // Arrange
    var createdAt = new DateTime(2026, 5, 16, 12, 0, 0, DateTimeKind.Utc);
    var rows = new[]
    {
      new BankrollEntryRow(3, "Bet win", 100m, BankrollFlowExtensions.InCode, createdAt.AddHours(2), 9),
      new BankrollEntryRow(2, "Bet stake", 50m, BankrollFlowExtensions.OutCode, createdAt.AddHours(1), 8),
    };

    // Act
    var items = BankrollEntriesPagination.MapRows(rows);

    // Assert
    items.Should().HaveCount(2);
    items[0].Flow.Should().Be(nameof(BankrollFlow.In));
    items[0].Delta.Should().Be(100m);
    items[1].Flow.Should().Be(nameof(BankrollFlow.Out));
    items[1].Delta.Should().Be(-50m);
  }

  [Fact]
  public void MapRows_PreservesRowOrder()
  {
    // Arrange
    var createdAt = new DateTime(2026, 5, 16, 12, 0, 0, DateTimeKind.Utc);
    var rows = new[]
    {
      new BankrollEntryRow(10, "Later id", 10m, BankrollFlowExtensions.InCode, createdAt, null),
      new BankrollEntryRow(9, "Earlier id", 5m, BankrollFlowExtensions.OutCode, createdAt, null),
    };

    // Act
    var items = BankrollEntriesPagination.MapRows(rows);

    // Assert
    items[0].Id.Should().Be(10);
    items[1].Id.Should().Be(9);
  }
}
