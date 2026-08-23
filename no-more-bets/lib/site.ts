export const SITE_NAME = "No More Bets";
export const SITE_TAGLINE = "AI football research agent";
export const DEFAULT_TITLE = "No More Bets — AI football research agent";
export const DEFAULT_DESCRIPTION =
  "Public AI agent that researches top-league fixtures, publishes briefs, and bets its own bankroll.";

export function getSiteUrl(): string {
  const raw = process.env.SITE_URL ?? "https://nomorebets.io";
  return raw.replace(/\/$/, "");
}

export function getApiBaseUrl(): string {
  const raw = process.env.API_URL ?? process.env.NEXT_PUBLIC_API_URL ?? "";
  return raw.replace(/\/$/, "");
}

export function absoluteUrl(path: string): string {
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${getSiteUrl()}${normalized}`;
}
