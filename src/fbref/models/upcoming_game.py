"""Upcoming game model for BetClick scraper."""

from typing import Optional
from pydantic import BaseModel, Field


class UpcomingGame(BaseModel):
    """Represents an upcoming game/match from BetClick."""
    
    date: str = Field(..., description="Match date header (e.g., 'Sob. 17/01')")
    home_team: str = Field(..., description="Home team name")
    away_team: str = Field(..., description="Away team name")
    time: str = Field(..., description="Match start time (e.g., '13:30')")
    home_odds: Optional[float] = Field(None, description="Home team win odds")
    draw_odds: Optional[float] = Field(None, description="Draw odds")
    away_odds: Optional[float] = Field(None, description="Away team win odds")
    url: str = Field(..., description="BetClick URL for the match")
    
    class Config:
        """Pydantic configuration."""
        frozen = True
