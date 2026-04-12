using MediatR;
using NoMoreBets.Application.SocialMedia;

namespace NoMoreBets.Application.SocialMedia.CreateXPost;

public record CreateXPostCommand(CreateXPostRequest Request) : IRequest<CreateXPostResult>;

public sealed class CreateXPostHandler(IXApiService xApiService) : IRequestHandler<CreateXPostCommand, CreateXPostResult>
{
  public Task<CreateXPostResult> Handle(CreateXPostCommand request, CancellationToken cancellationToken)
  {
    return xApiService.CreateXPostAsync(request.Request, cancellationToken);
  }
}
