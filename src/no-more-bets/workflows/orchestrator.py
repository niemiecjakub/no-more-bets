"""Workflow orchestrator for coordinating match analysis and betting ticket generation."""
import logging
from typing import List, Tuple
from models.match_analysis import MatchAnalysis
from workflows.shared.llm_client import LLMClient
from workflows.match_insights.processor import MatchInsightsProcessor
from workflows.match_insights.models import MatchAnalysisReport
from workflows.betting_ticket.processor import BettingTicketProcessor
from workflows.betting_ticket.models import BettingTicket

logger = logging.getLogger(__name__)


class MatchAnalysisWorkflow:
    """Orchestrator for the match analysis workflow.
    
    Coordinates between vertical slices to process MatchAnalysis data
    and generate both analysis reports and betting tickets.
    """
    
    def __init__(self, llm_client: LLMClient | None = None):
        """Initialize the workflow orchestrator.
        
        Parameters
        ----------
        llm_client : LLMClient | None
            LLM client instance. If None, creates a new one.
            A single client is shared across slices for efficiency.
        """
        self.llm_client = llm_client or LLMClient()
        self.insights_processor = MatchInsightsProcessor(llm_client=self.llm_client)
        self.ticket_processor = BettingTicketProcessor(llm_client=self.llm_client)
    
    async def process_match(
        self,
        match_analysis: MatchAnalysis
    ) -> Tuple[MatchAnalysisReport, BettingTicket]:
        """Process a single match and generate analysis report and betting ticket.
        
        Parameters
        ----------
        match_analysis : MatchAnalysis
            The match analysis data to process.
            
        Returns
        -------
        Tuple[MatchAnalysisReport, BettingTicket]
            Generated match analysis report and betting ticket.
            
        Raises
        ------
        ValueError
            If processing fails at any stage.
        """
        logger.info(
            f"Processing match: {match_analysis.match_info.home} vs "
            f"{match_analysis.match_info.away}"
        )
        
        try:
            # Slice 1: Generate match insights
            logger.debug("Generating match insights...")
            report = await self.insights_processor.process(match_analysis)
            logger.info("Match insights generated successfully")
            
            # Slice 2: Generate betting ticket (uses insights)
            logger.debug("Generating betting ticket...")
            ticket = await self.ticket_processor.process(match_analysis, report)
            logger.info("Betting ticket generated successfully")
            
            return report, ticket
            
        except Exception as e:
            logger.error(
                f"Failed to process match {match_analysis.match_info.home} vs "
                f"{match_analysis.match_info.away}: {e}"
            )
            raise
    
    async def process_matches(
        self,
        matches: List[MatchAnalysis]
    ) -> List[Tuple[MatchAnalysisReport, BettingTicket]]:
        """Process multiple matches and generate reports and tickets.
        
        Parameters
        ----------
        matches : List[MatchAnalysis]
            List of match analysis data to process.
            
        Returns
        -------
        List[Tuple[MatchAnalysisReport, BettingTicket]]
            List of tuples containing generated reports and tickets for each match.
        """
        results = []
        
        logger.info(f"Processing {len(matches)} matches...")
        
        for i, match in enumerate(matches, 1):
            logger.info(f"Processing match {i}/{len(matches)}")
            try:
                report, ticket = await self.process_match(match)
                results.append((report, ticket))
            except Exception as e:
                logger.error(f"Skipping match due to error: {e}")
                # Continue processing other matches even if one fails
                continue
        
        logger.info(f"Successfully processed {len(results)}/{len(matches)} matches")
        return results
