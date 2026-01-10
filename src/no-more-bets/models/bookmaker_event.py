from typing import Annotated, List
from pydantic import Field
from .base_model import FrozenBaseModel


class EventOption(FrozenBaseModel):
    """Represents a single betting option within an event."""
    
    label: Annotated[str, Field(..., description="Option description (e.g., 'Tak', 'Powyżej 2,5')")]
    odds: Annotated[float, Field(..., description="Betting odds as float (e.g., 1.48)")]


class BookmakerEvent(FrozenBaseModel):
    """Base model for all bookmaker match events."""
    
    event_type: Annotated[str, Field(..., description="Event type identifier (e.g., 'both_teams_score', 'over_under')")]
    title: Annotated[str, Field(..., description="Human-readable event name from HTML")]
    options: Annotated[List[EventOption], Field(..., description="Available betting options")]
 