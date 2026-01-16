from typing import Annotated
from pydantic import Field
from ..base_model import FrozenBaseModel
from .soccerdata_common import TeamInfo


class OverallStats(FrozenBaseModel):
    """Represents overall head-to-head statistics between two teams."""
    
    overall_games_played: Annotated[int, Field(..., description="Total number of games played between the teams")]
    overall_team1_wins: Annotated[int, Field(..., description="Number of wins for team 1")]
    overall_team2_wins: Annotated[int, Field(..., description="Number of wins for team 2")]
    overall_draws: Annotated[int, Field(..., description="Number of draws")]
    overall_team1_scored: Annotated[int, Field(..., description="Total goals scored by team 1")]
    overall_team2_scored: Annotated[int, Field(..., description="Total goals scored by team 2")]


class Team1AtHomeStats(FrozenBaseModel):
    """Represents statistics when team 1 plays at home."""
    
    team1_games_played_at_home: Annotated[int, Field(..., description="Number of games team 1 played at home")]
    team1_wins_at_home: Annotated[int, Field(..., description="Number of wins for team 1 at home")]
    team1_losses_at_home: Annotated[int, Field(..., description="Number of losses for team 1 at home")]
    team1_draws_at_home: Annotated[int, Field(..., description="Number of draws for team 1 at home")]
    team1_scored_at_home: Annotated[int, Field(..., description="Goals scored by team 1 at home")]
    team1_conceded_at_home: Annotated[int, Field(..., description="Goals conceded by team 1 at home")]


class Team2AtHomeStats(FrozenBaseModel):
    """Represents statistics when team 2 plays at home."""
    
    team2_games_played_at_home: Annotated[int, Field(..., description="Number of games team 2 played at home")]
    team2_wins_at_home: Annotated[int, Field(..., description="Number of wins for team 2 at home")]
    team2_losses_at_home: Annotated[int, Field(..., description="Number of losses for team 2 at home")]
    team2_draws_at_home: Annotated[int, Field(..., description="Number of draws for team 2 at home")]
    team2_scored_at_home: Annotated[int, Field(..., description="Goals scored by team 2 at home")]
    team2_conceded_at_home: Annotated[int, Field(..., description="Goals conceded by team 2 at home")]


class HeadToHeadStats(FrozenBaseModel):
    """Represents all head-to-head statistics."""
    
    overall: Annotated[OverallStats, Field(..., description="Overall statistics")]
    team1_at_home: Annotated[Team1AtHomeStats, Field(..., description="Team 1 at home statistics")]
    team2_at_home: Annotated[Team2AtHomeStats, Field(..., description="Team 2 at home statistics")]


class HeadToHead(FrozenBaseModel):
    """Represents head-to-head data between two teams from SoccerData API."""
    
    team1: Annotated[TeamInfo, Field(..., description="First team information")]
    team2: Annotated[TeamInfo, Field(..., description="Second team information")]
    stats: Annotated[HeadToHeadStats, Field(..., description="Head-to-head statistics")]
    
    @property
    def team_1(self) -> TeamInfo:
        """Alias for team1."""
        return self.team1
    
    @property
    def team_2(self) -> TeamInfo:
        """Alias for team2."""
        return self.team2