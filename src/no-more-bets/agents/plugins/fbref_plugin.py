"""FBref Plugin for Semantic Kernel agents."""

import sys
import os
from typing import Annotated

# Add parent directory to path for imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from semantic_kernel.functions import kernel_function
from services.fbref import FBref
from models import (
    LeagueStandingsResponse,
    ClubPlayersResponse,
    ClubGamesResponse,
    ClubComparisonResponse,
    ClubComparisonMetrics,
)


class FBrefPlugin:
    """Plugin for accessing football statistics from FBref.
    
    Provides functions for agents to retrieve Premier League standings,
    player statistics, and match history with advanced metrics like xG.
    """
    
    def __init__(self):
        """Initialize the FBrefPlugin with an FBref scraper instance."""
        self._fbref = FBref(delay=5.0, use_cache=True, cache_ttl=3600.0)
    
    @kernel_function(
        name="get_league_standings",
        description="Get the current Premier League standings table with statistics including points, goal difference, xG (expected goals), and recent form. Essential for understanding team positions and performance."
    )
    def get_league_standings(self) -> LeagueStandingsResponse:
        """Get Premier League standings with detailed statistics.
        
        Returns structured response with all clubs and their league statistics.
        """
        try:
            clubs = self._fbref.get_premier_league_stats()
            
            if not clubs:
                return LeagueStandingsResponse(
                    season="2025-26",
                    competition="Premier League",
                    clubs=[],
                    total_clubs=0
                )
            
            return LeagueStandingsResponse(
                season="2025-26",
                competition="Premier League",
                clubs=clubs,
                total_clubs=len(clubs)
            )
            
        except Exception:
            # Return empty response on error
            return LeagueStandingsResponse(
                season="2025-26",
                competition="Premier League",
                clubs=[],
                total_clubs=0
            )
    
    @kernel_function(
        name="get_club_players",
        description="Get detailed player statistics for a specific club including goals, assists, xG, minutes played, and per-90 metrics. Use to analyze key players and their contributions."
    )
    def get_club_players(
        self,
        club_name: Annotated[str, "Name of the club (e.g., 'Arsenal', 'Liverpool', 'Manchester City')"]
    ) -> ClubPlayersResponse:
        """Get player statistics for a specific club.
        
        Returns structured response with all players and their statistics.
        """
        try:
            players = self._fbref.get_club_players(club_name)
            
            return ClubPlayersResponse(
                club_name=club_name,
                players=players if players else [],
                total_players=len(players) if players else 0
            )
            
        except (ValueError, Exception):
            # Return empty response on error
            return ClubPlayersResponse(
                club_name=club_name,
                players=[],
                total_players=0
            )
    
    @kernel_function(
        name="get_club_recent_games",
        description="Get recent match results and statistics for a specific club. Shows date, opponent, result, goals, xG, possession, and formation. Essential for analyzing form and patterns."
    )
    def get_club_recent_games(
        self,
        club_name: Annotated[str, "Name of the club (e.g., 'Arsenal', 'Liverpool')"],
        epl_only: Annotated[bool, "If True, only return Premier League games. Default is True."] = True
    ) -> ClubGamesResponse:
        """Get recent games for a specific club.
        
        Returns structured response with recent matches including results, scores, xG,
        possession, formations, and other match details.
        """
        try:
            games = self._fbref.get_club_games(club_name, epl_only=epl_only)
            
            # Get last 10 games (most recent first)
            recent_games = games[-10:] if len(games) > 10 else games
            recent_games = list(reversed(recent_games)) if recent_games else []
            
            return ClubGamesResponse(
                club_name=club_name,
                epl_only=epl_only,
                games=recent_games,
                total_games=len(recent_games)
            )
            
        except (ValueError, Exception):
            # Return empty response on error
            return ClubGamesResponse(
                club_name=club_name,
                epl_only=epl_only,
                games=[],
                total_games=0
            )
    
    @kernel_function(
        name="compare_clubs",
        description="Compare two clubs' statistics side by side. Useful for head-to-head analysis before a match."
    )
    def compare_clubs(
        self,
        club1: Annotated[str, "First club name"],
        club2: Annotated[str, "Second club name"]
    ) -> ClubComparisonResponse:
        """Compare two clubs' season statistics.
        
        Returns structured response with side-by-side comparison of key metrics.
        """
        try:
            clubs = self._fbref.get_premier_league_stats()
            
            club1_data = None
            club2_data = None
            
            for club in clubs:
                if club1.lower() in club.team.lower():
                    club1_data = club
                if club2.lower() in club.team.lower():
                    club2_data = club
            
            if not club1_data or not club2_data:
                # Return empty comparison if clubs not found
                empty_metrics = ClubComparisonMetrics(
                    club_name="",
                    rank=0,
                    points=0,
                    goal_difference="",
                    goals_for=0,
                    goals_against=0,
                    xg_for=0.0,
                    xg_against=0.0,
                    xg_diff_per90=0.0,
                    form="",
                    top_scorers=""
                )
                return ClubComparisonResponse(
                    club1=empty_metrics,
                    club2=empty_metrics
                )
            
            club1_metrics = ClubComparisonMetrics(
                club_name=club1_data.team,
                rank=club1_data.rank,
                points=club1_data.points,
                goal_difference=club1_data.goal_diff,
                goals_for=club1_data.goals_for,
                goals_against=club1_data.goals_against,
                xg_for=club1_data.xg_for,
                xg_against=club1_data.xg_against,
                xg_diff_per90=club1_data.xg_diff_per90,
                form=club1_data.last_5,
                top_scorers=club1_data.top_team_scorers
            )
            
            club2_metrics = ClubComparisonMetrics(
                club_name=club2_data.team,
                rank=club2_data.rank,
                points=club2_data.points,
                goal_difference=club2_data.goal_diff,
                goals_for=club2_data.goals_for,
                goals_against=club2_data.goals_against,
                xg_for=club2_data.xg_for,
                xg_against=club2_data.xg_against,
                xg_diff_per90=club2_data.xg_diff_per90,
                form=club2_data.last_5,
                top_scorers=club2_data.top_team_scorers
            )
            
            return ClubComparisonResponse(
                club1=club1_metrics,
                club2=club2_metrics
            )
            
        except Exception:
            # Return empty comparison on error
            empty_metrics = ClubComparisonMetrics(
                club_name="",
                rank=0,
                points=0,
                goal_difference="",
                goals_for=0,
                goals_against=0,
                xg_for=0.0,
                xg_against=0.0,
                xg_diff_per90=0.0,
                form="",
                top_scorers=""
            )
            return ClubComparisonResponse(
                club1=empty_metrics,
                club2=empty_metrics
            )
