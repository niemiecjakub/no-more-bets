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
  rationale?: string | null;
  estimatedWinProbability?: number | null;
  riskLevelId?: number | null;
  riskLevelName?: string | null;
  slipDate?: string | null;
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

/** Risk IDs matching backend BetRiskLevel enum (Low=1, Medium=2, High=3). */
export const BET_RISK_LEVEL = {
  Low: 1,
  Medium: 2,
  High: 3,
} as const;

export type BetStatusId = (typeof BET_STATUS)[keyof typeof BET_STATUS];

/** API may send BetStatus as enum string (JsonStringEnumConverter) or numeric id. */
export type BetStatusApiValue = BetStatusId | keyof typeof BET_STATUS;

const BET_STATUS_NAME_TO_ID: Record<keyof typeof BET_STATUS, BetStatusId> = {
  Pending: BET_STATUS.Pending,
  Won: BET_STATUS.Won,
  Lost: BET_STATUS.Lost,
  Canceled: BET_STATUS.Canceled,
};

/** Maps API status (string enum name or numeric id) to BetStatusId; returns 0 when unrecognized. */
export function normalizeBetStatus(status: unknown): BetStatusId | 0 {
  if (typeof status === "number") {
    return status === BET_STATUS.Pending ||
      status === BET_STATUS.Won ||
      status === BET_STATUS.Lost ||
      status === BET_STATUS.Canceled
      ? status
      : 0;
  }
  if (typeof status === "string" && status in BET_STATUS_NAME_TO_ID) {
    return BET_STATUS_NAME_TO_ID[status as keyof typeof BET_STATUS];
  }
  return 0;
}

/**
 * Paper slip from research phase — aligned with backend BetSlipSummary.
 */
export interface BetSlipSummaryDto {
  id: number;
  createdAt: string;
  stakeAmount: number;
  totalOdds: number;
  potentialPayout: number;
  status: BetStatusId | 0;
  selections: BetSelectionSummaryDto[];
  rationale?: string | null;
  estimatedWinProbability?: number | null;
}

export interface BetSelectionSummaryDto {
  matchId: number;
  homeClubName: string;
  awayClubName: string;
  eventTypeName: string;
  outcomeKey: string;
  oddsAtPlacement: number;
  status: BetStatusId | 0;
}

/** GET …/research-bet-slip — slip + equal-stake parlay vs singles P&L (scenarios null while pending). */
export interface MatchResearchBetSlipDto {
  slip: BetSlipSummaryDto;
  scenarios: ResearchBetScenarioStatsDto | null;
}

export interface ResearchBetScenarioStatsDto {
  unitStake: number;
  parlay: ResearchBetParlayScenarioDto;
  singles: ResearchBetSinglesScenarioDto;
}

export interface ResearchBetParlayScenarioDto {
  stakeTotal: number;
  combinedOdds: number;
  potentialPayout: number;
  profit: number | null;
}

export interface ResearchBetSinglesScenarioDto {
  stakeTotal: number;
  potentialPayout: number;
  profit: number | null;
  legs: ResearchBetSingleLegDto[];
}

export interface ResearchBetSingleLegDto {
  stake: number;
  odds: number;
  status: BetStatusId | 0;
  profit: number | null;
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
  /** Whole UTC calendar days until month-end salary (0 = payday today). */
  daysUntilPayday: number;
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

/** GET api/agent/dashboard/research-betting-summary */
export interface ResearchScenarioPnl {
  stakeTotal: number;
  profit: number;
  roi: number;
}

export interface AgentDashboardResearchBettingSummaryWidget {
  settledSelectionsCount: number;
  wonSelectionsCount: number;
  lostSelectionsCount: number;
  winRatePercent: number;
  lossRatePercent: number;
  unitStake: number;
  scenarioSlipCount: number;
  parlay: ResearchScenarioPnl;
  singles: ResearchScenarioPnl;
}

/** GET api/agent/dashboard/betting-summary/details */
export interface AgentDashboardBettingSummaryDetails {
  wonSlipsCount: number;
  lostSlipsCount: number;
  wonSelectionsCount: number;
  lostSelectionsCount: number;
}

/** GET api/agent/dashboard/pending-bets */
export interface AgentDashboardPendingBetsWidget {
  pendingSlipsCount: number;
  pendingStakeTotal: number;
  pendingPotentialPayoutTotal: number;
  latestPendingCreatedAt: string | null;
}

/** GET api/agent/dashboard/sessions */
export interface AgentDashboardSessionsWidget {
  sessionsCount: number;
  latestStartedAt: string | null;
  latestPhaseName: string | null;
}

/** GET api/agent/dashboard/memories */
export interface AgentDashboardMemoriesWidget {
  memoriesCount: number;
  latestUpdatedAt: string | null;
  latestName: string | null;
}

export interface BankrollEntryListItemDto {
  id: number;
  name: string;
  amount: number;
  flow: "In" | "Out";
  delta: number;
  createdAt: string;
  betId: number | null;
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
  rationale?: string | null;
  estimatedWinProbability?: number | null;
}
