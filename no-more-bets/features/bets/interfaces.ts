/**
 * Bet slip list item aligned with backend BetSlipListItemDto.
 * GET api/Database/bet-slips returns an array of these.
 */
export interface BetSlipListItem {
  id: number;
  createdAt: string;
  stakeAmount: number;
  totalOdds: number;
  potentialPayout: number;
  statusId: number;
  statusName: string;
  selections: BetSelectionItem[];
  agentSessionId: number | null;
}

/**
 * Single selection within a bet slip, aligned with backend BetSelectionItemDto.
 */
export interface BetSelectionItem {
  matchId: number;
  homeClubName: string;
  awayClubName: string;
  homeClubSlug?: string | null;
  awayClubSlug?: string | null;
  eventTypeName: string;
  eventOptionName: string;
  oddsAtPlacement: number;
  statusId: number;
  statusName: string;
}

/** Status IDs matching backend BetStatus enum (Pending=1, Won=2, Lost=3, Canceled=4). */
export const BET_STATUS = {
  Pending: 1,
  Won: 2,
  Lost: 3,
  Canceled: 4,
} as const;

/**
 * Paper slip from research phase — aligned with backend BetSlipSummary (GET …/research-bet-slip).
 */
export interface BetSlipSummaryDto {
  id: number;
  createdAt: string;
  stakeAmount: number;
  totalOdds: number;
  potentialPayout: number;
  status: number;
  selections: BetSelectionSummaryDto[];
}

export interface BetSelectionSummaryDto {
  matchId: number;
  homeClubName: string;
  awayClubName: string;
  eventTypeName: string;
  outcomeKey: string;
  oddsAtPlacement: number;
  status: number;
}

/** GET api/Database/bankroll — BankrollRecordDto */
export interface BankrollRecord {
  id: number;
  name: string;
  amount: number;
  flow: "In" | "Out";
  betId: number | null;
  createdAt: string;
}

/** GET api/Database/bankroll — BankrollDashboardDto */
export interface BankrollDashboard {
  currentBalance: number;
  daysUntilPayday: number;
  records: BankrollRecord[];
}
