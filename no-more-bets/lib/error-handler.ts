import axios from "axios";

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
      errorMessage = "Something went wrong. Please try again in a moment.";
    } else {
      errorMessage = error.message;
    }
  } else if (error instanceof Error) {
    errorMessage = error.message;
  }

  return errorMessage;
}
