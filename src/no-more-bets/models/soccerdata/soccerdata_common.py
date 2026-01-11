from typing import Annotated
from pydantic import Field
from ..base_model import FrozenBaseModel


class CountryInfo(FrozenBaseModel):
    """Represents country information."""
    
    id: Annotated[int, Field(..., description="Country ID")]
    name: Annotated[str, Field(..., description="Country name")]


class TeamInfo(FrozenBaseModel):
    """Represents basic team information."""
    
    id: Annotated[int, Field(..., description="Team ID")]
    name: Annotated[str, Field(..., description="Team name")]


class Teams(FrozenBaseModel):
    """Represents home and away teams."""
    
    home: Annotated[TeamInfo, Field(..., description="Home team information")]
    away: Annotated[TeamInfo, Field(..., description="Away team information")]
