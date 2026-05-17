using FluentAssertions;
using MediatR;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Search;
using NoMoreBets.Domain.Memories;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class AgentMemoryMaintenancePluginTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMediator _mediator = Substitute.For<IMediator>();
  private readonly IMemoryRepository _memories = Substitute.For<IMemoryRepository>();
  private readonly ISearchService _searchService = Substitute.For<ISearchService>();
  private readonly MemoriesPlugin _memoriesPlugin;
  private readonly InternetSearchPlugin _searchPlugin;
  private readonly BankrollPlugin _bankrollPlugin;
  private readonly AgentMemoryMaintenancePlugin _sut;

  public AgentMemoryMaintenancePluginTests()
  {
    _unitOfWork.Memories.Returns(_memories);
    _memoriesPlugin = new MemoriesPlugin(_unitOfWork);
    _searchPlugin = new InternetSearchPlugin(_searchService);
    _bankrollPlugin = new BankrollPlugin(_unitOfWork, _mediator);
    _sut = new AgentMemoryMaintenancePlugin(_memoriesPlugin, _searchPlugin, _bankrollPlugin, _unitOfWork);
  }

  [Fact]
  public async Task DeleteMemoryAsync_WhenPresent_RemovesAndSaves()
  {
    _memories.SoftDeleteByNameAsync("EPHEMERAL_SCRATCH", Arg.Any<CancellationToken>()).Returns(true);
    _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

    var message = await _sut.DeleteMemoryAsync("EPHEMERAL_SCRATCH", CancellationToken.None);

    message.Should().Be("*Memory record deleted*");
    await _memories.Received(1).SoftDeleteByNameAsync("EPHEMERAL_SCRATCH", Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task DeleteMemoryAsync_WhenMissing_ThrowsKeyNotFoundException()
  {
    _memories.SoftDeleteByNameAsync("NO_SUCH_MEMORY", Arg.Any<CancellationToken>()).Returns(false);

    var act = async () => await _sut.DeleteMemoryAsync("NO_SUCH_MEMORY", CancellationToken.None);

    await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*NO_SUCH_MEMORY*");
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
