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
}

/**
 * Single selection within a bet slip, aligned with backend BetSelectionItemDto.
 */
export interface BetSelectionItem {
  matchId: number;
  homeClubName: string;
  awayClubName: string;
  eventTypeName: string;
  eventOptionName: string;
  oddsAtPlacement: number;
  statusId: number;
  statusName: string;
}

/** Status IDs matching backend BetStatus enum (Pending=1, Won=2, Lost=3). */
export const BET_STATUS = {
  Pending: 1,
  Won: 2,
  Lost: 3,
} as const;

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
