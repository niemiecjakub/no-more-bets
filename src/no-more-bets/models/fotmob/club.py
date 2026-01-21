from typing import Annotated, Optional
from pydantic import Field
from ..base_model import FrozenBaseModel


class Club(FrozenBaseModel):
    """Represents a club entry in the FotMob Premier League table."""
    
    position: Annotated[int, Field(..., description="League position/rank")]
    team_name: Annotated[str, Field(..., description="Full team name")]
    team_shortname: Annotated[str, Field(..., description="Short team name")]
    team_id: Annotated[int, Field(..., description="Team ID extracted from URL")]
    team_logo_url: Annotated[str, Field(..., description="Team logo image URL")]
    matches_played: Annotated[int, Field(..., description="Number of matches played")]
    wins: Annotated[int, Field(..., description="Number of wins")]
    draws: Annotated[int, Field(..., description="Number of draws")]
    losses: Annotated[int, Field(..., description="Number of losses")]
    goals_for: Annotated[int, Field(..., description="Goals scored")]
    goals_against: Annotated[int, Field(..., description="Goals conceded")]
    goal_difference: Annotated[str, Field(..., description="Goal difference as string (e.g., '+26', '-5')")]
    points: Annotated[int, Field(..., description="Total points")]
    form: Annotated[str, Field(..., description="Last 5 results (e.g., 'WWWDD' or 'ZZZRR')")]
    next_opponent_id: Annotated[Optional[int], Field(None, description="Next opponent team ID")]
    next_opponent_name: Annotated[Optional[str], Field(None, description="Next opponent team name")]
    next_opponent_logo_url: Annotated[Optional[str], Field(None, description="Next opponent logo URL")]
