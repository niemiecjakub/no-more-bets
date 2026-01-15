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
from models.soccerdata import UpcomingMatchPreview, Teams, TeamInfo, LeagueMatchPreviews
from services.betclic import Betclic
from services.fbref import FBref
from services.premierinjuries import PremierInjuries
from services.rotowire import Rotowire
from services.soccerdata import SoccerData
from utils.utils import print_events

logging.basicConfig(
    level=logging.ERROR, 
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

def find_matching_soccerdata_match(
    home_team_name: str,
    away_team_name: str,
    upcoming_league_matches: List[LeagueMatchPreviews]
) -> Optional[UpcomingMatchPreview]:
    """Find matching match from upcoming_league_matches by team names."""
    key = teams_key(home_team_name, away_team_name)
    
    # Search through all leagues and matches
    for league in upcoming_league_matches:
        for match in league.match_previews:
            match_key = teams_key(match.teams.home.name, match.teams.away.name)
            if match_key == key:
                return match
    
    # Fuzzy matching
    search_str = " vs ".join(sorted(key))
    candidates = {}
    for league in upcoming_league_matches:
        for match in league.match_previews:
            match_key = teams_key(match.teams.home.name, match.teams.away.name)
            candidate_str = " vs ".join(sorted(match_key))
            candidates[candidate_str] = match
    
    result = process.extractOne(search_str, candidates.keys(), score_cutoff=85)
    if result:
        return candidates[result[0]]
    
    return None


def soccer():
    rotowire = Rotowire()
    lineups = rotowire.get_soccer_lineups()
    lineup_index = build_lineup_index(lineups)

    soccerdata = SoccerData()
    upcoming_league_matches = soccerdata.get_match_previews_upcoming(league_id=PREMIER_LEAGUE.SOCCERDATA_PREMIER_LEAGUE_ID)

    bookmaker = Betclic(cache_ttl=9999999999999999999999999999999999999999999, delay=10, retry_delay=20, n_retries=10, timeout=60)
    bookmaker_matches = bookmaker.get_upcoming_games()
    
    for match in bookmaker_matches:
        home_team_name = match.home_team
        away_team_name = match.away_team
        print(f"{home_team_name} (H) vs {away_team_name} (A) - {match.date} @ {match.time}")

        temp_match = UpcomingMatchPreview(
            id=0, 
            date=match.date,
            time=match.time,
            excitement_rating=0.0,
            teams=Teams(
                home=TeamInfo(id=0, name=home_team_name),
                away=TeamInfo(id=0, name=away_team_name)
            )
        )
        
        lineup = find_matching_lineup(temp_match, lineup_index)
        if lineup:
            home_players = lineup.home_team.players
            away_players = lineup.away_team.players
            home_count = len(home_players)
            away_count = len(away_players)
            
            if home_count != 11:
                logging.error(f"{home_team_name} lineup has {home_count} players (expected 11)")
            if away_count != 11:
                logging.error(f"{away_team_name} lineup has {away_count} players (expected 11)")
            
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
        
        soccerdata_match = find_matching_soccerdata_match(home_team_name, away_team_name, upcoming_league_matches)
        
        if soccerdata_match:
            head_to_head = soccerdata.get_head_to_head(soccerdata_match.teams.home.id, soccerdata_match.teams.away.id)
            
            if head_to_head:
                print()
                print("  Head-to-Head:")
                overall = head_to_head.stats.overall
                team1_home = head_to_head.stats.team1_at_home
                team2_home = head_to_head.stats.team2_at_home
                
                # Overall statistics
                print("    Overall Statistics:")
                print(f"      Games Played: {overall.overall_games_played}")
                print(f"      {head_to_head.team1.name} Wins: {overall.overall_team1_wins}")
                print(f"      {head_to_head.team2.name} Wins: {overall.overall_team2_wins}")
                print(f"      Draws: {overall.overall_draws}")
                print(f"      Goals: {overall.overall_team1_scored} - {overall.overall_team2_scored}")
                print()
                print(f"    {head_to_head.team1.name + ' at Home':<40} {head_to_head.team2.name + ' at Home':<40}")
                print(f"    {'-' * 40} {'-' * 40}")          
                print(f"      {'Games: ' + str(team1_home.team1_games_played_at_home):<40} {'Games: ' + str(team2_home.team2_games_played_at_home):<40}")
                print(f"      {'Wins: ' + str(team1_home.team1_wins_at_home):<40} {'Wins: ' + str(team2_home.team2_wins_at_home):<40}")
                print(f"      {'Losses: ' + str(team1_home.team1_losses_at_home):<40} {'Losses: ' + str(team2_home.team2_losses_at_home):<40}")
                print(f"      {'Draws: ' + str(team1_home.team1_draws_at_home):<40} {'Draws: ' + str(team2_home.team2_draws_at_home):<40}")
                print(f"      {'Goals: ' + str(team1_home.team1_scored_at_home) + ' - ' + str(team1_home.team1_conceded_at_home):<40} {'Goals: ' + str(team2_home.team2_scored_at_home) + ' - ' + str(team2_home.team2_conceded_at_home):<40}")
            
            match_preview = soccerdata.get_match_preview(soccerdata_match.id)
            if match_preview:
                print()
                print("  Match Preview:")
                print(f"    Excitement Rating: {match_preview.match_data.excitement_rating}")
                
                # Get team name based on prediction choice
                prediction_choice = match_preview.match_data.prediction.choice
                if prediction_choice == "home":
                    team_name = match_preview.teams.home.name
                elif prediction_choice == "away":
                    team_name = match_preview.teams.away.name
                else:
                    team_name = prediction_choice  # For "draw" or other values
                
                print(f"    Prediction: {match_preview.match_data.prediction.type} - {match_preview.match_data.prediction.choice} ({team_name})")
                print(f"    Weather: {match_preview.match_data.weather.description} ({match_preview.match_data.weather.temp_c}°C / {match_preview.match_data.weather.temp_f}°F)")
                if match_preview.preview_content:
                    print("    Preview Content:")
                    for item in match_preview.preview_content:
                        print(f"      [{item.name}] {item.content}")
        
        events = bookmaker.get_match_events(match.url, expand=False)
        if events:
            print()
            print(f"  Betting Events ({len(events)} total):")     
            #print_events(events)
        
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
