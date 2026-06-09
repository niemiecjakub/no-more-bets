"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { Loader2, MessageSquare, Send } from "lucide-react";
import axios from "axios";
import { cn } from "@/lib/utils";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { Input } from "@/components/ui/input";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { Textarea } from "@/components/ui/textarea";
import {
  FEEDBACK_MAX_EMAIL_LENGTH,
  FEEDBACK_MAX_MESSAGE_LENGTH,
  FEEDBACK_MAX_NAME_LENGTH,
} from "../interfaces";
import { submitFeedback } from "../services/feedback-api";

const feedbackSectionClassName =
  "rounded-lg border border-zinc-200 bg-white p-3 dark:border-zinc-800 dark:bg-zinc-950";

const feedbackLabelClassName =
  "text-xs font-semibold uppercase tracking-wide text-zinc-500 dark:text-zinc-400";

const feedbackFieldClassName =
  "h-auto min-h-0 w-full rounded-md border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm text-foreground shadow-none transition-colors placeholder:text-zinc-400 focus-visible:border-zinc-400 focus-visible:ring-2 focus-visible:ring-zinc-200/80 dark:border-zinc-800 dark:bg-zinc-900/50 dark:placeholder:text-zinc-500 dark:focus-visible:border-zinc-600 dark:focus-visible:ring-zinc-800";

const feedbackPrimaryButtonClassName =
  "inline-flex h-9 shrink-0 items-center justify-center gap-1.5 rounded-md bg-zinc-900 px-4 text-sm font-semibold text-white transition-colors hover:bg-zinc-800 disabled:pointer-events-none disabled:opacity-50 dark:bg-zinc-100 dark:text-zinc-900 dark:hover:bg-zinc-200";

const LG_UP_MEDIA_QUERY = "(min-width: 1024px)";

function useIsLgUp() {
  const [isLgUp, setIsLgUp] = useState(
    () => typeof window !== "undefined" && window.matchMedia(LG_UP_MEDIA_QUERY).matches,
  );

  useEffect(() => {
    const media = window.matchMedia(LG_UP_MEDIA_QUERY);
    const onChange = () => setIsLgUp(media.matches);
    onChange();
    media.addEventListener("change", onChange);
    return () => media.removeEventListener("change", onChange);
  }, []);

  return isLgUp;
}

function getApiErrorMessage(err: unknown): string | null {
  if (!axios.isAxiosError(err) || !err.response) {
    return null;
  }
  const data = err.response.data;
  if (typeof data === "string" && data.trim().length > 0) {
    return data;
  }
  return null;
}

type FeedbackSheetProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

function validateEmail(email: string): string | null {
  const trimmed = email.trim();
  if (trimmed.length === 0) {
    return null;
  }
  if (trimmed.length > FEEDBACK_MAX_EMAIL_LENGTH) {
    return `Email must be at most ${FEEDBACK_MAX_EMAIL_LENGTH} characters.`;
  }
  if (!trimmed.includes("@") || trimmed.startsWith("@") || trimmed.endsWith("@")) {
    return "Please enter a valid email address.";
  }
  return null;
}

function validateForm(
  message: string,
  name: string,
  email: string,
): string | null {
  const trimmedMessage = message.trim();
  if (trimmedMessage.length === 0) {
    return "Please enter a message.";
  }
  if (trimmedMessage.length > FEEDBACK_MAX_MESSAGE_LENGTH) {
    return `Message must be at most ${FEEDBACK_MAX_MESSAGE_LENGTH} characters.`;
  }
  const trimmedName = name.trim();
  if (trimmedName.length > FEEDBACK_MAX_NAME_LENGTH) {
    return `Name must be at most ${FEEDBACK_MAX_NAME_LENGTH} characters.`;
  }
  return validateEmail(email);
}

export function FeedbackSheet({ open, onOpenChange }: FeedbackSheetProps) {
  const isLgUp = useIsLgUp();
  const sheetSide = isLgUp ? "right" : "bottom";

  const [message, setMessage] = useState("");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);

  const resetForm = useCallback(() => {
    setMessage("");
    setName("");
    setEmail("");
    setError(null);
    setIsSubmitting(false);
    setIsSuccess(false);
  }, []);

  useEffect(() => {
    if (!open) {
      resetForm();
    }
  }, [open, resetForm]);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    const validationError = validateForm(message, name, email);
    if (validationError) {
      setError(validationError);
      return;
    }

    setError(null);
    setIsSubmitting(true);

    const trimmedName = name.trim();
    const trimmedEmail = email.trim();

    try {
      await submitFeedback({
        message: message.trim(),
        ...(trimmedName ? { name: trimmedName } : {}),
        ...(trimmedEmail ? { email: trimmedEmail } : {}),
      });
      setIsSuccess(true);
    } catch (err: unknown) {
      setError(getApiErrorMessage(err) ?? "Something went wrong. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        key={sheetSide}
        side={sheetSide}
        className={cn(
          "flex flex-col gap-0 p-0",
          sheetSide === "bottom" && "max-h-[85dvh] overflow-y-auto",
          sheetSide === "right" && "h-full max-w-md",
        )}
      >
        <SheetHeader className="gap-2 border-b border-zinc-200 px-4 pb-4 pt-5 pr-12 dark:border-zinc-800">
          <SheetTitle className="flex items-center gap-2 text-base font-semibold tracking-tight text-foreground">
            <MessageSquare className="size-5 shrink-0 text-zinc-600 dark:text-zinc-400" aria-hidden />
            Feedback
          </SheetTitle>
          <SheetDescription className="text-sm leading-relaxed text-zinc-600 dark:text-zinc-400">
            A bug, an idea, or just a thought.
            <br />
            Whatever you want to share, I read every message.
          </SheetDescription>
        </SheetHeader>

        {isSuccess ? (
          <div className="flex flex-1 flex-col gap-4 px-4 py-5">
            <div className={feedbackSectionClassName}>
              <p className="text-sm leading-relaxed text-zinc-700 dark:text-zinc-300">
                Thanks — I got your feedback.
              </p>
            </div>
            <button
              type="button"
              className={cn(feedbackPrimaryButtonClassName, "w-full")}
              onClick={() => onOpenChange(false)}
            >
              Close
            </button>
          </div>
        ) : (
          <form className="flex min-h-0 flex-1 flex-col" onSubmit={handleSubmit}>
            <div className="flex flex-1 flex-col gap-4 overflow-y-auto px-4 py-4">
                <div className="flex flex-col gap-3">
                  <div className="flex flex-col gap-1.5">
                    <label htmlFor="feedback-name" className="text-sm font-medium text-zinc-800 dark:text-zinc-200">
                      Name{" "}
                      <span className="font-normal text-zinc-500 dark:text-zinc-500">(optional)</span>
                    </label>
                    <Input
                      id="feedback-name"
                      type="text"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                      placeholder="Your name"
                      maxLength={FEEDBACK_MAX_NAME_LENGTH}
                      disabled={isSubmitting}
                      autoComplete="name"
                      className={feedbackFieldClassName}
                    />
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <label htmlFor="feedback-email" className="text-sm font-medium text-zinc-800 dark:text-zinc-200">
                      Email{" "}
                      <span className="font-normal text-zinc-500 dark:text-zinc-500">(optional)</span>
                    </label>
                    <Input
                      id="feedback-email"
                      type="email"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="you@example.com"
                      maxLength={FEEDBACK_MAX_EMAIL_LENGTH}
                      disabled={isSubmitting}
                      autoComplete="email"
                      className={feedbackFieldClassName}
                    />
                  </div>
                </div>

                <div className="flex flex-col gap-3 border-t border-zinc-200 pt-4 dark:border-zinc-800">
                  <p className={feedbackLabelClassName}>Message</p>
                  <div className="flex flex-col gap-1.5">
                    <Textarea
                      id="feedback-message"
                      value={message}
                      onChange={(e) => setMessage(e.target.value)}
                      placeholder="What's on your mind?"
                      rows={8}
                      maxLength={FEEDBACK_MAX_MESSAGE_LENGTH}
                      disabled={isSubmitting}
                      aria-invalid={error !== null}
                      className={cn(
                        feedbackFieldClassName,
                        "field-sizing-fixed h-52 max-h-52 min-h-52 resize-none overflow-y-auto py-2.5",
                      )}
                    />
                    <p className="text-xs text-zinc-500 dark:text-zinc-500">
                      {message.trim().length}/{FEEDBACK_MAX_MESSAGE_LENGTH}
                    </p>
                    {error ? (
                      <p
                        className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/30 dark:text-red-200"
                        role="alert"
                      >
                        {error}
                      </p>
                    ) : null}
                    <button
                      type="submit"
                      className={cn(feedbackPrimaryButtonClassName, "mt-1 w-full")}
                      disabled={isSubmitting}
                    >
                      {isSubmitting ? (
                        <>
                          <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                          Sending…
                        </>
                      ) : (
                        <>
                          Send feedback
                          <Send className="size-4 shrink-0" aria-hidden />
                        </>
                      )}
                    </button>
                  </div>
                </div>
            </div>
          </form>
        )}
      </SheetContent>
    </Sheet>
  );
}

const FEEDBACK_INTRO_TOOLTIP_MS = 3000;
const FEEDBACK_INTRO_TOOLTIP_DELAY_MS = 800;

export function FeedbackSheetTrigger({
  className,
}: {
  className?: string;
}) {
  const [sheetOpen, setSheetOpen] = useState(false);
  const [introTooltipOpen, setIntroTooltipOpen] = useState(false);
  const dismissTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const closeIntroTooltip = useCallback(() => {
    if (dismissTimerRef.current) {
      clearTimeout(dismissTimerRef.current);
      dismissTimerRef.current = null;
    }
    setIntroTooltipOpen(false);
  }, []);

  useEffect(() => {
    const showTimer = setTimeout(() => {
      setIntroTooltipOpen(true);
      dismissTimerRef.current = setTimeout(closeIntroTooltip, FEEDBACK_INTRO_TOOLTIP_MS);
    }, FEEDBACK_INTRO_TOOLTIP_DELAY_MS);

    return () => {
      clearTimeout(showTimer);
      if (dismissTimerRef.current) {
        clearTimeout(dismissTimerRef.current);
      }
    };
  }, [closeIntroTooltip]);

  function openFeedbackSheet() {
    closeIntroTooltip();
    setSheetOpen(true);
  }

  return (
    <>
      <Tooltip
        open={introTooltipOpen}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) {
            closeIntroTooltip();
          } else {
            setIntroTooltipOpen(true);
          }
        }}
      >
        <TooltipTrigger
          render={
            <button
              type="button"
              onClick={openFeedbackSheet}
              aria-label="Send feedback"
              className={className}
            />
          }
        >
          <MessageSquare aria-hidden />
          <span className="sr-only">Send feedback</span>
        </TooltipTrigger>
        <TooltipContent side="bottom" className="max-w-[220px] text-center">
          Send feedback
        </TooltipContent>
      </Tooltip>
      <FeedbackSheet open={sheetOpen} onOpenChange={setSheetOpen} />
    </>
  );
}
