from typing import Annotated, Optional
from pydantic import Field
from ..base_model import FrozenBaseModel


class Club(FrozenBaseModel):
    """Represents a club with its statistics."""
    
    rank: Annotated[int, Field(..., description="League position/rank")]
    team: Annotated[str, Field(..., description="Team name")]
    games: Annotated[int, Field(..., description="Number of games played")]
    wins: Annotated[int, Field(..., description="Number of wins")]
    ties: Annotated[int, Field(..., description="Number of ties/draws")]
    losses: Annotated[int, Field(..., description="Number of losses")]
    goals_for: Annotated[int, Field(..., description="Goals scored")]
    goals_against: Annotated[int, Field(..., description="Goals conceded")]
    goal_diff: Annotated[str, Field(..., description="Goal difference (as string, e.g., '+26')")]
    points: Annotated[int, Field(..., description="Total points")]
    points_avg: Annotated[float, Field(..., description="Average points per game")]
    xg_for: Annotated[float, Field(..., description="Expected goals for")]
    xg_against: Annotated[float, Field(..., description="Expected goals against")]
    xg_diff: Annotated[float, Field(..., description="Expected goal difference")]
    xg_diff_per90: Annotated[float, Field(..., description="Expected goal difference per 90 minutes")]
    last_5: Annotated[str, Field(..., description="Form in last 5 games (e.g., 'WWWWD')")]
    attendance_per_g: Annotated[str, Field(..., description="Average attendance per game")]
    top_team_scorers: Annotated[str, Field(..., description="Top team scorers")]
    top_keeper: Annotated[str, Field(..., description="Top goalkeeper")]
    notes: Annotated[Optional[str], Field(None, description="Additional notes")]
