import axiosInstance from "@/lib/axios";
import type { SubmitFeedbackPayload, SubmitFeedbackResponse } from "../interfaces";

/**
 * Submits anonymous user feedback to the backend.
 */
export async function submitFeedback(
  payload: SubmitFeedbackPayload,
): Promise<SubmitFeedbackResponse> {
  const { data } = await axiosInstance.post<SubmitFeedbackResponse>(
    "/api/feedback",
    payload,
  );
  return data;
}
