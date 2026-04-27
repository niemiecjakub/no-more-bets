/**
 * Public assets: `/clubs/{segment}.svg` then `.png` (see SlugIcon). Normalizes API slug and
 * falls back to a kebab-case guess from the club name when slug is missing.
 */
export function clubLogoSlugSegment(
  slug: string | undefined | null,
  clubName: string
): string {
  const raw = slug?.trim();
  if (raw) return raw.toLowerCase();
  return clubName
    .toLowerCase()
    .replace(/[''`]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}
