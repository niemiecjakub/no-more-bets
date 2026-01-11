from typing import Annotated
from pydantic import Field
from ..base_model import FrozenBaseModel


class Player(FrozenBaseModel):
    """Represents a player with their statistics from a club's squad page."""
    
    player: Annotated[str, Field(..., description="Player name")]
    nationality: Annotated[str, Field(..., description="Player nationality code")]
    position: Annotated[str, Field(..., description="Player position (e.g., FW, MF, DF, GK)")]
    age: Annotated[str, Field(..., description="Player age (format: YY-DDD)")]
    games: Annotated[int, Field(..., description="Number of games played")]
    games_starts: Annotated[int, Field(..., description="Number of games started")]
    minutes: Annotated[int, Field(..., description="Total minutes played")]
    minutes_90s: Annotated[float, Field(..., description="90-minute intervals played")]
    goals: Annotated[int, Field(..., description="Goals scored")]
    assists: Annotated[int, Field(..., description="Assists")]
    goals_assists: Annotated[int, Field(..., description="Goals + assists")]
    goals_pens: Annotated[int, Field(..., description="Goals from penalties")]
    pens_made: Annotated[int, Field(..., description="Penalties made")]
    pens_att: Annotated[int, Field(..., description="Penalties attempted")]
    cards_yellow: Annotated[int, Field(..., description="Yellow cards")]
    cards_red: Annotated[int, Field(..., description="Red cards")]
    xg: Annotated[float, Field(..., description="Expected goals")]
    npxg: Annotated[float, Field(..., description="Non-penalty expected goals")]
    xg_assist: Annotated[float, Field(..., description="Expected assists")]
    npxg_xg_assist: Annotated[float, Field(..., description="Non-penalty xG + xA")]
    progressive_carries: Annotated[int, Field(..., description="Progressive carries")]
    progressive_passes: Annotated[int, Field(..., description="Progressive passes")]
    progressive_passes_received: Annotated[int, Field(..., description="Progressive passes received")]
    goals_per90: Annotated[float, Field(..., description="Goals per 90 minutes")]
    assists_per90: Annotated[float, Field(..., description="Assists per 90 minutes")]
    goals_assists_per90: Annotated[float, Field(..., description="Goals + assists per 90 minutes")]
    goals_pens_per90: Annotated[float, Field(..., description="Penalty goals per 90 minutes")]
    goals_assists_pens_per90: Annotated[float, Field(..., description="Goals + assists (excluding pens) per 90 minutes")]
    xg_per90: Annotated[float, Field(..., description="Expected goals per 90 minutes")]
    xg_assist_per90: Annotated[float, Field(..., description="Expected assists per 90 minutes")]
    xg_xg_assist_per90: Annotated[float, Field(..., description="xG + xA per 90 minutes")]
    npxg_per90: Annotated[float, Field(..., description="Non-penalty xG per 90 minutes")]
    npxg_xg_assist_per90: Annotated[float, Field(..., description="Non-penalty xG + xA per 90 minutes")]
