"""Betting Ticket Processor for the betting_ticket slice."""
import logging
import uuid
from datetime import datetime
from models.match_analysis import MatchAnalysis
from workflows.shared.serialization import serialize_match_analysis
from workflows.shared.llm_client import LLMClient
from workflows.match_insights.models import MatchAnalysisReport
from .agent import BettingTicketAgent
from .models import BettingTicket

logger = logging.getLogger(__name__)


class BettingTicketProcessor:
    """Processor for generating betting tickets from MatchAnalysis and MatchAnalysisReport."""
    
    def __init__(self, llm_client: LLMClient | None = None):
        """Initialize the Betting Ticket Processor.
        
        Parameters
        ----------
        llm_client : LLMClient | None
            LLM client instance. If None, creates a new one.
        """
        self.llm_client = llm_client or LLMClient()
        self.agent = BettingTicketAgent(llm_client=self.llm_client)
    
    async def process(
        self,
        match_analysis: MatchAnalysis,
        match_report: MatchAnalysisReport
    ) -> BettingTicket:
        """Process MatchAnalysis and MatchAnalysisReport to generate betting ticket.
        
        Parameters
        ----------
        match_analysis : MatchAnalysis
            The match analysis data with betting events.
        match_report : MatchAnalysisReport
            The match analysis report with probabilities and insights.
            
        Returns
        -------
        BettingTicket
            Generated betting ticket.
            
        Raises
        ------
        ValueError
            If betting events are not available.
        """
        # Check if betting events are available
        if not match_analysis.betting_events:
            logger.warning(f"No betting events available for {match_analysis.match_info.home} vs {match_analysis.match_info.away}")
        
        # Serialize for agent
        match_json = serialize_match_analysis(match_analysis)
        report_json = match_report.model_dump_json()
        
        # Generate ticket
        try:
            ticket = await self.agent.generate_ticket(match_json, report_json)
            
            # Ensure ticket has valid ID and timestamp if not set by agent
            if not ticket.ticket_id or ticket.ticket_id == "":
                ticket = ticket.model_copy(update={
                    "ticket_id": str(uuid.uuid4()),
                    "created_at": datetime.now().isoformat()
                })
            
            # Validate ticket
            self._validate_ticket(ticket)
            
            logger.info(f"Successfully generated betting ticket: {ticket.ticket_id}")
            return ticket
        except Exception as e:
            logger.error(f"Failed to generate betting ticket: {e}")
            raise
    
    def _validate_ticket(self, ticket: BettingTicket) -> None:
        """Validate betting ticket structure and values.
        
        Parameters
        ----------
        ticket : BettingTicket
            The ticket to validate.
            
        Raises
        ------
        ValueError
            If ticket validation fails.
        """
        # Check total stake matches sum of selections
        calculated_stake = sum(sel.stake_units for sel in ticket.selections)
        if ticket.total_stake != calculated_stake:
            logger.warning(f"Ticket total_stake ({ticket.total_stake}) doesn't match sum of selections ({calculated_stake})")
        
        # Check expected return calculation
        if ticket.selections:
            calculated_return = sum(
                sel.odds * sel.stake_units for sel in ticket.selections
            )
            if abs(ticket.expected_return - calculated_return) > 0.01:
                logger.warning(f"Ticket expected_return ({ticket.expected_return}) doesn't match calculated ({calculated_return})")
        
        # Validate confidence and risk values
        valid_confidence = {"High", "Medium", "Low"}
        if ticket.overall_confidence not in valid_confidence:
            raise ValueError(f"Invalid overall_confidence: {ticket.overall_confidence}")
        
        if ticket.risk_assessment not in valid_confidence:
            raise ValueError(f"Invalid risk_assessment: {ticket.risk_assessment}")
