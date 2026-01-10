"""Response models for Semantic Kernel plugin functions.

These models provide structured, semantically annotated responses
instead of formatted strings, enabling better type safety and LLM understanding.
"""

from typing import Annotated, List, Optional
from pydantic import Field
from .base_model import FrozenBaseModel
from .search_result import TextSearchResult, NewsSearchResult
from .club import Club
from .player import Player
from .game import Game
from .upcoming_game import UpcomingGame
from .bookmaker_event import BookmakerEvent


# ============================================================================
# WebSearchPlugin Response Models
# ============================================================================

class FootballNewsSearchResponse(FrozenBaseModel):
    """Response from searching football news on major sports sites.
    
    Contains search query metadata and a list of relevant news articles
    from trusted football sources (BBC, Sky Sports, The Guardian, etc.).
    """
    
    query: Annotated[str, Field(..., description="The search query that was executed")]
    timelimit: Annotated[str, Field(..., description="Time limit filter applied (d/w/m/y)")]
    results: Annotated[List[TextSearchResult], Field(..., description="List of football news search results")]
    result_count: Annotated[int, Field(..., description="Number of results returned")]
    


class GeneralNewsSearchResponse(FrozenBaseModel):
    """Response from searching general news articles.
    
    Contains search query metadata and a list of news articles
    useful for injury reports, transfer news, team news, and insider information.
    """
    
    query: Annotated[str, Field(..., description="The search query that was executed")]
    timelimit: Annotated[str, Field(..., description="Time limit filter applied (d/w/m/y)")]
    results: Annotated[List[NewsSearchResult], Field(..., description="List of news article results")]
    result_count: Annotated[int, Field(..., description="Number of results returned")]
    


class WebSearchResponse(FrozenBaseModel):
    """Response from general web search.
    
    Contains search query metadata and a list of web search results
    for broader football-related information beyond news articles.
    """
    
    query: Annotated[str, Field(..., description="The search query that was executed")]
    timelimit: Annotated[str, Field(..., description="Time limit filter applied (d/w/m/y)")]
    results: Annotated[List[TextSearchResult], Field(..., description="List of web search results")]
    result_count: Annotated[int, Field(..., description="Number of results returned")]
    


# ============================================================================
# FBrefPlugin Response Models
# ============================================================================

class LeagueStandingsResponse(FrozenBaseModel):
    """Response containing Premier League standings and statistics.
    
    Provides complete league table with all teams, their positions,
    match statistics, goals, points, xG metrics, and recent form.
    """
    
    season: Annotated[str, Field(..., description="Season identifier (e.g., '2025-26')")]
    competition: Annotated[str, Field(default="Premier League", description="Competition name")]
    clubs: Annotated[List[Club], Field(..., description="List of clubs with their league statistics")]
    total_clubs: Annotated[int, Field(..., description="Total number of clubs in the standings")]
    


class ClubPlayersResponse(FrozenBaseModel):
    """Response containing player statistics for a specific club.
    
    Provides detailed player data including appearances, goals, assists,
    xG metrics, and per-90 statistics for all players in the squad.
    """
    
    club_name: Annotated[str, Field(..., description="Name of the club")]
    players: Annotated[List[Player], Field(..., description="List of players with their statistics")]
    total_players: Annotated[int, Field(..., description="Total number of players returned")]
    


class ClubGamesResponse(FrozenBaseModel):
    """Response containing match history for a specific club.
    
    Provides recent game results with scores, xG, possession,
    formations, and other match details.
    """
    
    club_name: Annotated[str, Field(..., description="Name of the club")]
    epl_only: Annotated[bool, Field(..., description="Whether only Premier League games were requested")]
    games: Annotated[List[Game], Field(..., description="List of games/matches")]
    total_games: Annotated[int, Field(..., description="Total number of games returned")]


class ClubComparisonMetrics(FrozenBaseModel):
    """Comparison metrics for a single club in a head-to-head comparison."""
    
    club_name: Annotated[str, Field(..., description="Club name")]
    rank: Annotated[int, Field(..., description="League position")]
    points: Annotated[int, Field(..., description="Total points")]
    goal_difference: Annotated[str, Field(..., description="Goal difference (e.g., '+26')")]
    goals_for: Annotated[int, Field(..., description="Goals scored")]
    goals_against: Annotated[int, Field(..., description="Goals conceded")]
    xg_for: Annotated[float, Field(..., description="Expected goals for")]
    xg_against: Annotated[float, Field(..., description="Expected goals against")]
    xg_diff_per90: Annotated[float, Field(..., description="xG difference per 90 minutes")]
    form: Annotated[str, Field(..., description="Recent form (last 5 games, e.g., 'WWWWD')")]
    top_scorers: Annotated[str, Field(..., description="Top team scorers")]
    


class ClubComparisonResponse(FrozenBaseModel):
    """Response containing head-to-head comparison between two clubs.
    
    Provides side-by-side statistical comparison of two teams
    including league position, points, goals, xG, and form.
    """
    
    club1: Annotated[ClubComparisonMetrics, Field(..., description="Statistics for the first club")]
    club2: Annotated[ClubComparisonMetrics, Field(..., description="Statistics for the second club")]
    


# ============================================================================
# BetclicPlugin Response Models
# ============================================================================

class UpcomingMatchesResponse(FrozenBaseModel):
    """Response containing upcoming Premier League matches with odds.
    
    Provides list of scheduled matches with basic 1X2 odds
    (home win, draw, away win) and match metadata.
    """
    
    matches: Annotated[List[UpcomingGame], Field(..., description="List of upcoming matches")]
    total_matches: Annotated[int, Field(..., description="Total number of matches")]
    


class MatchBettingMarketsResponse(FrozenBaseModel):
    """Response containing all betting markets for a specific match.
    
    Provides comprehensive list of available betting markets including
    over/under goals, both teams to score, exact score, handicaps,
    goalscorer markets, and more.
    """
    
    match_url: Annotated[str, Field(..., description="URL of the match")]
    events: Annotated[List[BookmakerEvent], Field(..., description="List of betting market events")]
    total_events: Annotated[int, Field(..., description="Total number of betting markets")]
    


class MatchUrlResponse(FrozenBaseModel):
    """Response containing match URL and basic information.
    
    Provides the betting URL for a match found by team names,
    along with basic match details and odds.
    """
    
    home_team: Annotated[str, Field(..., description="Home team name")]
    away_team: Annotated[str, Field(..., description="Away team name")]
    date: Annotated[str, Field(..., description="Match date header")]
    time: Annotated[str, Field(..., description="Match start time")]
    url: Annotated[str, Field(..., description="Betting URL for the match")]
    home_odds: Annotated[Optional[float], Field(None, description="Home team win odds")]
    draw_odds: Annotated[Optional[float], Field(None, description="Draw odds")]
    away_odds: Annotated[Optional[float], Field(None, description="Away team win odds")]
    


class ValueBetOpportunity(FrozenBaseModel):
    """Represents a potential value betting opportunity.
    
    Identifies bets where odds may be mispriced relative to
    the true probability of the outcome.
    """
    
    market: Annotated[str, Field(..., description="Betting market name (e.g., 'Over/Under 2.5 Goals')")]
    selection: Annotated[str, Field(..., description="Betting selection (e.g., 'Over 2.5')")]
    odds: Annotated[float, Field(..., description="Current odds for this selection")]
    implied_probability: Annotated[float, Field(..., description="Implied probability from odds (as percentage)")]
    event_type: Annotated[str, Field(..., description="Type of betting event")]
    


class ValueBetsResponse(FrozenBaseModel):
    """Response containing value bet analysis for a match.
    
    Provides analysis of betting markets to identify potential
    value opportunities where odds may be mispriced.
    """
    
    match_url: Annotated[str, Field(..., description="URL of the analyzed match")]
    opportunities: Annotated[List[ValueBetOpportunity], Field(..., description="List of identified value bet opportunities")]
    total_opportunities: Annotated[int, Field(..., description="Total number of value opportunities found")]
