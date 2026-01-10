"""Betclic Plugin for Semantic Kernel agents."""

import sys
import os
from typing import Annotated

# Add parent directory to path for imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from semantic_kernel.functions import kernel_function
from services.betclic import Betclic
from models import (
    UpcomingMatchesResponse,
    MatchBettingMarketsResponse,
    MatchUrlResponse,
    ValueBetsResponse,
    ValueBetOpportunity,
)


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
    def get_upcoming_matches(self) -> UpcomingMatchesResponse:
        """Get upcoming Premier League matches with basic odds.
        
        Returns structured response with upcoming matches including date, time, teams,
        and 1X2 (home/draw/away) odds.
        """
        try:
            games = self._betclic.get_upcoming_games()
            
            return UpcomingMatchesResponse(
                matches=games if games else [],
                total_matches=len(games) if games else 0
            )
            
        except Exception:
            return UpcomingMatchesResponse(
                matches=[],
                total_matches=0
            )
    
    @kernel_function(
        name="get_match_betting_options",
        description="Get all available betting markets for a specific match including over/under goals, both teams to score, exact score, handicaps, and goalscorer markets. Requires the match URL from get_upcoming_matches."
    )
    def get_match_betting_options(
        self,
        match_url: Annotated[str, "URL of the match from get_upcoming_matches"]
    ) -> MatchBettingMarketsResponse:
        """Get detailed betting markets for a specific match.
        
        Returns structured response with all available betting markets including
        options and odds for each market.
        """
        try:
            events = self._betclic.get_match_events(match_url, expand=False)
            
            return MatchBettingMarketsResponse(
                match_url=match_url,
                events=events if events else [],
                total_events=len(events) if events else 0
            )
            
        except Exception:
            return MatchBettingMarketsResponse(
                match_url=match_url,
                events=[],
                total_events=0
            )
    
    @kernel_function(
        name="find_match_url",
        description="Find the betting URL for a specific match by team names. Returns the URL that can be used with get_match_betting_options."
    )
    def find_match_url(
        self,
        team1: Annotated[str, "First team name (partial match supported)"],
        team2: Annotated[str, "Second team name (partial match supported)"]
    ) -> MatchUrlResponse:
        """Find the betting URL for a match between two teams.
        
        Searches upcoming matches for a match involving both specified teams.
        Returns structured response with match details and URL.
        """
        try:
            games = self._betclic.get_upcoming_games()
            
            if not games:
                return MatchUrlResponse(
                    home_team="",
                    away_team="",
                    date="",
                    time="",
                    url="",
                    home_odds=None,
                    draw_odds=None,
                    away_odds=None
                )
            
            team1_lower = team1.lower()
            team2_lower = team2.lower()
            
            for game in games:
                home_lower = game.home_team.lower()
                away_lower = game.away_team.lower()
                
                # Check if both teams match (in either order)
                match1 = team1_lower in home_lower or team1_lower in away_lower
                match2 = team2_lower in home_lower or team2_lower in away_lower
                
                if match1 and match2:
                    return MatchUrlResponse(
                        home_team=game.home_team,
                        away_team=game.away_team,
                        date=game.date,
                        time=game.time,
                        url=game.url,
                        home_odds=game.home_odds,
                        draw_odds=game.draw_odds,
                        away_odds=game.away_odds
                    )
            
            # No match found
            return MatchUrlResponse(
                home_team="",
                away_team="",
                date="",
                time="",
                url="",
                home_odds=None,
                draw_odds=None,
                away_odds=None
            )
            
        except Exception:
            return MatchUrlResponse(
                home_team="",
                away_team="",
                date="",
                time="",
                url="",
                home_odds=None,
                draw_odds=None,
                away_odds=None
            )
    
    @kernel_function(
        name="get_value_bets",
        description="Analyze a match for potential value bets by comparing odds implied probabilities. Higher value scores indicate potentially mispriced odds."
    )
    def get_value_bets(
        self,
        match_url: Annotated[str, "URL of the match to analyze"]
    ) -> ValueBetsResponse:
        """Analyze betting markets for potential value opportunities.
        
        Returns structured response identifying markets where odds might offer value
        based on implied probabilities (odds between 2.0 and 10.0).
        """
        try:
            events = self._betclic.get_match_events(match_url, expand=False)
            
            if not events:
                return ValueBetsResponse(
                    match_url=match_url,
                    opportunities=[],
                    total_opportunities=0
                )
            
            opportunities = []
            
            for event in events:
                for option in event.options:
                    implied_prob = (1 / option.odds) * 100
                    
                    # Flag bets with odds between 2.0 and 10.0 as potential value
                    if 2.0 <= option.odds <= 10.0:
                        opportunities.append(ValueBetOpportunity(
                            market=event.title,
                            selection=option.label,
                            odds=option.odds,
                            implied_probability=implied_prob,
                            event_type=event.event_type
                        ))
            
            # Sort by odds descending
            opportunities.sort(key=lambda x: x.odds, reverse=True)
            
            # Limit to top 15
            opportunities = opportunities[:15]
            
            return ValueBetsResponse(
                match_url=match_url,
                opportunities=opportunities,
                total_opportunities=len(opportunities)
            )
            
        except Exception:
            return ValueBetsResponse(
                match_url=match_url,
                opportunities=[],
                total_opportunities=0
            )
