from typing import Annotated, List
from pydantic import Field
from .base_model import FrozenBaseModel


class EventOption(FrozenBaseModel):
    """Represents a single betting option within an event."""
    
    label: Annotated[str, Field(..., description="Option description")]
    odds: Annotated[float, Field(..., description="Bet odds")]


class BookmakerEvent(FrozenBaseModel):
    """Base model for all bookmaker match events."""
    
    title: Annotated[str, Field(..., description="Event name")]
    options: Annotated[List[EventOption], Field(..., description="Available bet options")]
 