"""Match Insights vertical slice for generating comprehensive match analysis reports."""

from .models import MatchAnalysisReport, TeamAnalysis, StatisticalSummary
from .processor import MatchInsightsProcessor

__all__ = [
    "MatchAnalysisReport",
    "TeamAnalysis",
    "StatisticalSummary",
    "MatchInsightsProcessor",
]
