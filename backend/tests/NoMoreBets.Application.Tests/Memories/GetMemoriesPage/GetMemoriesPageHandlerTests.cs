using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Memories.GetMemoriesPage;
using NoMoreBets.Domain.Memories;

namespace NoMoreBets.Application.Tests.Memories.GetMemoriesPage;

public class GetMemoriesPageHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMemoryRepository _memoryRepository = Substitute.For<IMemoryRepository>();
  private readonly GetMemoriesPageHandler _sut;

  public GetMemoriesPageHandlerTests()
  {
    _unitOfWork.Memories.Returns(_memoryRepository);
    _sut = new GetMemoriesPageHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_PassesLimitAndCursorToRepository()
  {
    var cursorAt = new DateTime(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);
    _memoryRepository
      .GetPageAsync(20, cursorAt, 7, Arg.Any<CancellationToken>())
      .Returns(new MemoryPage(Array.Empty<MemoryListItem>(), false));

    await _sut.Handle(new GetMemoriesPageQuery(20, cursorAt, 7), CancellationToken.None);

    await _memoryRepository.Received(1).GetPageAsync(20, cursorAt, 7, Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenHasMore_SetsNextCursorFromLastItem()
  {
    var items = new List<MemoryListItem>
    {
      new(2, "b", null, "content-b", new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc)),
      new(1, "a", "desc", "content-a", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)),
    };
    _memoryRepository
      .GetPageAsync(Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
      .Returns(new MemoryPage(items, true));

    var result = await _sut.Handle(new GetMemoriesPageQuery(15, null, null), CancellationToken.None);

    result.HasMore.Should().BeTrue();
    result.Items.Should().HaveCount(2);
    result.NextCursorAt.Should().Be(items[^1].UpdatedAt);
    result.NextCursorId.Should().Be(items[^1].Id);
  }

  [Fact]
  public async Task Handle_WhenNoMore_SetsNextCursorToNull()
  {
    var items = new List<MemoryListItem>
    {
      new(1, "a", null, "content", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc)),
    };
    _memoryRepository
      .GetPageAsync(Arg.Any<int>(), Arg.Any<DateTime?>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
      .Returns(new MemoryPage(items, false));

    var result = await _sut.Handle(new GetMemoriesPageQuery(15, null, null), CancellationToken.None);

    result.HasMore.Should().BeFalse();
    result.NextCursorAt.Should().BeNull();
    result.NextCursorId.Should().BeNull();
  }
}
