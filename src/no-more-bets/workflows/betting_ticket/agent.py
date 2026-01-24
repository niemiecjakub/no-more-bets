"""Betting Ticket Agent for generating structured betting recommendations."""
import logging
from typing import Optional
from semantic_kernel.contents import ChatHistory
from workflows.shared.base_agent import BaseAgent
from workflows.shared.serialization import serialize_match_analysis
from workflows.shared.llm_client import LLMClient
from models.match_analysis import MatchAnalysis
from workflows.match_insights.models import MatchAnalysisReport
from .models import BettingTicket

logger = logging.getLogger(__name__)


BETTING_TICKET_AGENT_INSTRUCTIONS = """You are a Professional Betting Analyst focused on identifying value bets and creating structured betting tickets.

YOUR PRIMARY RESPONSIBILITIES:
1. Compare calculated probabilities vs bookmaker odds to find value
2. Identify mispriced markets and value opportunities
3. Assess risk/reward ratios
4. Rank betting opportunities by value
5. Select optimal bet combinations
6. Balance risk across bet types
7. Assign confidence levels and stake recommendations
8. Generate structured ticket with detailed reasoning

VALUE BETTING PRINCIPLES:
1. VALUE = Calculated Probability > Implied Probability from Odds
   - If you calculate Team A has 60% chance to win, but odds imply only 50%, that's value
   
2. LOOK FOR:
   - Teams with strong xG but poor recent results (regression candidates)
   - Motivated teams (fighting relegation, chasing title)
   - Key player returns from injury
   - Underrated away teams
   - Over/under goals based on xG profiles
   - Mispriced markets where bookmaker odds don't reflect true probability

3. AVOID:
   - Heavy favorites with very low odds (limited value)
   - Matches with too much uncertainty
   - Bets based solely on narrative without data support
   - Bets where calculated probability is lower than implied probability

STAKE SIZING:
- High confidence + High value = 4-5 units
- Medium confidence + High value = 3 units
- High confidence + Medium value = 2-3 units
- Medium confidence + Medium value = 2 units
- Low confidence or Low value = 1 unit (or skip)

BET SELECTION STRATEGY:
- Include 2-4 selections per ticket
- Mix bet types (1X2, Over/Under, BTTS) for diversification
- Balance safer bets with value bets
- Ensure total stake is reasonable (typically 5-10 units total)

OUTPUT REQUIREMENTS:
You must output a valid JSON object matching this exact structure.
IMPORTANT: Always include ticket_id (a unique identifier like "ticket-2024-01-22-001") and created_at (ISO format timestamp like "2024-01-22T12:00:00").

{
  "ticket_id": "ticket-2024-01-22-001",
  "created_at": "2024-01-22T12:00:00",
  "selections": [
    {
      "match": {
        "home": "Team A",
        "away": "Team B",
        "date": "Date",
        "time": "Time"
      },
      "bet_type": "1X2",
      "selection": "Home Win",
      "odds": 2.10,
      "confidence": "High",
      "stake_units": 3,
      "reasoning": "Detailed explanation...",
      "value_score": 1.25,
      "implied_probability": 0.476,
      "calculated_probability": 0.60
    }
  ],
  "total_stake": 5,
  "expected_return": 10.50,
  "risk_assessment": "Medium",
  "overall_confidence": "High"
}

RULES:
- Always justify bets with specific data from the match analysis report
- Calculate value_score as: calculated_probability / implied_probability
- Include at least one "safer" bet and one "value" bet
- Never recommend bets you cannot justify with data
- Be honest about uncertainty - some matches are not bettable
- Acknowledge potential risks and what could go wrong
- If no value bets are found, create a ticket with 0 selections but explain why
"""


class BettingTicketAgent(BaseAgent[BettingTicket]):
    """Agent for generating betting tickets."""
    
    def __init__(self, llm_client: LLMClient | None = None):
        """Initialize the Betting Ticket Agent.
        
        Parameters
        ----------
        llm_client : LLMClient | None
            LLM client instance. If None, creates a new one.
        """
        super().__init__(
            name="BettingTicketAgent",
            instructions=BETTING_TICKET_AGENT_INSTRUCTIONS,
            llm_client=llm_client,
            output_model=BettingTicket
        )
    
    async def generate_ticket(
        self,
        match_analysis_json: str,
        match_report_json: str
    ) -> BettingTicket:
        """Generate betting ticket from match data and analysis report.
        
        Parameters
        ----------
        match_analysis_json : str
            Serialized MatchAnalysis JSON string.
        match_report_json : str
            Serialized MatchAnalysisReport JSON string.
            
        Returns
        -------
        BettingTicket
            Generated betting ticket.
        """
        prompt = self.create_prompt(match_analysis_json, match_report_json)
        
        # Create chat history with the prompt
        chat_history = ChatHistory()
        chat_history.add_user_message(prompt)
        
        # Invoke the agent (returns async generator)
        response_content = ""
        async for response in self.agent.invoke(chat_history):
            if response and response.content:
                # Extract text from ChatMessageContent object
                # ChatMessageContent can be converted to string or may have .content attribute
                if isinstance(response.content, str):
                    response_content = response.content
                elif hasattr(response.content, 'content'):
                    response_content = str(response.content.content) if response.content.content else str(response.content)
                else:
                    response_content = str(response.content)
        
        if not response_content:
            raise ValueError("Agent returned empty response")
        
        # Parse structured output
        ticket = self.parse_structured_output(response_content)
        logger.info(f"Generated betting ticket with {len(ticket.selections)} selections")
        
        return ticket
    
    def create_prompt(self, match_context: str, report_context: str) -> str:
        """Create the prompt for betting ticket generation.
        
        Parameters
        ----------
        match_context : str
            Serialized MatchAnalysis JSON string.
        report_context : str
            Serialized MatchAnalysisReport JSON string.
            
        Returns
        -------
        str
            Complete prompt string.
        """
        return f"""Generate a betting ticket based on the following match data and analysis report.

MATCH DATA (JSON):
{match_context}

MATCH ANALYSIS REPORT (JSON):
{report_context}

Analyze the available betting events from the match data and compare them with the probabilities and insights from the analysis report. Identify value bets where the calculated probability exceeds the implied probability from the odds.

Create a structured betting ticket with 2-4 selections that:
1. Show clear value (calculated probability > implied probability)
2. Are well-justified by the analysis
3. Balance risk across different bet types
4. Have appropriate stake sizing based on confidence and value

Output only the JSON object matching the required structure, no additional text."""
