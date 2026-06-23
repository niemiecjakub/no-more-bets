import type { MatchEventDto } from "../interfaces";

/** Scoring events shown in the match header timeline. */
const GOAL_EVENT_TYPES = new Set(["Goal", "OwnGoal", "PenaltyGoal"]);

export function partitionMatchEventsBySide(
  events: MatchEventDto[],
  homeClubId: number,
  awayClubId: number,
): { home: MatchEventDto[]; away: MatchEventDto[] } {
  const home: MatchEventDto[] = [];
  const away: MatchEventDto[] = [];

  for (const event of events) {
    if (event.eventType === "OwnGoal") {
      if (event.clubId === homeClubId) {
        away.push(event);
      } else if (event.clubId === awayClubId) {
        home.push(event);
      }
    } else if (event.clubId === homeClubId) {
      home.push(event);
    } else if (event.clubId === awayClubId) {
      away.push(event);
    }
  }

  return { home, away };
}

interface MatchClubEventsListProps {
  events: MatchEventDto[];
  isLoading: boolean;
  error?: string;
  align: "start" | "end";
}

export function MatchClubEventsList({ events, isLoading, error, align }: MatchClubEventsListProps) {
  const alignClass = align === "end" ? "items-end text-end" : "items-start text-start";
  const goalEvents = events.filter((e) => GOAL_EVENT_TYPES.has(e.eventType));

  if (error) {
    return <p className={`w-full text-xs text-red-800 dark:text-red-200 ${alignClass}`}>{error}</p>;
  }

  if (isLoading) {
    return (
      <ul className={`flex w-full min-w-0 flex-col gap-1 ${alignClass}`} aria-hidden>
        {[1, 2].map((i) => (
          <li key={i}>
            <div
              className={`h-3 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800 ${i === 1 ? "w-24" : "w-20"}`}
            />
          </li>
        ))}
      </ul>
    );
  }

  if (goalEvents.length === 0) {
    return null;
  }

  return (
    <ul className={`flex w-full min-w-0 flex-col gap-0.5 ${alignClass}`}>
      {goalEvents.map((event, index) => (
        <li
          key={`${event.minute}-${event.eventTypeId}-${event.playerName}-${index}`}
          className="max-w-full truncate text-xs text-zinc-500 dark:text-zinc-400"
        >
          <span className="text-zinc-600 dark:text-zinc-300">{event.playerName}</span>
          {event.eventType === "OwnGoal" ? (
            <>
              {" "}
              <span className="text-zinc-500 dark:text-zinc-400">(OG)</span>
            </>
          ) : null}{" "}
          <span className="tabular-nums">{event.minute}&apos;</span>
        </li>
      ))}
    </ul>
  );
}
