"""Models for betting ticket slice."""
from typing import Annotated, List
from pydantic import Field
from models.base_model import FrozenBaseModel
from models.match_analysis import MatchInfo


class BetSelection(FrozenBaseModel):
    """A single bet selection in a betting ticket."""
    
    match: Annotated[MatchInfo, Field(..., description="Match information")]
    bet_type: Annotated[str, Field(..., description="Bet type (e.g., '1X2', 'Over/Under', 'BTTS')")]
    selection: Annotated[str, Field(..., description="Bet selection (e.g., 'Home Win', 'Over 2.5')")]
    odds: Annotated[float, Field(..., description="Betting odds")]
    confidence: Annotated[str, Field(..., description="Confidence level: 'High', 'Medium', or 'Low'")]
    stake_units: Annotated[int, Field(..., ge=1, le=5, description="Recommended stake in units (1-5 scale)")]
    reasoning: Annotated[str, Field(..., description="Detailed reasoning for this bet")]
    value_score: Annotated[float, Field(..., description="Calculated value metric (higher is better)")]
    implied_probability: Annotated[float, Field(..., description="Implied probability from odds (0-1)")]
    calculated_probability: Annotated[float, Field(..., description="Calculated probability from analysis (0-1)")]


class BettingTicket(FrozenBaseModel):
    """Structured betting ticket with multiple selections."""
    
    ticket_id: Annotated[str, Field(..., description="Unique ticket identifier")]
    created_at: Annotated[str, Field(..., description="Ticket creation timestamp")]
    selections: Annotated[List[BetSelection], Field(default_factory=list, description="List of bet selections")]
    total_stake: Annotated[int, Field(..., description="Total stake units across all selections")]
    expected_return: Annotated[float, Field(..., description="Expected return if all bets win")]
    risk_assessment: Annotated[str, Field(..., description="Overall risk assessment: 'Low', 'Medium', or 'High'")]
    overall_confidence: Annotated[str, Field(..., description="Overall confidence: 'High', 'Medium', or 'Low'")]
