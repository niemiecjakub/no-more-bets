using NoMoreBets.Domain.Enums;

namespace NoMoreBets.Domain.Entities.Rotowire;

/// <summary>
/// Represents a player in a lineup (e.g. GK, DL, DC, DR, DMC, AML, AMC, AMR, FW).
/// </summary>
public record PlayerInLineup(FootballPosition Position, string Player);
