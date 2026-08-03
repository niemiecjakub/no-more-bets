using MediatR;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Leagues.GetSeasonYearsList;

public record SeasonYearDto(string Year);

public record GetSeasonYearsListQuery : IRequest<IReadOnlyList<SeasonYearDto>>;

public sealed class GetSeasonYearsListHandler(IUnitOfWork unitOfWork)
  : IRequestHandler<GetSeasonYearsListQuery, IReadOnlyList<SeasonYearDto>>
{
  public async Task<IReadOnlyList<SeasonYearDto>> Handle(
    GetSeasonYearsListQuery request,
    CancellationToken cancellationToken)
  {
    var years = await unitOfWork.Leagues
      .GetSeasonYearsOrderedLatestFirstAsync(cancellationToken)
      .ConfigureAwait(false);

    return years.Select(y => new SeasonYearDto(y)).ToList();
  }
}
