from typing import Annotated, Optional
from pydantic import Field
from .base_model import FrozenBaseModel


class Game(FrozenBaseModel):
    """Represents a game/match with its statistics."""
    
    date: Annotated[str, Field(..., description="Match date (e.g., '2025-08-16')")]
    start_time: Annotated[str, Field(..., description="Match start time (e.g., '17:30')")]
    comp: Annotated[str, Field(..., description="Competition name (e.g., 'Premier League', 'Champions Lg')")]
    round: Annotated[str, Field(..., description="Round or matchweek (e.g., 'Matchweek 1', 'League phase')")]
    dayofweek: Annotated[str, Field(..., description="Day of week (e.g., 'Sat', 'Sun')")]
    venue: Annotated[str, Field(..., description="Venue (e.g., 'Home', 'Away')")]
    result: Annotated[Optional[str], Field(None, description="Match result (W/L/D)")]
    goals_for: Annotated[Optional[int], Field(None, description="Goals scored by the club")]
    goals_against: Annotated[Optional[int], Field(None, description="Goals conceded by the club")]
    opponent: Annotated[str, Field(..., description="Opponent team name")]
    xg_for: Annotated[Optional[float], Field(None, description="Expected goals for")]
    xg_against: Annotated[Optional[float], Field(None, description="Expected goals against")]
    possession: Annotated[Optional[int], Field(None, description="Possession percentage")]
    attendance: Annotated[Optional[int], Field(None, description="Match attendance")]
    captain: Annotated[Optional[str], Field(None, description="Team captain name")]
    formation: Annotated[Optional[str], Field(None, description="Team formation (e.g., '4-3-3')")]
    opp_formation: Annotated[Optional[str], Field(None, description="Opponent formation (e.g., '3-4-3')")]
    referee: Annotated[Optional[str], Field(None, description="Referee name")]
    notes: Annotated[Optional[str], Field(None, description="Additional notes")]

