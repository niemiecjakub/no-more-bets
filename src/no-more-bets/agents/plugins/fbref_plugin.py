from typing import Annotated
from semantic_kernel.functions import kernel_function
from services.fbref import FBref
from models.fbref import Club, Player, Game


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
    def get_league_standings(self) -> list[Club]:
        """Get Premier League standings with detailed statistics.
        
        Returns structured response with all clubs and their league statistics.
        """
        return self._fbref.get_premier_league_stats()

    
    @kernel_function(
        name="get_club_players",
        description="Get detailed player statistics for a specific club including goals, assists, xG, minutes played, and per-90 metrics. Use to analyze key players and their contributions."
    )
    def get_club_players(
        self,
        club_name: Annotated[str, "Name of the club (e.g., 'Arsenal', 'Liverpool', 'Manchester City')"]
    ) -> list[Player]:
        """Get player statistics for a specific club.
        
        Returns structured response with all players and their statistics.
        """
        return self._fbref.get_club_players(club_name)

    
    @kernel_function(
        name="get_club_recent_games",
        description="Get recent match results and statistics for a specific club. Shows date, opponent, result, goals, xG, possession, and formation. Essential for analyzing form and patterns."
    )
    def get_club_recent_games(
        self,
        club_name: Annotated[str, "Name of the club (e.g., 'Arsenal', 'Liverpool')"],
        epl_only: Annotated[bool, "If True, only return Premier League games. Default is True."] = True
    ) -> list[Game]:
        """Get 5 recent games for a specific club.
        
        Returns structured response with recent matches including results, scores, xG,
        possession, formations, and other match details.
        """

        games = self._fbref.get_club_games(club_name, epl_only=epl_only)
        games_with_result = [game for game in games if getattr(game, "result", None) is not None]
        games_sorted = sorted(games_with_result, key=lambda x: getattr(x, "date", None), reverse=True)
        return games_sorted[:5]

    
    @kernel_function(
        name="compare_clubs",
        description="Compare two clubs' statistics side by side. Useful for head-to-head analysis before a match."
    )
    def compare_clubs(
        self,
        club1: Annotated[str, "First club name"],
        club2: Annotated[str, "Second club name"]
    ) -> list[Club]:
        """Compare two clubs' season statistics.
        
        Returns structured response with side-by-side comparison of key metrics.
        """
        clubs = self._fbref.get_premier_league_stats()
        club1_stats = next((club for club in clubs if club.name == club1), None)
        club2_stats = next((club for club in clubs if club.name == club2), None)

        return [club1_stats, club2_stats]

