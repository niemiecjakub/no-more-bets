"""Bookmaker event models for match betting events."""

from typing import List, Optional, Dict, Any
from pydantic import BaseModel, Field


class EventOption(BaseModel):
    """Represents a single betting option within an event."""
    
    label: str = Field(..., description="Option description (e.g., 'Tak', 'Powyżej 2,5')")
    odds: float = Field(..., description="Betting odds as float (e.g., 1.48)")
    
    class Config:
        """Pydantic configuration."""
        frozen = True


class BookmakerEvent(BaseModel):
    """Base model for all bookmaker match events.
    
    All event types inherit from this model to enable aggregation into a single list.
    Uses a generic structure with event_type as discriminator and flexible metadata.
    """
    
    event_type: str = Field(..., description="Event type identifier (e.g., 'both_teams_score', 'over_under')")
    title: str = Field(..., description="Human-readable event name from HTML")
    options: List[EventOption] = Field(..., description="Available betting options")
    metadata: Optional[Dict[str, Any]] = Field(
        None, 
        description="Additional structured data (team names, thresholds, handicap values, etc.)"
    )
    
    class Config:
        """Pydantic configuration."""
        frozen = True
