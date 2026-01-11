from typing import Annotated, List
from pydantic import Field
from ..base_model import FrozenBaseModel
from .soccerdata_common import CountryInfo, Teams


class UpcomingMatchPreview(FrozenBaseModel):
    """Represents a single upcoming match preview summary."""
    
    id: Annotated[int, Field(..., description="Match ID")]
    date: Annotated[str, Field(..., description="Match date (e.g., '11/01/2026')")]
    time: Annotated[str, Field(..., description="Match time (e.g., '15:00')")]
    word_count: Annotated[int, Field(..., description="Word count of the preview")]
    excitement_rating: Annotated[float, Field(..., description="Excitement rating for the match")]
    teams: Annotated[Teams, Field(..., description="Home and away teams")]


class LeagueMatchPreviews(FrozenBaseModel):
    """Represents match previews grouped by league."""
    
    league_id: Annotated[int, Field(..., description="League ID")]
    league_name: Annotated[str, Field(..., description="League name")]
    is_cup: Annotated[bool, Field(..., description="Whether this is a cup competition")]
    country: Annotated[CountryInfo, Field(..., description="Country information")]
    match_previews: Annotated[List[UpcomingMatchPreview], Field(..., description="List of upcoming match previews")]


class MatchPreviewsUpcoming(FrozenBaseModel):
    """Represents upcoming match previews response from SoccerData API."""
    
    count: Annotated[int, Field(..., description="Total count of match previews")]
    updated_at: Annotated[str, Field(..., description="Timestamp when the data was last updated (ISO format)")]
    results: Annotated[List[LeagueMatchPreviews], Field(..., description="List of league match previews")]
