using FluentAssertions;
using Microsoft.Extensions.AI;
using NSubstitute;
using NoMoreBets.Application.SocialMedia;
using NoMoreBets.Application.SocialMedia.CreateXPost;
using NoMoreBets.Infrastructure.AI.Common;
using NoMoreBets.Infrastructure.AI.Plugins;

namespace NoMoreBets.Infrastructure.Tests.AI.Plugins;

public class SocialMediaPluginTests
{
  private readonly IXApiService _xApiService = Substitute.For<IXApiService>();
  private readonly SocialMediaPlugin _sut;

  public SocialMediaPluginTests()
  {
    _sut = new SocialMediaPlugin(_xApiService);
  }

  [Fact]
  public async Task CreateXPostAsync_DelegatesToXApiService()
  {
    _xApiService
      .CreateXPostAsync(Arg.Any<CreateXPostRequest>(), Arg.Any<CancellationToken>())
      .Returns(new CreateXPostResult { Id = "123", Text = "hello" });

    var result = await _sut.CreateXPostAsync("  hello  ", CancellationToken.None);

    result.Id.Should().Be("123");
    result.Text.Should().Be("hello");
    await _xApiService.Received(1).CreateXPostAsync(
      Arg.Is<CreateXPostRequest>(r => r.Text == "  hello  "),
      Arg.Any<CancellationToken>());
  }

  [Fact]
  public void CreateXPost_RegistersNamedFunction()
  {
    var tool = AgentAiTools.Create(_sut.CreateXPostAsync, "CreateXPost");

    tool.Should().BeAssignableTo<AIFunction>();
    ((AIFunction)tool).Name.Should().Be("CreateXPost");
  }
}
