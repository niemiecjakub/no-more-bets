/**
 * Formats an ISO date string for match display (date + time) in the user's local timezone.
 */
export function formatMatchDate(isoDateString: string): string {
  const date = new Date(isoDateString);
  return new Intl.DateTimeFormat("en-GB", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}

/**
 * Time only (HH:MM) in the user's local timezone.
 */
export function formatMatchTime(isoDateString: string): string {
  const date = new Date(isoDateString);
  return new Intl.DateTimeFormat("en-GB", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}

/**
 * Returns the user's local timezone label (e.g. "GMT+2" or "CEST").
 */
export function getLocalTimeZoneLabel(): string {
  try {
    const parts = new Intl.DateTimeFormat(undefined, {
      timeZoneName: "short",
    }).formatToParts(new Date());
    const tzPart = parts.find((part) => part.type === "timeZoneName");
    return tzPart?.value ?? "local time";
  } catch {
    return "local time";
  }
}
