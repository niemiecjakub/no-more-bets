import asyncio
import logging
import os
from pprint import pprint
from typing import Dict, List, Optional, Mapping
from dotenv import load_dotenv
from rapidfuzz import process
from agents.group_chat import run_betting_analysis
from constants import PREMIER_LEAGUE
from models.rotowire import GameLineup
from models.soccerdata import UpcomingMatchPreview
from services.betclic import Betclic
from services.fbref import FBref
from services.premierinjuries import PremierInjuries
from services.rotowire import Rotowire
from services.soccerdata import SoccerData
from utils.utils import print_events

logging.basicConfig(
    level=logging.DEBUG, 
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)



def teams_key(home: str, away: str) -> frozenset[str]:
    return frozenset({home.lower().strip(), away.lower().strip()})
 
def build_lineup_index(lineups: List[GameLineup]) -> Dict[frozenset[str], GameLineup]:
    return {
        teams_key(lineup.home_team.team_name, lineup.away_team.team_name): lineup
        for lineup in lineups
    }

def find_matching_lineup(
    match_preview: UpcomingMatchPreview,
    lineup_index: Mapping[frozenset[str], GameLineup]
) -> Optional[GameLineup]:
    key = teams_key(
        match_preview.teams.home.name,
        match_preview.teams.away.name
    )
    
    if key in lineup_index:
        return lineup_index[key]
    
    # Fuzzy matching
    search_str = " vs ".join(sorted(key))
    candidates = {" vs ".join(sorted(k)): lineup_index[k] for k in lineup_index.keys()}
    
    result = process.extractOne(search_str, candidates.keys(), score_cutoff=85)
    if result:
        return candidates[result[0]]
    
    return None


def soccer():
    #soccerdata = SoccerData()
    #soccerdata.get_matches(league_id=PREMIER_LEAGUE.SOCCERDATA_PREMIER_LEAGUE_ID, season=PREMIER_LEAGUE.SOCCERDATA_CURRENT_SEASON)

    #premierinjuries = PremierInjuries()
    #x = premierinjuries.get_premier_league_injuries_html()
    #pprint(x)

    rotowire = Rotowire()
    lineups = rotowire.get_soccer_lineups()
    lineup_index = build_lineup_index(lineups)

    soccerdata = SoccerData()
    upcoming_league_matches = soccerdata.get_match_previews_upcoming(league_id=PREMIER_LEAGUE.SOCCERDATA_PREMIER_LEAGUE_ID)

    for league in upcoming_league_matches:
        print(f"{league.league_name} - {len(league.match_previews)}")

        for match in league.match_previews:
            home_team_name = match.teams.home.name
            away_team_name = match.teams.away.name
            print(f"{home_team_name} (H) vs {away_team_name} (A) - {match.date} @ {match.time}")

            lineup = find_matching_lineup(match, lineup_index)
            if lineup:
                home_players = lineup.home_team.players
                away_players = lineup.away_team.players
                home_count = len(home_players)
                away_count = len(away_players)
                
                # Check if lineup lengths are not 11
                if home_count != 11:
                    logging.error(f"{home_team_name} lineup has {home_count} players (expected 11)")
                if away_count != 11:
                    logging.error(f"{away_team_name} lineup has {away_count} players (expected 11)")
                
                # Print players side by side
                max_players = max(home_count, away_count)
                home_header = f"{home_team_name} ({lineup.home_team.lineup_type})"
                away_header = f"{away_team_name} ({lineup.away_team.lineup_type})"
                print(f"  {home_header:<40} {away_header:<40}")
                print(f"  {'-' * 40} {'-' * 40}")
                
                for i in range(max_players):
                    home_player = home_players[i] if i < home_count else None
                    away_player = away_players[i] if i < away_count else None
                    
                    home_str = f"[{home_player.position}] {home_player.player_name}" if home_player else ""
                    away_str = f"[{away_player.position}] {away_player.player_name}" if away_player else ""
                    
                    print(f"  {home_str:<40} {away_str:<40}")
                
                print() 
            
                home_injuries = lineup.home_team.injuries
                away_injuries = lineup.away_team.injuries
                home_injury_count = len(home_injuries)
                away_injury_count = len(away_injuries)
                
                if home_injury_count > 0 or away_injury_count > 0:
                    home_injury_title = f"Injured ({home_injury_count}):" if home_injury_count > 0 else ""
                    away_injury_title = f"Injured ({away_injury_count}):" if away_injury_count > 0 else ""
                    print(f"  {home_injury_title:<40} {away_injury_title:<40}")
                    
                    max_injuries = max(home_injury_count, away_injury_count)
                    for i in range(max_injuries):
                        home_injury = home_injuries[i] if i < home_injury_count else None
                        away_injury = away_injuries[i] if i < away_injury_count else None
                        
                        home_str = f"[{home_injury.position}] {home_injury.player} ({home_injury.status})" if home_injury else ""
                        away_str = f"[{away_injury.position}] {away_injury.player} ({away_injury.status})" if away_injury else ""
                        
                        print(f"  {home_str:<40} {away_str:<40}")
            else:
                logging.error(f"No matching lineup found for {home_team_name} vs {away_team_name}")
            
            print() 


def main():
    betclic = Betclic(cache_ttl=9999999999999999999999999999999999999999999, delay=10, retry_delay=20, n_retries=10, timeout=60)
    games = betclic.get_upcoming_games()
    pprint("Upcoming games: " + str(len(games)))
    all_events = []
    for game in games:
        events = betclic.get_match_events(game.url, expand=False)
        all_events.extend(events)
        print("Game: " + game.url + " - Events: " + str(len(events)))

    print_events(all_events[:3])
    #print_events(events)
    #load_dotenv()
    #result = asyncio.run(run_betting_analysis("Analyze Leeds vs Fulham", verbose=True))
    #print(result)

if __name__ == "__main__":
    load_dotenv()
    #main()
    soccer()
