using NoMoreBets.Application.SocialMedia.CreateXPost;

namespace NoMoreBets.Application.SocialMedia;

public interface IXApiService
{
  Task<CreateXPostResult> CreateXPostAsync(CreateXPostRequest request, CancellationToken cancellationToken = default);
}
