"""Game model for club match statistics."""

from typing import Optional
from pydantic import BaseModel, Field


class Game(BaseModel):
    """Represents a game/match with its statistics."""
    
    date: str = Field(..., description="Match date (e.g., '2025-08-16')")
    start_time: str = Field(..., description="Match start time (e.g., '17:30')")
    comp: str = Field(..., description="Competition name (e.g., 'Premier League', 'Champions Lg')")
    round: str = Field(..., description="Round or matchweek (e.g., 'Matchweek 1', 'League phase')")
    dayofweek: str = Field(..., description="Day of week (e.g., 'Sat', 'Sun')")
    venue: str = Field(..., description="Venue (e.g., 'Home', 'Away')")
    result: Optional[str] = Field(None, description="Match result (W/L/D)")
    goals_for: Optional[int] = Field(None, description="Goals scored by the club")
    goals_against: Optional[int] = Field(None, description="Goals conceded by the club")
    opponent: str = Field(..., description="Opponent team name")
    xg_for: Optional[float] = Field(None, description="Expected goals for")
    xg_against: Optional[float] = Field(None, description="Expected goals against")
    possession: Optional[int] = Field(None, description="Possession percentage")
    attendance: Optional[int] = Field(None, description="Match attendance")
    captain: Optional[str] = Field(None, description="Team captain name")
    formation: Optional[str] = Field(None, description="Team formation (e.g., '4-3-3')")
    opp_formation: Optional[str] = Field(None, description="Opponent formation (e.g., '3-4-3')")
    referee: Optional[str] = Field(None, description="Referee name")
    notes: Optional[str] = Field(None, description="Additional notes")
    
    class Config:
        """Pydantic configuration."""
        frozen = True

