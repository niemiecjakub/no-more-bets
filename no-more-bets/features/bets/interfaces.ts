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

/** GET api/bankroll/betting-balance */
export interface BankrollBettingBalance {
  balance: number;
}

/** GET api/agent/dashboard/bankroll */
export interface AgentDashboardBankrollWidget {
  totalValue: number;
  balance: number;
}

/** GET api/agent/dashboard/betting-summary */
export interface AgentDashboardBettingSummaryWidget {
  settledSlipsCount: number;
  settledSelectionsCount: number;
  wonSlipsCount: number;
  lostSlipsCount: number;
  winRatePercent: number;
  lossRatePercent: number;
}

/** GET api/agent/dashboard/betting-summary/details */
export interface AgentDashboardBettingSummaryDetails {
  wonSlipsCount: number;
  lostSlipsCount: number;
  wonSelectionsCount: number;
  lostSelectionsCount: number;
  slips: BetSlipListItem[];
}

/** GET api/agent/dashboard/pending-bets */
export interface AgentDashboardPendingBetsWidget {
  pendingSlipsCount: number;
  pendingStakeTotal: number;
  pendingPotentialPayoutTotal: number;
  latestPendingCreatedAt: string | null;
}

/** GET api/bankroll/flow-points */
export interface BankrollFlowPointDto {
  entryId: number;
  timestamp: string;
  delta: number;
  balanceAfter: number;
  flow: "In" | "Out";
  betId: number | null;
  name: string;
}

/** GET api/bankroll/entries */
export interface BankrollEntryListItemDto {
  id: number;
  name: string;
  amount: number;
  flow: "In" | "Out";
  delta: number;
  createdAt: string;
  betId: number | null;
  balanceAfter: number;
}

/** GET api/bankroll/entries/{entryId}/bet-details */
export interface BankrollEntryBetDetailsDto {
  entryId: number;
  betId: number;
  betCreatedAt: string;
  stakeAmount: number;
  totalOdds: number;
  potentialPayout: number;
  statusId: number;
  statusName: string;
  agentSessionId: number | null;
  selections: BetSelectionItem[];
}
