using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Domain.Memories;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class MemoriesPluginTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMemoryRepository _memories = Substitute.For<IMemoryRepository>();
  private readonly MemoriesPlugin _sut;

  public MemoriesPluginTests()
  {
    _unitOfWork.Memories.Returns(_memories);
    _sut = new MemoriesPlugin(_unitOfWork);
  }

  [Fact]
  public async Task GetMemoryRecordsAsync_ReturnsRecordsFromRepository()
  {
    var t1 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    var t2 = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
    IReadOnlyList<MemoryRecordListItem> fromRepo =
    [
      new("STRATEGY", "First", t1),
      new("GENERAL_KNOWLEDGE", null, t2)
    ];
    _memories.GetRecordsAsync(Arg.Any<CancellationToken>()).Returns(fromRepo);

    var result = await _sut.GetMemoryRecordsAsync(CancellationToken.None);

    result.Should().HaveCount(2);
    result[0].Should().Be(new MemoryRecordListItem("STRATEGY", "First", t1));
    result[1].Should().Be(new MemoryRecordListItem("GENERAL_KNOWLEDGE", null, t2));
  }

  [Fact]
  public async Task ReadAsync_WhenMissing_ThrowsKeyNotFoundException()
  {
    // Arrange
    _memories.GetByNameAsync("REFLECTIONS", Arg.Any<CancellationToken>())
      .Returns((Memory?)null);

    // Act
    var act = async () => await _sut.ReadAsync("REFLECTIONS", CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*REFLECTIONS*");
  }
}
