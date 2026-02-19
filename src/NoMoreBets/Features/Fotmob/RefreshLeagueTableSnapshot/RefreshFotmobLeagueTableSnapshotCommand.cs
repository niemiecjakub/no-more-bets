using MediatR;

namespace NoMoreBets.Features.Fotmob.RefreshLeagueTableSnapshot;

/// <summary>Command to refresh league table snapshot from FotMob (scrape table + xG, merge, persist). Always updates the latest season (max id) for the given league.</summary>
public record RefreshFotmobLeagueTableSnapshotCommand(int LeagueId) : IRequest<Unit>;
