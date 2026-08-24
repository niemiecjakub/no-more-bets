/**
 * Parses an API ISO date string. Timezone-less datetimes are treated as UTC
 * (API contract: values are stored and returned as UTC).
 */
export function parseApiDate(isoDateString: string): Date {
  const trimmed = isoDateString.trim();
  // Already has Z or ±HH:MM / ±HHMM offset
  if (/[zZ]|[+-]\d{2}:?\d{2}$/.test(trimmed)) {
    return new Date(trimmed);
  }
  // Date-only (YYYY-MM-DD) — leave as-is; appending Z would shift the calendar day in western zones
  if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
    return new Date(trimmed);
  }
  // Datetime without offset — treat as UTC
  return new Date(`${trimmed}Z`);
}

/**
 * Formats an ISO date string for match display (date + time) in the user's local timezone.
 */
export function formatMatchDate(isoDateString: string): string {
  const date = parseApiDate(isoDateString);
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
  const date = parseApiDate(isoDateString);
  return new Intl.DateTimeFormat("en-GB", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  }).format(date);
}

/**
 * Medium date + short 24h time in the user's local timezone (session list/detail).
 */
export function formatLocalDateTime(isoDateString: string): string {
  try {
    return parseApiDate(isoDateString).toLocaleString(undefined, {
      dateStyle: "medium",
      timeStyle: "short",
      hour12: false,
    });
  } catch {
    return isoDateString;
  }
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
