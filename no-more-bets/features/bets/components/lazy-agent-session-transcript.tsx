"use client";

import { useEffect, useRef, useState } from "react";
import { handleServiceError } from "@/lib/error-handler";
import { fetchAgentSessionMessages } from "../services/agent-session-api";
import type { AgentSessionMessage } from "../services/agent-session-api";
import { AgentSessionTranscript } from "./agent-session-transcript";

interface LazyAgentSessionTranscriptProps {
  sessionId: number;
  active: boolean;
}

/**
 * Fetches transcript once when `active` becomes true (e.g. details element opened).
 */
export function LazyAgentSessionTranscript({
  sessionId,
  active,
}: LazyAgentSessionTranscriptProps) {
  const [messages, setMessages] = useState<AgentSessionMessage[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fetchStartedRef = useRef(false);

  useEffect(() => {
    fetchStartedRef.current = false;
    setMessages(null);
    setError(null);
    setLoading(false);
  }, [sessionId]);

  useEffect(() => {
    if (!active || fetchStartedRef.current) return;
    fetchStartedRef.current = true;
    let cancelled = false;
    setLoading(true);
    setError(null);
    void fetchAgentSessionMessages(sessionId)
      .then((data) => {
        if (!cancelled) setMessages(data);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(handleServiceError(err, "Failed to load session transcript."));
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [active, sessionId]);

  if (!active) return null;
  if (loading && messages === null) {
    return (
      <p className="px-4 py-3 text-sm text-zinc-500 dark:text-zinc-400">
        Loading transcript...
      </p>
    );
  }
  if (error) {
    return <p className="px-4 py-3 text-sm text-red-800 dark:text-red-200">{error}</p>;
  }
  if (messages) {
    return <AgentSessionTranscript messages={messages} />;
  }
  return null;
}
