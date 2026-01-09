from typing import Optional
from pydantic import BaseModel, Field


class Club(BaseModel):
    """Represents a club with its statistics."""
    
    rank: int = Field(..., description="League position/rank")
    team: str = Field(..., description="Team name")
    games: int = Field(..., description="Number of games played")
    wins: int = Field(..., description="Number of wins")
    ties: int = Field(..., description="Number of ties/draws")
    losses: int = Field(..., description="Number of losses")
    goals_for: int = Field(..., description="Goals scored")
    goals_against: int = Field(..., description="Goals conceded")
    goal_diff: str = Field(..., description="Goal difference (as string, e.g., '+26')")
    points: int = Field(..., description="Total points")
    points_avg: float = Field(..., description="Average points per game")
    xg_for: float = Field(..., description="Expected goals for")
    xg_against: float = Field(..., description="Expected goals against")
    xg_diff: float = Field(..., description="Expected goal difference")
    xg_diff_per90: float = Field(..., description="Expected goal difference per 90 minutes")
    last_5: str = Field(..., description="Form in last 5 games (e.g., 'WWWWD')")
    attendance_per_g: str = Field(..., description="Average attendance per game")
    top_team_scorers: str = Field(..., description="Top team scorers")
    top_keeper: str = Field(..., description="Top goalkeeper")
    notes: Optional[str] = Field(None, description="Additional notes")
    
    class Config:
        frozen = True

