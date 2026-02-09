using MediatR;
using NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails.Dtos;

namespace NoMoreBets.Features.Fotmob.GetFotmobCoreMatchDetails;

/// <summary>
/// Query to fetch core match details (goal-format per-team stats) from a FotMob match for a given team.
/// </summary>
/// <param name="GameUrl">FotMob match page URL.</param>
/// <param name="TeamName">Team name (e.g. "Paris Saint-Germain") to get stats for.</param>
public record GetFotmobCoreMatchDetailsQuery(string GameUrl, string TeamName) : IRequest<GoalTeamMatchData?>;
