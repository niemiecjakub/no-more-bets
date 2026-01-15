from typing import Annotated, List, Optional
from pydantic import Field
from .base_model import FrozenBaseModel
from .rotowire import PlayerInLineup, InjuryEntry
from .soccerdata import TeamInfo, OverallStats, Team1AtHomeStats, Team2AtHomeStats, PreviewContentItem
from .betclic import BookmakerEvent


class MatchInfo(FrozenBaseModel):
    """Represents basic match information."""
    
    home_team: Annotated[str, Field(..., description="Home team name")]
    away_team: Annotated[str, Field(..., description="Away team name")]
    date: Annotated[str, Field(..., description="Match date")]
    time: Annotated[str, Field(..., description="Match time")]


class TeamLineupData(FrozenBaseModel):
    """Represents a team's lineup data including players and injuries."""
    
    team_name: Annotated[str, Field(..., description="Team name")]
    lineup_type: Annotated[str, Field(..., description="Type of lineup (e.g., 'Predicted Lineup', 'Confirmed Lineup')")]
    players: Annotated[List[PlayerInLineup], Field(default_factory=list, description="List of players in the lineup")]
    injuries: Annotated[List[InjuryEntry], Field(default_factory=list, description="List of injuries for the team")]


class LineupData(FrozenBaseModel):
    """Represents lineup data for both teams."""
    
    home_team: Annotated[TeamLineupData, Field(..., description="Home team lineup data")]
    away_team: Annotated[TeamLineupData, Field(..., description="Away team lineup data")]


class HeadToHeadData(FrozenBaseModel):
    """Represents head-to-head statistics between two teams."""
    
    team1: Annotated[TeamInfo, Field(..., description="First team information")]
    team2: Annotated[TeamInfo, Field(..., description="Second team information")]
    overall: Annotated[OverallStats, Field(..., description="Overall statistics")]
    team1_at_home: Annotated[Team1AtHomeStats, Field(..., description="Team 1 at home statistics")]
    team2_at_home: Annotated[Team2AtHomeStats, Field(..., description="Team 2 at home statistics")]


class PredictionData(FrozenBaseModel):
    """Represents match prediction with derived team name."""
    
    type: Annotated[str, Field(..., description="Prediction type (e.g., 'match_winner')")]
    choice: Annotated[str, Field(..., description="Prediction choice (e.g., 'home', 'away', 'draw')")]
    team_name: Annotated[str, Field(..., description="Team name for the prediction (or 'draw')")]


class WeatherData(FrozenBaseModel):
    """Represents weather information for the match."""
    
    description: Annotated[str, Field(..., description="Weather description")]
    temp_c: Annotated[float, Field(..., description="Temperature in Celsius")]
    temp_f: Annotated[float, Field(..., description="Temperature in Fahrenheit")]


class MatchPreviewData(FrozenBaseModel):
    """Represents match preview data."""
    
    excitement_rating: Annotated[float, Field(..., description="Excitement rating for the match")]
    prediction: Annotated[PredictionData, Field(..., description="Match prediction")]
    weather: Annotated[WeatherData, Field(..., description="Weather information")]
    preview_content: Annotated[List[PreviewContentItem], Field(default_factory=list, description="Preview content items")]


class MatchAnalysis(FrozenBaseModel):
    """Comprehensive model representing all data for a match analysis.
    
    This model consolidates data from multiple sources:
    - Match basic info from Betclic
    - Lineup data from Rotowire
    - Head-to-head statistics from SoccerData
    - Match preview from SoccerData
    - Betting events from Betclic
    """
    
    match_info: Annotated[MatchInfo, Field(..., description="Basic match information")]
    lineup: Annotated[Optional[LineupData], Field(None, description="Lineup data for both teams")]
    head_to_head: Annotated[Optional[HeadToHeadData], Field(None, description="Head-to-head statistics")]
    match_preview: Annotated[Optional[MatchPreviewData], Field(None, description="Match preview data")]
    betting_events: Annotated[Optional[List[BookmakerEvent]], Field(None, description="Betting events data")]


__all__ = [
    "MatchInfo",
    "TeamLineupData",
    "LineupData",
    "HeadToHeadData",
    "PredictionData",
    "WeatherData",
    "MatchPreviewData",
    "MatchAnalysis",
]
