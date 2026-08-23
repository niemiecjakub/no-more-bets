import { getApiBaseUrl } from "@/lib/site";

type FetchOptions = {
  revalidate?: number | false;
};

async function apiFetch(path: string, options: FetchOptions = {}): Promise<Response> {
  const base = getApiBaseUrl();
  if (!base) {
    throw new Error("API_URL / NEXT_PUBLIC_API_URL is not configured");
  }

  const init: RequestInit = {
    headers: { Accept: "application/json" },
  };

  if (options.revalidate === false) {
    init.cache = "no-store";
  } else {
    init.next = { revalidate: options.revalidate ?? 60 };
  }

  return fetch(`${base}${path}`, init);
}

export async function apiGetJson<T>(path: string, options: FetchOptions = {}): Promise<T | null> {
  const response = await apiFetch(path, options);
  if (response.status === 404 || response.status === 204) return null;
  if (!response.ok) {
    throw new Error(`API ${response.status} for ${path}`);
  }
  return (await response.json()) as T;
}
