from typing import Annotated
from semantic_kernel.functions import kernel_function
from services.betclic import Betclic
from models.betclic import BookmakerEvent, UpcomingGame
from typing import List


class BetclicPlugin:
    """Plugin for accessing betting odds and markets from Betclic.
    
    Provides functions for agents to retrieve upcoming matches with odds
    and detailed betting markets for specific matches.
    """
    
    def __init__(self):
        """Initialize the BetclicPlugin with a Betclic scraper instance."""
        self._betclic = Betclic(delay=5.0, use_cache=True, cache_ttl=1800.0)
    
    @kernel_function(
        name="get_upcoming_matches",
        description="Get list of upcoming Premier League matches with basic 1X2 odds (home win, draw, away win). Returns match dates, times, teams, and odds."
    )
    def get_upcoming_matches(self) -> List[UpcomingGame]:
        """Get upcoming Premier League matches with basic odds.
        
        Returns structured response with upcoming matches including date, time, teams,
        and 1X2 (home/draw/away) odds.
        """
        return self._betclic.get_upcoming_games()

    
    @kernel_function(
        name="get_match_betting_options",
        description="Get all available betting markets for a specific match including over/under goals, both teams to score, exact score, handicaps, and goalscorer markets. Requires the match URL from get_upcoming_matches."
    )
    def get_match_betting_options(
        self,
        match_url: Annotated[str, "URL of the match from get_upcoming_matches"]
    ) -> List[BookmakerEvent]:
        """Get detailed betting markets for a specific match.
        
        Returns structured response with all available betting markets including
        options and odds for each market.
        """

        return self._betclic.get_match_events(match_url, expand=False)

    
    @kernel_function(
        name="find_match_url",
        description="Find the betting URL for a specific match by team names. Returns the URL that can be used with get_match_betting_options."
    )
    def find_match_url(
        self,
        team1: Annotated[str, "First team name"],
        team2: Annotated[str, "Second team name"]
    ) -> str:
        """Find the betting URL for a match between two teams.
        
        Searches upcoming matches for a match involving both specified teams.
        Returns the URL of the match.
        """
        games = self._betclic.get_upcoming_games()
        
        team1_lower = team1.lower()
        team2_lower = team2.lower()
            
        for game in games:
            home_lower = game.home_team.lower()
            away_lower = game.away_team.lower()
                
            match1 = team1_lower in home_lower or team1_lower in away_lower
            match2 = team2_lower in home_lower or team2_lower in away_lower
                
            if match1 and match2:
                return game.url
