"""Player model for club player statistics."""

from typing import Optional
from pydantic import BaseModel, Field


class Player(BaseModel):
    """Represents a player with their statistics from a club's squad page."""
    
    player: str = Field(..., description="Player name")
    nationality: str = Field(..., description="Player nationality code")
    position: str = Field(..., description="Player position (e.g., FW, MF, DF, GK)")
    age: str = Field(..., description="Player age (format: YY-DDD)")
    games: int = Field(..., description="Number of games played")
    games_starts: int = Field(..., description="Number of games started")
    minutes: int = Field(..., description="Total minutes played")
    minutes_90s: float = Field(..., description="90-minute intervals played")
    goals: int = Field(..., description="Goals scored")
    assists: int = Field(..., description="Assists")
    goals_assists: int = Field(..., description="Goals + assists")
    goals_pens: int = Field(..., description="Goals from penalties")
    pens_made: int = Field(..., description="Penalties made")
    pens_att: int = Field(..., description="Penalties attempted")
    cards_yellow: int = Field(..., description="Yellow cards")
    cards_red: int = Field(..., description="Red cards")
    xg: float = Field(..., description="Expected goals")
    npxg: float = Field(..., description="Non-penalty expected goals")
    xg_assist: float = Field(..., description="Expected assists")
    npxg_xg_assist: float = Field(..., description="Non-penalty xG + xA")
    progressive_carries: int = Field(..., description="Progressive carries")
    progressive_passes: int = Field(..., description="Progressive passes")
    progressive_passes_received: int = Field(..., description="Progressive passes received")
    goals_per90: float = Field(..., description="Goals per 90 minutes")
    assists_per90: float = Field(..., description="Assists per 90 minutes")
    goals_assists_per90: float = Field(..., description="Goals + assists per 90 minutes")
    goals_pens_per90: float = Field(..., description="Penalty goals per 90 minutes")
    goals_assists_pens_per90: float = Field(..., description="Goals + assists (excluding pens) per 90 minutes")
    xg_per90: float = Field(..., description="Expected goals per 90 minutes")
    xg_assist_per90: float = Field(..., description="Expected assists per 90 minutes")
    xg_xg_assist_per90: float = Field(..., description="xG + xA per 90 minutes")
    npxg_per90: float = Field(..., description="Non-penalty xG per 90 minutes")
    npxg_xg_assist_per90: float = Field(..., description="Non-penalty xG + xA per 90 minutes")
    
    class Config:
        """Pydantic configuration."""
        frozen = True

