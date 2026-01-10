from typing import Annotated, Optional
from pydantic import Field
from .base_model import FrozenBaseModel


class UpcomingGame(FrozenBaseModel):
    """Represents an upcoming game/match from BetClick."""
    
    date: Annotated[str, Field(..., description="Match date header (e.g., 'Sob. 17/01')")]
    home_team: Annotated[str, Field(..., description="Home team name")]
    away_team: Annotated[str, Field(..., description="Away team name")]
    time: Annotated[str, Field(..., description="Match start time (e.g., '13:30')")]
    home_odds: Annotated[Optional[float], Field(None, description="Home team win odds")]
    draw_odds: Annotated[Optional[float], Field(None, description="Draw odds")]
    away_odds: Annotated[Optional[float], Field(None, description="Away team win odds")]
    url: Annotated[str, Field(..., description="BetClick URL for the match")]
