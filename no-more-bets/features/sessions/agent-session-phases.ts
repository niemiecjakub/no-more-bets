import { Bot, Globe, Lightbulb, Search, Ticket, Trash2 } from "lucide-react";
import type { LucideIcon } from "lucide-react";

export interface AgentSessionPhaseDefinition {
  id: number;
  name: string;
  label: string;
  icon: LucideIcon;
}

/** Aligned with backend AgentSessionPhase enum. */
export const AGENT_SESSION_PHASES: AgentSessionPhaseDefinition[] = [
  { id: 1, name: "Research", label: "Research", icon: Search },
  { id: 2, name: "Betting", label: "Betting", icon: Ticket },
  { id: 3, name: "Reflection", label: "Reflection", icon: Lightbulb },
  { id: 4, name: "InternetResearch", label: "Internet", icon: Globe },
  { id: 5, name: "MemoryCleanup", label: "Cleanup", icon: Trash2 },
];

const phaseIconById = new Map(AGENT_SESSION_PHASES.map((phase) => [phase.id, phase.icon]));

export function sessionPhaseIcon(phaseId: number): LucideIcon {
  return phaseIconById.get(phaseId) ?? Bot;
}

export function isBettingSessionPhase(phaseId: number): boolean {
  return phaseId === 2;
}
