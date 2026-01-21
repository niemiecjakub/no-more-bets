"""Output strategy for match analysis results."""
from abc import ABC, abstractmethod
from typing import List, Optional
from models.match_analysis import (
    MatchInfo,
    LineupData,
    HeadToHeadData,
    MatchPreviewData,
)
from models.betclic import BookmakerEvent


class MatchAnalysisOutput(ABC):
    """Abstract base class for match analysis output handlers."""
    
    @abstractmethod
    def print_match_header(self, match_info: MatchInfo) -> None:
        """Print match header information.
        
        Parameters
        ----------
        match_info : MatchInfo
            Basic match information.
        """
        pass
    
    @abstractmethod
    def print_lineup(self, lineup_data: LineupData, home_team_name: str, away_team_name: str) -> None:
        """Print lineup information.
        
        Parameters
        ----------
        lineup_data : LineupData
            Lineup data for both teams.
        home_team_name : str
            Home team name.
        away_team_name : str
            Away team name.
        """
        pass
    
    @abstractmethod
    def print_head_to_head(self, head_to_head: HeadToHeadData) -> None:
        """Print head-to-head statistics.
        
        Parameters
        ----------
        head_to_head : HeadToHeadData
            Head-to-head statistics data.
        """
        pass
    
    @abstractmethod
    def print_match_preview(self, preview: MatchPreviewData) -> None:
        """Print match preview information.
        
        Parameters
        ----------
        preview : MatchPreviewData
            Match preview data.
        """
        pass
    
    @abstractmethod
    def print_betting_events(self, events: List[BookmakerEvent]) -> None:
        """Print betting events information.
        
        Parameters
        ----------
        events : List[BookmakerEvent]
            List of betting events.
        """
        pass
    
    def print_empty_line(self) -> None:
        """Print an empty line (optional, can be overridden)."""
        pass


class ConsoleOutput(MatchAnalysisOutput):
    """Console output handler that prints to stdout."""
    
    def print_match_header(self, match_info: MatchInfo) -> None:
        """Print match header information."""
        print(f"{match_info.home} (H) vs {match_info.away} (A) - {match_info.date} @ {match_info.time}")
    
    def print_lineup(self, lineup_data: LineupData, home_team_name: str, away_team_name: str) -> None:
        """Print lineup information."""
        home_players = lineup_data.home.players
        away_players = lineup_data.away.players
        home_count = len(home_players)
        away_count = len(away_players)
        
        max_players = max(home_count, away_count)
        home_header = f"{home_team_name} ({lineup_data.home.lineup_type})"
        away_header = f"{away_team_name} ({lineup_data.away.lineup_type})"
        print(f"  {home_header:<40} {away_header:<40}")
        print(f"  {'-' * 40} {'-' * 40}")
        
        for i in range(max_players):
            home_player = home_players[i] if i < home_count else None
            away_player = away_players[i] if i < away_count else None
            
            home_str = f"[{home_player.position}] {home_player.player}" if home_player else ""
            away_str = f"[{away_player.position}] {away_player.player}" if away_player else ""
            
            print(f"  {home_str:<40} {away_str:<40}")
        
        print()
        
        home_injuries = lineup_data.home.injuries
        away_injuries = lineup_data.away.injuries
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
    
    def print_head_to_head(self, head_to_head: HeadToHeadData) -> None:
        """Print head-to-head statistics."""
        print()
        print("  Head-to-Head:")
        overall = head_to_head.overall
        team1_home = head_to_head.team1_at_home
        team2_home = head_to_head.team2_at_home
        
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
    
    def print_match_preview(self, preview: MatchPreviewData) -> None:
        """Print match preview information."""
        print()
        print("  Match Preview:")
        print(f"    Excitement Rating: {preview.excitement_rating}")
        print(f"    Prediction: {preview.prediction.type} - {preview.prediction.choice} ({preview.prediction.team_name})")
        print(f"    Weather: {preview.weather.description} ({preview.weather.temp_c}°C / {preview.weather.temp_f}°F)")
        if preview.preview_content:
            print("    Preview Content:")
            for item in preview.preview_content:
                print(f"      [{item.name}] {item.content}")
    
    def print_betting_events(self, events: List[BookmakerEvent]) -> None:
        """Print betting events information."""
        print()
        print(f"  Betting Events ({len(events)} total):")
    
    def print_empty_line(self) -> None:
        """Print an empty line."""
        print()


class SilentOutput(MatchAnalysisOutput):
    """Silent output handler that does nothing (for testing/automation)."""
    
    def print_match_header(self, match_info: MatchInfo) -> None:
        """Do nothing."""
        pass
    
    def print_lineup(self, lineup_data: LineupData, home_team_name: str, away_team_name: str) -> None:
        """Do nothing."""
        pass
    
    def print_head_to_head(self, head_to_head: HeadToHeadData) -> None:
        """Do nothing."""
        pass
    
    def print_match_preview(self, preview: MatchPreviewData) -> None:
        """Do nothing."""
        pass
    
    def print_betting_events(self, events: List[BookmakerEvent]) -> None:
        """Do nothing."""
        pass
    
    def print_empty_line(self) -> None:
        """Do nothing."""
        pass
