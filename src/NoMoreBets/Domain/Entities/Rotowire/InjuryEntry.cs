namespace NoMoreBets.Domain.Entities.Rotowire;

/// <summary>
/// Represents an injury entry for a player (e.g. QUES, OUT, SUS).
/// </summary>
public record InjuryEntry(string Position, string Player, string Status) : PlayerInLineup(Position, Player);
