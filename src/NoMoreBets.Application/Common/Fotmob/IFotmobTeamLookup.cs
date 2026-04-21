namespace NoMoreBets.Application.Common.Fotmob;

/// <summary>Resolves FotMob team ids from in-app club display names (see FotMob constants mapping).</summary>
public interface IFotmobTeamLookup
{
  /// <summary>Returns FotMob team id when the club name matches a known team, otherwise null.</summary>
  int? TryResolveFotmobTeamId(string clubName);
}
