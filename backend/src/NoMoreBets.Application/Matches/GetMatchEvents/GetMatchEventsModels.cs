namespace NoMoreBets.Application.Matches.GetMatchEvents;

public record MatchEventDto(
  string PlayerName,
  int ClubId,
  int EventTypeId,
  string EventType,
  int Minute);
