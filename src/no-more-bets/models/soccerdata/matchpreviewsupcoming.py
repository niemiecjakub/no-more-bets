from typing import Annotated, List
from pydantic import Field
from ..base_model import FrozenBaseModel
from .soccerdata_common import Teams


class UpcomingMatchPreview(FrozenBaseModel):
    """Represents a single upcoming match preview summary."""
    
    id: Annotated[int, Field(..., description="Match ID")]
    date: Annotated[str, Field(..., description="Match date (e.g., '11/01/2026')")]
    time: Annotated[str, Field(..., description="Match time (e.g., '15:00')")]
    excitement_rating: Annotated[float, Field(..., description="Excitement rating for the match")]
    teams: Annotated[Teams, Field(..., description="Home and away teams")]


class LeagueMatchPreviews(FrozenBaseModel):
    """Represents match previews grouped by league."""
    
    league_id: Annotated[int, Field(..., description="League ID")]
    league_name: Annotated[str, Field(..., description="League name")]
    match_previews: Annotated[List[UpcomingMatchPreview], Field(..., description="List of upcoming match previews")]
