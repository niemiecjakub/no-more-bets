using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Matches.GetMatchPreview;
using NoMoreBets.Domain.Matches;

namespace NoMoreBets.Application.Tests.Matches.GetMatchPreview;

public class GetMatchPreviewHandlerTests
{
  private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IMatchRepository _matches = Substitute.For<IMatchRepository>();
  private readonly GetMatchPreviewHandler _sut;

  public GetMatchPreviewHandlerTests()
  {
    _unitOfWork.Matches.Returns(_matches);
    _sut = new GetMatchPreviewHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_WhenPreviewNull_ReturnsFallbackMessage()
  {
    // Arrange
    _matches.GetMatchPreview(1).Returns((MatchPreview?)null);

    // Act
    var result = await _sut.Handle(new GetMatchPreviewQuery(1), CancellationToken.None);

    // Assert
    result.Should().Be("No preview available.");
  }

  [Fact]
  public async Task Handle_WhenPreviewExists_ReturnsMarkdownFromContent()
  {
    // Arrange
    var items = new[] { new PreviewContentItem { Name = "h1", Content = "Kickoff" } };
    var preview = new MatchPreview
    {
      MatchId = 2,
      PreviewContentJson = JsonSerializer.Serialize(items, JsonOpts)
    };
    _matches.GetMatchPreview(2).Returns(preview);

    // Act
    var result = await _sut.Handle(new GetMatchPreviewQuery(2), CancellationToken.None);

    // Assert
    result.Should().Contain("## Kickoff");
  }
}
