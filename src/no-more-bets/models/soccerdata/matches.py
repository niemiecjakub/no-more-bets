from typing import Annotated, List, Optional
from pydantic import Field
from ..base_model import FrozenBaseModel
from .soccerdata_common import CountryInfo, Teams


class Player(FrozenBaseModel):
    """Represents a player."""
    
    id: Annotated[int, Field(..., description="Player ID")]
    name: Annotated[str, Field(..., description="Player name")]


class Goals(FrozenBaseModel):
    """Represents match goals at different stages."""
    
    home_ht_goals: Annotated[int, Field(..., description="Home team half-time goals")]
    away_ht_goals: Annotated[int, Field(..., description="Away team half-time goals")]
    home_ft_goals: Annotated[int, Field(..., description="Home team full-time goals")]
    away_ft_goals: Annotated[int, Field(..., description="Away team full-time goals")]
    home_et_goals: Annotated[int, Field(..., description="Home team extra-time goals (-1 if not applicable)")]
    away_et_goals: Annotated[int, Field(..., description="Away team extra-time goals (-1 if not applicable)")]
    home_pen_goals: Annotated[int, Field(..., description="Home team penalty goals (-1 if not applicable)")]
    away_pen_goals: Annotated[int, Field(..., description="Away team penalty goals (-1 if not applicable)")]


class MatchPreviewInfo(FrozenBaseModel):
    """Represents match preview information."""
    
    has_preview: Annotated[bool, Field(..., description="Whether a preview exists")]
    word_count: Annotated[int, Field(..., description="Word count of the preview (-1 if not available)")]


class MatchWinnerOdds(FrozenBaseModel):
    """Represents match winner odds."""
    
    home: Annotated[Optional[float], Field(default=None, description="Home team win odds")]
    draw: Annotated[Optional[float], Field(default=None, description="Draw odds")]
    away: Annotated[Optional[float], Field(default=None, description="Away team win odds")]


class OverUnderOdds(FrozenBaseModel):
    """Represents over/under odds."""
    
    total: Annotated[Optional[float], Field(default=None, description="Total goals threshold")]
    over: Annotated[Optional[float], Field(default=None, description="Over odds")]
    under: Annotated[Optional[float], Field(default=None, description="Under odds")]


class HandicapOdds(FrozenBaseModel):
    """Represents handicap odds."""
    
    market: Annotated[Optional[float], Field(default=None, description="Handicap market value")]
    home: Annotated[Optional[float], Field(default=None, description="Home team handicap odds")]
    away: Annotated[Optional[float], Field(default=None, description="Away team handicap odds")]


class Odds(FrozenBaseModel):
    """Represents match odds."""
    
    match_winner: Annotated[MatchWinnerOdds, Field(..., description="Match winner odds (can be empty dict)")]
    over_under: Annotated[OverUnderOdds, Field(..., description="Over/under odds (can be empty dict)")]
    handicap: Annotated[HandicapOdds, Field(..., description="Handicap odds (can be empty dict)")]
    last_modified_timestamp: Annotated[Optional[int], Field(default=None, description="Last modified timestamp")]


class MatchEvent(FrozenBaseModel):
    """Represents a match event (goal, card, substitution, etc.)."""
    
    event_type: Annotated[str, Field(..., description="Event type (e.g., 'goal', 'yellow_card', 'red_card', 'substitution', 'penalty_goal')")]
    event_minute: Annotated[str, Field(..., description="Event minute as string")]
    team: Annotated[str, Field(..., description="Team side ('home' or 'away')")]
    player: Annotated[Optional[Player], Field(None, description="Player involved in the event")]
    assist_player: Annotated[Optional[Player], Field(None, description="Assisting player (for goals)")]
    player_in: Annotated[Optional[Player], Field(None, description="Player coming in (for substitutions)")]
    player_out: Annotated[Optional[Player], Field(None, description="Player going out (for substitutions)")]


class Match(FrozenBaseModel):
    """Represents a single match."""
    
    id: Annotated[int, Field(..., description="Match ID")]
    date: Annotated[str, Field(..., description="Match date (e.g., '15/08/2025')")]
    time: Annotated[str, Field(..., description="Match time (e.g., '12:00')")]
    teams: Annotated[Teams, Field(..., description="Home and away teams")]
    status: Annotated[str, Field(..., description="Match status (e.g., 'pre-match', 'finished', 'live')")]
    minute: Annotated[int, Field(..., description="Current minute (-1 if not applicable)")]
    winner: Annotated[str, Field(..., description="Match winner ('home', 'away', 'draw', 'tbd')")]
    has_extra_time: Annotated[bool, Field(..., description="Whether the match has extra time")]
    has_penalties: Annotated[bool, Field(..., description="Whether the match has penalties")]
    goals: Annotated[Goals, Field(..., description="Match goals")]
    events: Annotated[List[MatchEvent], Field(..., description="Match events")]
    odds: Annotated[Odds, Field(..., description="Match odds")]
    match_preview: Annotated[MatchPreviewInfo, Field(..., description="Match preview information")]


class Stage(FrozenBaseModel):
    """Represents a stage with its matches."""
    
    stage_id: Annotated[int, Field(..., description="Stage ID")]
    stage_name: Annotated[str, Field(..., description="Stage name")]
    is_active: Annotated[bool, Field(..., description="Whether the stage is active")]
    matches: Annotated[List[Match], Field(..., description="List of matches in this stage")]


class Season(FrozenBaseModel):
    """Represents season information."""
    
    is_active: Annotated[bool, Field(..., description="Whether the season is active")]
    year: Annotated[str, Field(..., description="Season year (e.g., '2025-2026')")]


class LeagueMatches(FrozenBaseModel):
    """Represents matches grouped by league."""
    
    league_id: Annotated[int, Field(..., description="League ID")]
    league_name: Annotated[str, Field(..., description="League name")]
    country: Annotated[CountryInfo, Field(..., description="Country information")]
    is_cup: Annotated[bool, Field(..., description="Whether this is a cup competition")]
    season: Annotated[Season, Field(..., description="Season information")]
    stage: Annotated[List[Stage], Field(..., description="List of stages with matches")]
