/**
 * Club list item aligned with backend ClubDto.
 * GET api/Database/clubs returns an array of these.
 */
export interface ClubListItem {
  id: number;
  name: string;
  leagueId: number;
  leagueName: string;
}
