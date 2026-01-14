from typing import Annotated, List, Optional
from pydantic import Field
from ..base_model import FrozenBaseModel


class PlayerInjury(FrozenBaseModel):
    """Represents an individual player injury entry."""
    
    player: Annotated[str, Field(..., description="Player name")]
    reason: Annotated[str, Field(..., description="Injury reason (e.g., 'Knee Injury', 'Thigh Injury')")]
    further_detail: Annotated[Optional[str], Field(None, description="Additional details about the injury")]
    potential_return: Annotated[Optional[str], Field(None, description="Potential return date or 'No Return Date'")]
    condition: Annotated[Optional[str], Field(None, description="Current condition (e.g., 'Not Available', 'Currently Being Assessed')")]
    status: Annotated[Optional[str], Field(None, description="Status (e.g., 'Ruled Out', '25%', '50%')")]
    team_id: Annotated[int, Field(..., description="Team ID from data-team-id attribute")]


class TeamInjury(FrozenBaseModel):
    """Represents injury data for a team."""
    
    team_name: Annotated[str, Field(..., description="Team name")]
    team_id: Annotated[int, Field(..., description="Team ID from data-team-id attribute")]
    injury_count: Annotated[Optional[int], Field(None, description="Number of injuries for this team")]
    players: Annotated[List[PlayerInjury], Field(default_factory=list, description="List of injured players")]


class InjuryData(FrozenBaseModel):
    """Container for all Premier League injury data."""
    
    teams: Annotated[List[TeamInjury], Field(default_factory=list, description="List of teams with injury data")]


__all__ = [
    "PlayerInjury",
    "TeamInjury",
    "InjuryData",
]
