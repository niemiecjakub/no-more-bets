/**
 * League list item aligned with backend LeagueDto.
 * GET api/Database/leagues returns an array of these.
 */
export interface LeagueListItem {
  id: number;
  name: string;
}
