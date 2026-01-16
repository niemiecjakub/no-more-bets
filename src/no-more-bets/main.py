import asyncio
import json
import logging
import os
from datetime import datetime
from pprint import pprint
from typing import Dict, List, Optional, Mapping
from dotenv import load_dotenv
from rapidfuzz import process
from agents.group_chat import run_betting_analysis
from constants import PREMIER_LEAGUE
from models.rotowire import GameLineup
from models.soccerdata import UpcomingMatchPreview, Teams, TeamInfo, LeagueMatchPreviews
from models.match_analysis import (
    MatchAnalysis,
    MatchInfo,
    LineupData,
    TeamLineupData,
    HeadToHeadData,
    MatchPreviewData,
    PredictionData,
    WeatherData,
    FBrefTeamData,
)
from models.fbref import Club, Game
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


def find_matching_fbref_club(team_name: str, fbref_clubs: List[Club]) -> Optional[Club]:
    """Find matching club from FBref clubs by team name using fuzzy matching."""
    # Normalize team name for better matching
    normalized_search = team_name.lower().strip()
    
    # Exact match first (case-insensitive)
    for club in fbref_clubs:
        if club.team.lower().strip() == normalized_search:
            return club
    
    # Try partial match (if search name is contained in club name or vice versa)
    for club in fbref_clubs:
        normalized_club = club.team.lower().strip()
        if normalized_search in normalized_club or normalized_club in normalized_search:
            return club
    
    # Fuzzy matching with lower threshold for better matching
    candidates = {club.team: club for club in fbref_clubs}
    result = process.extractOne(team_name, candidates.keys(), score_cutoff=70)
    if result:
        logging.debug(f"Fuzzy matched '{team_name}' to '{result[0]}' (score: {result[1]:.1f})")
        return candidates[result[0]]
    
    return None


def soccer() -> List[MatchAnalysis]:
    rotowire = Rotowire()
    lineups = rotowire.get_soccer_lineups()
    lineup_index = build_lineup_index(lineups)

    soccerdata = SoccerData()
    upcoming_league_matches = soccerdata.get_match_previews_upcoming(league_id=PREMIER_LEAGUE.SOCCERDATA_PREMIER_LEAGUE_ID)

    bookmaker = Betclic(cache_ttl=9999999999999999999999999999999999999999999, delay=10, retry_delay=20, n_retries=10, timeout=60)
    bookmaker_matches = bookmaker.get_upcoming_games()
    
    # Initialize FBref and get Premier League stats once
    fbref = FBref(cache_ttl=36000000000)
    try:
        fbref_clubs = fbref.get_premier_league_stats()
        logging.info(f"Loaded {len(fbref_clubs)} FBref clubs")
    except Exception as e:
        logging.error(f"Failed to load FBref Premier League stats: {e}")
        fbref_clubs = []
    
    results: List[MatchAnalysis] = []
    
    for match in bookmaker_matches:
        home_team_name = match.home_team
        away_team_name = match.away_team
        print(f"{home_team_name} (H) vs {away_team_name} (A) - {match.date} @ {match.time}")

        match_info = MatchInfo(
            home_team=match.home_team,
            away_team=match.away_team,
            date=match.date,
            time=match.time
        )

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
            
            lineup_data = LineupData(
                home_team=TeamLineupData(
                    team_name=lineup.home_team.team_name,
                    lineup_type=lineup.home_team.lineup_type,
                    players=lineup.home_team.players,
                    injuries=lineup.home_team.injuries
                ),
                away_team=TeamLineupData(
                    team_name=lineup.away_team.team_name,
                    lineup_type=lineup.away_team.lineup_type,
                    players=lineup.away_team.players,
                    injuries=lineup.away_team.injuries
                )
            )
        else:
            logging.error(f"No matching lineup found for {home_team_name} vs {away_team_name}")
            lineup_data = None
        
        soccerdata_match = find_matching_soccerdata_match(home_team_name, away_team_name, upcoming_league_matches)
        head_to_head_data = None
        
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
                
                head_to_head_data = HeadToHeadData(
                    team1=head_to_head.team1,
                    team2=head_to_head.team2,
                    overall=head_to_head.stats.overall,
                    team1_at_home=head_to_head.stats.team1_at_home,
                    team2_at_home=head_to_head.stats.team2_at_home
                )
            else:
                head_to_head_data = None
            
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
                
                match_preview_data = MatchPreviewData(
                    excitement_rating=match_preview.match_data.excitement_rating,
                    prediction=PredictionData(
                        type=match_preview.match_data.prediction.type,
                        choice=match_preview.match_data.prediction.choice,
                        team_name=team_name
                    ),
                    weather=WeatherData(
                        description=match_preview.match_data.weather.description,
                        temp_c=match_preview.match_data.weather.temp_c,
                        temp_f=match_preview.match_data.weather.temp_f
                    ),
                    preview_content=match_preview.preview_content
                )
            else:
                match_preview_data = None
        else:
            match_preview_data = None
        
        events = bookmaker.get_match_events(match.url, expand=False)
        if events:
            print()
            print(f"  Betting Events ({len(events)} total):")     
            #print_events(events)
            betting_events_data = events
        else:
            betting_events_data = None
        
        # Fetch FBref data for both teams
        fbref_home_data = None
        fbref_away_data = None
        
        if fbref_clubs:
            # Find home team club stats
            home_club = find_matching_fbref_club(home_team_name, fbref_clubs)
            if home_club:
                try:
                    home_recent_games = fbref.get_club_games(home_club.team, epl_only=True, limit=5, only_finished=True)
                    fbref_home_data = FBrefTeamData(
                        club_stats=home_club,
                        recent_games=home_recent_games
                    )
                    logging.info(f"FBref data loaded for {home_team_name}: {len(home_recent_games)} recent games")
                except Exception as e:
                    logging.error(f"Failed to get FBref games for {home_team_name}: {e}")
                    fbref_home_data = FBrefTeamData(club_stats=home_club, recent_games=[])
            else:
                logging.warning(f"FBref club not found for {home_team_name}")
            
            # Find away team club stats
            away_club = find_matching_fbref_club(away_team_name, fbref_clubs)
            if away_club:
                try:
                    away_recent_games = fbref.get_club_games(away_club.team, epl_only=True, limit=5, only_finished=True)
                    fbref_away_data = FBrefTeamData(
                        club_stats=away_club,
                        recent_games=away_recent_games
                    )
                    logging.info(f"FBref data loaded for {away_team_name}: {len(away_recent_games)} recent games")
                except Exception as e:
                    logging.error(f"Failed to get FBref games for {away_team_name}: {e}")
                    fbref_away_data = FBrefTeamData(club_stats=away_club, recent_games=[])
            else:
                logging.warning(f"FBref club not found for {away_team_name}")
        
        print()
        
        match_analysis = MatchAnalysis(
            match_info=match_info,
            lineup=lineup_data,
            head_to_head=head_to_head_data,
            match_preview=match_preview_data,
            betting_events=betting_events_data,
            fbref_home=fbref_home_data,
            fbref_away=fbref_away_data
        )
        results.append(match_analysis)
    
    # Serialize and save results to output directory
    output_dir = os.path.join(os.path.dirname(__file__), "cache", "output")
    os.makedirs(output_dir, exist_ok=True)
    
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    output_file = os.path.join(output_dir, f"match_analysis_{timestamp}.json")
    
    serialized_results = [result.model_dump() for result in results]
    
    with open(output_file, "w", encoding="utf-8") as f:
        json.dump(serialized_results, f, indent=2, ensure_ascii=False)
    
    logging.info(f"Results saved to: {output_file}")
    
    return results


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
