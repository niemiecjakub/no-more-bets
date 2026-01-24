"""Models for match insights slice."""
from typing import Annotated, List, Dict
from pydantic import Field
from models.base_model import FrozenBaseModel
from models.match_analysis import MatchInfo


class TeamAnalysis(FrozenBaseModel):
    """Analysis of a single team."""
    
    team_name: Annotated[str, Field(..., description="Team name")]
    form_summary: Annotated[str, Field(..., description="Summary of team's recent form")]
    key_players: Annotated[List[str], Field(default_factory=list, description="List of key players")]
    injury_impact: Annotated[str, Field(..., description="Assessment of injury impact on team strength")]
    statistical_strengths: Annotated[List[str], Field(default_factory=list, description="Team's statistical strengths")]
    statistical_weaknesses: Annotated[List[str], Field(default_factory=list, description="Team's statistical weaknesses")]


class StatisticalSummary(FrozenBaseModel):
    """Statistical summary of match probabilities and metrics."""
    
    win_probability_home: Annotated[float, Field(..., description="Probability of home team win (0-1)")]
    win_probability_away: Annotated[float, Field(..., description="Probability of away team win (0-1)")]
    draw_probability: Annotated[float, Field(..., description="Probability of draw (0-1)")]
    expected_goals: Annotated[float, Field(..., description="Expected total goals in the match")]
    injury_adjusted_strength: Annotated[Dict[str, float], Field(default_factory=dict, description="Team strength adjustments due to injuries")]


class MatchAnalysisReport(FrozenBaseModel):
    """Comprehensive match analysis report."""
    
    match_info: Annotated[MatchInfo, Field(..., description="Basic match information")]
    key_insights: Annotated[List[str], Field(default_factory=list, description="Top insights from the analysis")]
    home_team_analysis: Annotated[TeamAnalysis, Field(..., description="Analysis of home team")]
    away_team_analysis: Annotated[TeamAnalysis, Field(..., description="Analysis of away team")]
    statistical_summary: Annotated[StatisticalSummary, Field(..., description="Statistical summary and probabilities")]
    head_to_head_insights: Annotated[List[str], Field(default_factory=list, description="Insights from head-to-head statistics")]
    match_context: Annotated[str, Field(..., description="Match context including weather, excitement rating, and predictions")]
