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
  public async Task GetMemoryFilenamesAsync_ReturnsNamesFromRepository()
  {
    // Arrange
    _memories.GetNamesAsync(Arg.Any<CancellationToken>())
      .Returns(new[] { "a.md", "b.md" });

    // Act
    var result = await _sut.GetMemoryFilenamesAsync(CancellationToken.None);

    // Assert
    result.Should().Equal("a.md", "b.md");
  }

  [Fact]
  public async Task ReadAsync_WhenMissing_ThrowsKeyNotFoundException()
  {
    // Arrange
    _memories.GetByNameAsync("NOTE.md", Arg.Any<CancellationToken>())
      .Returns((Memory?)null);

    // Act
    var act = async () => await _sut.ReadAsync("NOTE.md", CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*NOTE.md*");
  }
}
