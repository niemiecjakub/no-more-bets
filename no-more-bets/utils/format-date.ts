/**
 * Formats an ISO date string for match display (date DD/MM/YYYY + time).
 * Uses a fixed locale and UTC so server and client render the same (avoids hydration mismatch).
 */
export function formatMatchDate(isoDateString: string): string {
  const date = new Date(isoDateString);
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "UTC",
  }).format(date);
}

/**
 * Time only (HH:MM), UTC — consistent with formatMatchDate for list rows.
 */
export function formatMatchTime(isoDateString: string): string {
  const date = new Date(isoDateString);
  return new Intl.DateTimeFormat("en-GB", {
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "UTC",
  }).format(date);
}
