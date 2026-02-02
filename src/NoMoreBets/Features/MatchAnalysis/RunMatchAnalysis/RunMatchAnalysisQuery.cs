using MediatR;
using NoMoreBets.Features.MatchAnalysis.Model;

namespace NoMoreBets.Features.MatchAnalysis.RunMatchAnalysis;

/// <summary>
/// Query to run full match analysis for upcoming games (Betclic) with data from Rotowire, SoccerData, FotMob.
/// </summary>
/// <param name="LeagueId">Optional league ID for SoccerData upcoming previews; if null, uses options default.</param>
public record RunMatchAnalysisQuery(int? LeagueId = null) : IRequest<IReadOnlyList<Model.MatchAnalysis>>;
