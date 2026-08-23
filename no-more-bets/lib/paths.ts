function dateStamp(isoDate: string): string {
  const stamp = isoDate.slice(0, 10);
  return /^\d{4}-\d{2}-\d{2}$/.test(stamp) ? stamp : isoDate.slice(0, 10);
}

export function matchSlug(input: {
  id: number;
  homeClubSlug: string;
  awayClubSlug: string;
  matchDate: string;
}): string {
  const home = (input.homeClubSlug || "home").trim().toLowerCase();
  const away = (input.awayClubSlug || "away").trim().toLowerCase();
  return `${home}-vs-${away}-${dateStamp(input.matchDate)}-${input.id}`;
}

export function matchPath(input: {
  id?: number;
  matchId?: number;
  homeClubSlug?: string | null;
  awayClubSlug?: string | null;
  matchDate?: string | null;
}): string {
  const id = input.id ?? input.matchId;
  if (id == null || id < 1) return "/";
  if (input.homeClubSlug && input.awayClubSlug && input.matchDate) {
    return `/match/${matchSlug({
      id,
      homeClubSlug: input.homeClubSlug,
      awayClubSlug: input.awayClubSlug,
      matchDate: input.matchDate,
    })}`;
  }
  return `/match/${id}`;
}

export function parseMatchParam(param: string): number | null {
  const trimmed = param.trim();
  if (trimmed === "") return null;
  const token = trimmed.includes("-") ? trimmed.slice(trimmed.lastIndexOf("-") + 1) : trimmed;
  if (!/^\d+$/.test(token)) return null;
  const id = Number(token);
  return id >= 1 ? id : null;
}

export function isBareNumericParam(param: string): boolean {
  return /^\d+$/.test(param.trim());
}

export function clubPath(slug: string): string {
  return `/club/${slug}`;
}

export function leaguePath(slug: string): string {
  return `/leagues/${slug}`;
}
