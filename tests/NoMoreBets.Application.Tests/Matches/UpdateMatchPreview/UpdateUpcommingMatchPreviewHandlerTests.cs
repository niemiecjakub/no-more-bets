using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Common.Dto.Matches;
using NoMoreBets.Application.Matches;
using NoMoreBets.Application.Matches.UpdateMatchPreview;
using NoMoreBets.Domain.Matches;
using DomainMatch = NoMoreBets.Domain.Matches.Match;

namespace NoMoreBets.Application.Tests.Matches.UpdateMatchPreview;

public class UpdateUpcommingMatchPreviewHandlerTests
{
  private readonly IMatchPreviewProvider _matchPreviewProvider;
  private readonly IUnitOfWork _unitOfWork;
  private readonly ILogger<UpdateUpcommingMatchPreviewHandler> _logger;
  private readonly UpdateUpcommingMatchPreviewHandler _sut;

  public UpdateUpcommingMatchPreviewHandlerTests()
  {
    _matchPreviewProvider = Substitute.For<IMatchPreviewProvider>();
    _unitOfWork = Substitute.For<IUnitOfWork>();
    _logger = Substitute.For<ILogger<UpdateUpcommingMatchPreviewHandler>>();
    _sut = new UpdateUpcommingMatchPreviewHandler(_matchPreviewProvider, _unitOfWork, _logger);
  }

  private static MatchPreviewDto CreatePreviewDto(IReadOnlyList<PreviewContentItem>? content = null) =>
    new()
    {
      Id = 1,
      Date = "",
      Time = "",
      Country = new CountryInfo { Id = 0, Name = "" },
      League = new LeagueInfo { Id = 0, Name = "" },
      Stage = new StageInfo { Id = 0, Name = "", IsActive = false },
      Teams = new Teams { Home = new TeamInfo { Id = 0, Name = "" }, Away = new TeamInfo { Id = 0, Name = "" } },
      MatchData = new MatchData
      {
        Weather = new Weather { TempF = 0, TempC = 0, Description = "" },
        ExcitementRating = 0,
        Prediction = new Prediction { Type = "", Choice = "" }
      },
      PreviewContent = content ?? []
    };

  [Fact]
  public async Task Handle_WhenMatchNotFoundInDb_ReturnsWithoutSaving()
  {
    _matchPreviewProvider.GetMatchPreviewAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(CreatePreviewDto());
    _unitOfWork.Matches.GetMatchBySoccerdataId(Arg.Any<int>()).Returns(Task.FromResult<DomainMatch?>(null));

    var result = await _sut.Handle(new UpdateUpcommingMatchPreviewCommand(42), CancellationToken.None);

    result.Should().Be(Unit.Value);
    await _unitOfWork.Matches.DidNotReceive().AddMatchPreview(Arg.Any<MatchPreview>());
    await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenMatchFound_NoExistingPreview_AddsPreviewAndSaveChanges()
  {
    _matchPreviewProvider.GetMatchPreviewAsync(42, Arg.Any<CancellationToken>()).Returns(CreatePreviewDto());
    var match = new DomainMatch { Id = 10, SoccerdataId = 42 };
    _unitOfWork.Matches.GetMatchBySoccerdataId(42).Returns(match);
    _unitOfWork.Matches.GetMatchPreview(10).Returns((MatchPreview?)null);

    await _sut.Handle(new UpdateUpcommingMatchPreviewCommand(42), CancellationToken.None);

    await _unitOfWork.Matches.Received(1).AddMatchPreview(Arg.Is<MatchPreview>(p => p.MatchId == 10 && !string.IsNullOrEmpty(p.PreviewContentJson)));
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task Handle_WhenMatchFound_ExistingPreview_UpdatesJsonAndSaveChanges()
  {
    _matchPreviewProvider.GetMatchPreviewAsync(42, Arg.Any<CancellationToken>()).Returns(CreatePreviewDto(new[] { new PreviewContentItem { Name = "Form", Content = "New" } }));
    var match = new DomainMatch { Id = 10, SoccerdataId = 42 };
    _unitOfWork.Matches.GetMatchBySoccerdataId(42).Returns(match);
    var existing = new MatchPreview { MatchId = 10, PreviewContentJson = "old" };
    _unitOfWork.Matches.GetMatchPreview(10).Returns(existing);

    await _sut.Handle(new UpdateUpcommingMatchPreviewCommand(42), CancellationToken.None);

    existing.PreviewContentJson.Should().NotBe("old");
    await _unitOfWork.Matches.DidNotReceive().AddMatchPreview(Arg.Any<MatchPreview>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
  }
}
