from typing import Annotated, Optional
from pydantic import Field
from ..base_model import FrozenBaseModel


class XgStats(FrozenBaseModel):
    """Represents xG statistics for a team in the FotMob Premier League xG table."""
    
    position: Annotated[int, Field(..., description="League position/rank")]
    position_change: Annotated[Optional[int], Field(None, description="Change in position (positive for up, negative for down, None if no change)")]
    team_id: Annotated[int, Field(..., description="Team ID extracted from URL")]
    team_name: Annotated[str, Field(..., description="Full team name")]
    team_shortname: Annotated[str, Field(..., description="Short team name")]
    team_logo_url: Annotated[str, Field(..., description="Team logo image URL")]
    xg: Annotated[float, Field(..., description="Expected goals (main value)")]
    xg_diff: Annotated[Optional[str], Field(None, description="Difference between expected and actual goals. Positive value (e.g., '+0.7') means team is performing better than expected. Negative value (e.g., '-2.5') means team is performing worse than expected.")]
    xga: Annotated[float, Field(..., description="Expected goals against (main value)")]
    xga_diff: Annotated[Optional[str], Field(None, description="Difference between expected and actual goals against. Positive value (e.g., '+1.6') means team is conceding fewer goals than expected. Negative value (e.g., '-4.4') means team is conceding more goals than expected.")]
    xpts: Annotated[float, Field(..., description="Expected points (main value)")]
    xpts_diff: Annotated[Optional[str], Field(None, description="Difference between expected and actual points. Positive value (e.g., '+3') means team has more points than expected. Negative value (e.g., '-2') means team has fewer points than expected.")]
