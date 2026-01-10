"""Model for approved betting coupon with closing thoughts."""

from typing import Annotated, List, Optional
from datetime import datetime
from pydantic import Field
from .base_model import FrozenBaseModel


class BettingSelection(FrozenBaseModel):
    """Represents a single bet selection in the approved coupon."""
    
    match: Annotated[str, Field(..., description="Match identifier (e.g., 'Arsenal vs Liverpool')")]
    bet_type: Annotated[str, Field(..., description="Type of bet (e.g., '1X2', 'Over/Under 2.5', 'Both Teams to Score')")]
    selection: Annotated[str, Field(..., description="The betting selection (e.g., 'Arsenal Win', 'Over 2.5', 'Yes')")]
    odds: Annotated[float, Field(..., description="Current odds for this selection")]
    confidence: Annotated[str, Field(..., description="Confidence level: High, Medium, or Low")]
    stake: Annotated[int, Field(..., description="Recommended stake in units (1-5 scale)")]
    reasoning: Annotated[str, Field(..., description="Detailed justification for this bet based on research and analytics")]
    implied_probability: Annotated[Optional[float], Field(None, description="Implied probability from odds (as percentage)")]


class ApprovedCoupon(FrozenBaseModel):
    """Approved betting coupon with all selections and closing thoughts.
    
    This model represents the final, validated betting coupon after
    thorough analysis by all agents (Research, Analytics, Betting, Critic).
    """
    
    # Metadata
    query: Annotated[str, Field(..., description="Original user query that initiated the analysis")]
    approved_at: Annotated[datetime, Field(default_factory=datetime.now, description="Timestamp when coupon was approved")]
    analysis_date: Annotated[str, Field(..., description="Date of the analysis (formatted string)")]
    
    # Betting selections
    bets: Annotated[List[BettingSelection], Field(..., description="List of approved betting selections")]
    total_bets: Annotated[int, Field(..., description="Total number of bets in the coupon")]
    
    # Financial summary
    total_stake: Annotated[int, Field(..., description="Total stake across all bets (sum of individual stakes)")]
    expected_return: Annotated[Optional[float], Field(None, description="Expected return if all bets win (calculated from odds)")]
    potential_profit: Annotated[Optional[float], Field(None, description="Potential profit if all bets win (expected return - total stake)")]
    
    # Risk assessment
    overall_risk: Annotated[str, Field(..., description="Overall risk assessment: Low, Medium, or High")]
    risk_notes: Annotated[Optional[str], Field(None, description="Additional notes about risks and considerations")]
    
    # Closing thoughts
    closing_thoughts: Annotated[str, Field(..., description="Final summary and closing thoughts from the Critic Agent")]
    key_insights: Annotated[Optional[str], Field(None, description="Key insights from the analysis process")]
    warnings: Annotated[Optional[str], Field(None, description="Important warnings or caveats about the coupon")]
    
    # Agent summary
    research_summary: Annotated[Optional[str], Field(None, description="Summary of key research findings")]
    analytics_summary: Annotated[Optional[str], Field(None, description="Summary of statistical analysis")]
