from typing import Annotated, List
from pydantic import Field
from ..base_model import FrozenBaseModel
from .soccerdata_common import CountryInfo, TeamInfo, Teams


class LeagueInfo(FrozenBaseModel):
    """Represents league information."""
    
    id: Annotated[int, Field(..., description="League ID")]
    name: Annotated[str, Field(..., description="League name")]


class StageInfo(FrozenBaseModel):
    """Represents stage information."""
    
    id: Annotated[int, Field(..., description="Stage ID")]
    name: Annotated[str, Field(..., description="Stage name")]
    is_active: Annotated[bool, Field(..., description="Whether the stage is active")]


class Weather(FrozenBaseModel):
    """Represents weather information for the match."""
    
    temp_f: Annotated[float, Field(..., description="Temperature in Fahrenheit")]
    temp_c: Annotated[float, Field(..., description="Temperature in Celsius")]
    description: Annotated[str, Field(..., description="Weather description")]


class Prediction(FrozenBaseModel):
    """Represents match prediction."""
    
    type: Annotated[str, Field(..., description="Prediction type (e.g., 'match_winner')")]
    choice: Annotated[str, Field(..., description="Prediction choice (e.g., 'home', 'away', 'draw')")]


class MatchData(FrozenBaseModel):
    """Represents match data including weather, excitement rating, and prediction."""
    
    weather: Annotated[Weather, Field(..., description="Weather information")]
    excitement_rating: Annotated[float, Field(..., description="Excitement rating for the match")]
    prediction: Annotated[Prediction, Field(..., description="Match prediction")]


class PreviewContentItem(FrozenBaseModel):
    """Represents a single item in the preview content."""
    
    name: Annotated[str, Field(..., description="Content item name (e.g., 'p1', 'h1')")]
    content: Annotated[str, Field(..., description="Content text")]


class MatchPreview(FrozenBaseModel):
    """Represents match preview data from SoccerData API."""
    
    id: Annotated[int, Field(..., description="Match ID")]
    date: Annotated[str, Field(..., description="Match date (e.g., '11-01-2026')")]
    time: Annotated[str, Field(..., description="Match time (e.g., '16:30')")]
    country: Annotated[CountryInfo, Field(..., description="Country information")]
    league: Annotated[LeagueInfo, Field(..., description="League information")]
    stage: Annotated[StageInfo, Field(..., description="Stage information")]
    teams: Annotated[Teams, Field(..., description="Home and away teams")]
    match_data: Annotated[MatchData, Field(..., description="Match data including weather and prediction")]
    preview_content: Annotated[List[PreviewContentItem], Field(..., description="Preview content items")]
