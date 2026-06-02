using FluentAssertions;
using NSubstitute;
using NoMoreBets.Application.Common;
using NoMoreBets.Application.Feedback.SubmitFeedback;
using NoMoreBets.Domain.Feedback;
using DomainFeedback = NoMoreBets.Domain.Feedback.Feedback;

namespace NoMoreBets.Application.Tests.Feedback.SubmitFeedback;

public class SubmitFeedbackHandlerTests
{
  private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
  private readonly IFeedbackRepository _feedbackRepository = Substitute.For<IFeedbackRepository>();
  private readonly SubmitFeedbackHandler _sut;

  public SubmitFeedbackHandlerTests()
  {
    _unitOfWork.Feedback.Returns(_feedbackRepository);
    _sut = new SubmitFeedbackHandler(_unitOfWork);
  }

  [Fact]
  public async Task Handle_ValidInput_AddsFeedbackAndSaves()
  {
    DomainFeedback? captured = null;
    await _feedbackRepository
      .AddAsync(Arg.Do<DomainFeedback>(f => captured = f), Arg.Any<CancellationToken>());

    var act = () => _sut.Handle(
      new SubmitFeedbackCommand("  Great app  ", "  Alex  ", " alex@example.com "),
      CancellationToken.None);

    await act.Should().NotThrowAsync();
    await _feedbackRepository.Received(1).AddAsync(Arg.Any<DomainFeedback>(), Arg.Any<CancellationToken>());
    await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    captured.Should().NotBeNull();
    captured!.Message.Should().Be("Great app");
    captured.Name.Should().Be("Alex");
    captured.Email.Should().Be("alex@example.com");
  }

  [Fact]
  public async Task Handle_MessageOnly_OmitsOptionalFields()
  {
    DomainFeedback? captured = null;
    await _feedbackRepository
      .AddAsync(Arg.Do<DomainFeedback>(f => captured = f), Arg.Any<CancellationToken>());

    await _sut.Handle(new SubmitFeedbackCommand("Hello", null, "   "), CancellationToken.None);

    captured!.Name.Should().BeNull();
    captured.Email.Should().BeNull();
  }

  [Fact]
  public async Task Handle_EmptyMessage_ThrowsArgumentException()
  {
    var act = () => _sut.Handle(
      new SubmitFeedbackCommand("   ", null, null),
      CancellationToken.None);

    await act.Should().ThrowAsync<ArgumentException>();
    await _feedbackRepository.DidNotReceive().AddAsync(Arg.Any<DomainFeedback>(), Arg.Any<CancellationToken>());
  }
}
