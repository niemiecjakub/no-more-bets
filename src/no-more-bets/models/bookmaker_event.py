from typing import List
from pydantic import BaseModel, Field


class EventOption(BaseModel):
    """Represents a single betting option within an event."""
    
    label: str = Field(..., description="Option description (e.g., 'Tak', 'Powyżej 2,5')")
    odds: float = Field(..., description="Betting odds as float (e.g., 1.48)")
    
    class Config:
        frozen = True


class BookmakerEvent(BaseModel):
    """Base model for all bookmaker match events."""
    
    event_type: str = Field(..., description="Event type identifier (e.g., 'both_teams_score', 'over_under')")
    title: str = Field(..., description="Human-readable event name from HTML")
    options: List[EventOption] = Field(..., description="Available betting options")
    
    class Config:
        frozen = True
 