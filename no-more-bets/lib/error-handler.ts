import axios, { AxiosError } from "axios";

function getRequestUrl(error: AxiosError): string | null {
  const base = error.config?.baseURL;
  const url = error.config?.url;
  if (base && url) return `${base}${url.startsWith("/") ? url : `/${url}`}`;
  return base ?? error.config?.url ?? null;
}

export function handleServiceError(
  error: unknown,
  customMessage?: string
): string {
  let errorMessage = customMessage ?? "An unexpected error occurred";

  if (axios.isAxiosError(error)) {
    if (error.response) {
      errorMessage =
        error.response.data?.error ??
        error.response.data?.message ??
        `Error: ${error.response.status}`;
    } else if (error.request) {
      const requestUrl = getRequestUrl(error);
      errorMessage =
        "No response from server. Check that NEXT_PUBLIC_API_URL in .env.local points to your API, the API is running, and CORS allows this origin. " +
        (requestUrl ? `Requested: ${requestUrl}` : "");
    } else {
      errorMessage = error.message;
    }
  } else if (error instanceof Error) {
    errorMessage = error.message;
  }

  return errorMessage;
}
