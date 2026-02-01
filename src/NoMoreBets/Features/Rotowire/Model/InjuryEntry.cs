using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Features.Rotowire.Model;

/// <summary>
/// Represents an injury entry for a player (e.g. QUES, OUT, SUS).
/// </summary>
public record InjuryEntry(FootballPosition Position, string Player, InjuryStatus Status) : PlayerInLineup(Position, Player);
