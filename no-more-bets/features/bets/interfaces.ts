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
  outcomeKey: string;
  oddsAtPlacement: number;
  statusId: number;
  statusName: string;
}

/** Status IDs matching backend BetStatus enum (Pending=1, Won=2, Lost=3, CashedOut=4). */
export const BET_STATUS = {
  Pending: 1,
  Won: 2,
  Lost: 3,
  CashedOut: 4,
} as const;
