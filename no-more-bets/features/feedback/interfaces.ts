export interface SubmitFeedbackPayload {
  message: string;
  name?: string;
  email?: string;
}

export interface SubmitFeedbackResponse {
  id: number;
}

export const FEEDBACK_MAX_MESSAGE_LENGTH = 2000;
export const FEEDBACK_MAX_NAME_LENGTH = 200;
export const FEEDBACK_MAX_EMAIL_LENGTH = 320;
