
export function getApiBase(): string {
  const base = (process.env.NEXT_PUBLIC_API_URL ?? "").trim().replace(/\/$/, "");
  if (!base) {
    throw new Error("NEXT_PUBLIC_API_URL is not set");
  }
  return base;
}


export interface ApiGetOptions {
  revalidate?: number;
}

/**
 * GET request to the backend API. Handles base URL, fetch, errors, and JSON parse.
 * @param path - Path starting with / (e.g. /api/Database/matches)
 * @param resourceName - Name for error messages (e.g. "matches")
 * @param options - Optional revalidate for Next.js fetch cache (default 60)
 */
export async function apiGet<T>(
  path: string,
  resourceName: string,
  options?: ApiGetOptions
): Promise<T> {
  const base = getApiBase();
  const url = `${base}${path.startsWith("/") ? path : `/${path}`}`;
  let res: Response;
  try {
    res = await fetch(url, {
      next: { revalidate: options?.revalidate ?? 60 },
    });
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    const cause = err instanceof Error && err.cause instanceof Error ? err.cause.message : "";
    throw new Error(`Request to backend failed: ${msg}${cause ? `. ${cause}` : ""}.`);
  }
  if (!res.ok) {
    throw new Error(`Failed to fetch ${resourceName}: ${res.status} ${res.statusText}`);
  }
  return res.json();
}
