from typing import Annotated, List, Optional
from pydantic import Field
from ..base_model import FrozenBaseModel


class PlayerInLineup(FrozenBaseModel):
    """Represents a player in a lineup."""
    
    position: Annotated[str, Field(..., description="Player position (e.g., 'GK', 'DL', 'DC', 'DR', 'DMC', 'AML', 'AMC', 'AMR', 'FW')")]
    player_name: Annotated[str, Field(..., description="Player name")]


class InjuryEntry(FrozenBaseModel):
    """Represents an injury entry for a player."""
    
    player: Annotated[str, Field(..., description="Player name")]
    position: Annotated[str, Field(..., description="Player position")]
    status: Annotated[str, Field(..., description="Injury status (e.g., 'QUES', 'OUT', 'SUS')")]


class TeamLineup(FrozenBaseModel):
    """Represents a team's lineup and related information."""
    
    team_name: Annotated[str, Field(..., description="Team name")]
    team_code: Annotated[Optional[str], Field(None, description="Team abbreviation/code")]
    lineup_type: Annotated[str, Field(..., description="Type of lineup (e.g., 'Predicted Lineup', 'Confirmed Lineup')")]
    players: Annotated[List[PlayerInLineup], Field(default_factory=list, description="List of players in the lineup")]
    injuries: Annotated[List[InjuryEntry], Field(default_factory=list, description="List of injuries for the team")]


class GameOdds(FrozenBaseModel):
    """Represents betting odds for a game."""
    
    home_odds: Annotated[Optional[str], Field(None, description="Home team odds")]
    draw_odds: Annotated[Optional[str], Field(None, description="Draw odds")]
    away_odds: Annotated[Optional[str], Field(None, description="Away team odds")]


class WeatherInfo(FrozenBaseModel):
    """Represents weather information for a game."""
    
    condition: Annotated[Optional[str], Field(None, description="Weather condition (e.g., 'cloudy', 'partly-cloudy-day')")]
    precipitation: Annotated[Optional[str], Field(None, description="Precipitation percentage")]
    temperature: Annotated[Optional[str], Field(None, description="Temperature")]
    wind: Annotated[Optional[str], Field(None, description="Wind speed and direction")]


class GameLineup(FrozenBaseModel):
    """Represents a complete game with lineups."""
    
    date: Annotated[str, Field(..., description="Game date")]
    time: Annotated[Optional[str], Field(None, description="Game time (ET)")]
    home_team: Annotated[TeamLineup, Field(..., description="Home team lineup")]
    away_team: Annotated[TeamLineup, Field(..., description="Away team lineup")]
    odds: Annotated[Optional[GameOdds], Field(None, description="Betting odds")]
    weather: Annotated[Optional[WeatherInfo], Field(None, description="Weather information")]


__all__ = [
    "PlayerInLineup",
    "InjuryEntry",
    "TeamLineup",
    "GameOdds",
    "WeatherInfo",
    "GameLineup",
]
