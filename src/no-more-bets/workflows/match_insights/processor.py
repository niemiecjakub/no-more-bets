"""Match Insights Processor for the match_insights slice."""
import logging
from models.match_analysis import MatchAnalysis
from workflows.shared.serialization import serialize_match_analysis, validate_match_analysis
from workflows.shared.llm_client import LLMClient
from .agent import MatchInsightsAgent
from .models import MatchAnalysisReport

logger = logging.getLogger(__name__)


class MatchInsightsProcessor:
    """Processor for generating match insights from MatchAnalysis data."""
    
    def __init__(self, llm_client: LLMClient | None = None):
        """Initialize the Match Insights Processor.
        
        Parameters
        ----------
        llm_client : LLMClient | None
            LLM client instance. If None, creates a new one.
        """
        self.llm_client = llm_client or LLMClient()
        self.agent = MatchInsightsAgent(llm_client=self.llm_client)
    
    async def process(self, match_analysis: MatchAnalysis) -> MatchAnalysisReport:
        """Process MatchAnalysis and generate insights report.
        
        Parameters
        ----------
        match_analysis : MatchAnalysis
            The match analysis data to process.
            
        Returns
        -------
        MatchAnalysisReport
            Generated match analysis report.
            
        Raises
        ------
        ValueError
            If match_analysis validation fails.
        """
        # Validate input
        if not validate_match_analysis(match_analysis):
            logger.warning(f"MatchAnalysis validation failed for {match_analysis.match_info.home} vs {match_analysis.match_info.away}")
        
        # Serialize for agent
        match_json = serialize_match_analysis(match_analysis)
        
        # Generate insights
        try:
            report = await self.agent.analyze(match_json)
            logger.info(f"Successfully generated insights report")
            return report
        except Exception as e:
            logger.error(f"Failed to generate insights report: {e}")
            raise
