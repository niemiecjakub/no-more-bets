/**
 * Club list item aligned with backend ClubDto.
 * GET api/clubs returns an array of these.
 */
export interface ClubListItem {
  id: number;
  name: string;
  leagueId: number;
  leagueName: string;
  slug: string;
  leagueSlug: string;
}

/** Club detail from GET api/clubs/{id} */
export type ClubDetail = ClubListItem;

export interface ClubNextMatch {
  matchId: number;
  matchDate: string;
  homeClubId: number;
  awayClubId: number;
  homeClubName: string;
  awayClubName: string;
  homeClubSlug: string;
  awayClubSlug: string;
  isHome: boolean;
}

export interface ClubBetSelectionStats {
  wonCount: number;
  lostCount: number;
  totalCount: number;
}
