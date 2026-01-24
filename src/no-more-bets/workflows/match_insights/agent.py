"""Match Insights Agent for analyzing MatchAnalysis data."""
import logging
from typing import Optional
from semantic_kernel.contents import ChatHistory
from workflows.shared.base_agent import BaseAgent
from workflows.shared.serialization import serialize_match_analysis
from workflows.shared.llm_client import LLMClient
from .models import MatchAnalysisReport

logger = logging.getLogger(__name__)


MATCH_INSIGHTS_AGENT_INSTRUCTIONS = """You are a Football Match Analysis Specialist focused on extracting comprehensive insights from match data.

YOUR PRIMARY RESPONSIBILITIES:
1. Extract and synthesize key insights from comprehensive match data
2. Parse lineup data to identify key players and assess injury impact
3. Analyze head-to-head patterns and historical statistics
4. Calculate win probabilities from statistical data
5. Assess team form and strength adjustments
6. Generate comprehensive team analysis for both sides

ANALYSIS METHODOLOGY:

1. LINEUP ANALYSIS:
   - Identify key players in each team's lineup
   - Assess impact of injuries on team strength
   - Note missing key players and their importance
   - Consider lineup type (Predicted vs Confirmed)

2. HEAD-TO-HEAD ANALYSIS:
   - Extract patterns from historical matchups
   - Note home/away performance differences
   - Identify trends (e.g., "Team A has won last 3 meetings")

3. STATISTICAL ANALYSIS:
   - Calculate win probabilities based on:
     * Head-to-head record
     * Team form (from FBref data)
     * League position
     * Goals scored/conceded
   - Adjust probabilities for injuries
   - Calculate expected goals based on team statistics

4. TEAM FORM ANALYSIS:
   - Analyze recent form (last 5 games)
   - Compare home vs away performance
   - Identify strengths and weaknesses from statistics

5. MATCH CONTEXT:
   - Consider weather conditions
   - Note excitement rating
   - Include match preview predictions

OUTPUT REQUIREMENTS:
You must output a valid JSON object matching this exact structure:
{
  "match_info": {
    "home": "Team Name",
    "away": "Team Name",
    "date": "Date",
    "time": "Time"
  },
  "key_insights": ["Insight 1", "Insight 2", ...],
  "home_team_analysis": {
    "team_name": "Team Name",
    "form_summary": "Summary of form",
    "key_players": ["Player 1", "Player 2"],
    "injury_impact": "Assessment of injury impact",
    "statistical_strengths": ["Strength 1"],
    "statistical_weaknesses": ["Weakness 1"]
  },
  "away_team_analysis": {
    "team_name": "Team Name",
    "form_summary": "Summary of form",
    "key_players": ["Player 1", "Player 2"],
    "injury_impact": "Assessment of injury impact",
    "statistical_strengths": ["Strength 1"],
    "statistical_weaknesses": ["Weakness 1"]
  },
  "statistical_summary": {
    "win_probability_home": 0.45,
    "win_probability_away": 0.35,
    "draw_probability": 0.20,
    "expected_goals": 2.5,
    "injury_adjusted_strength": {
      "home": 0.95,
      "away": 1.0
    }
  },
  "head_to_head_insights": ["Insight 1", "Insight 2"],
  "match_context": "Context including weather, excitement, predictions"
}

RULES:
- Probabilities must sum to approximately 1.0
- Be specific and data-driven in your insights
- Reference actual statistics from the provided data
- If data is missing, note it but still provide analysis based on available data
- Injury impact should be quantitative where possible (e.g., "Reduces team strength by 10%")
"""


class MatchInsightsAgent(BaseAgent[MatchAnalysisReport]):
    """Agent for generating match analysis insights."""
    
    def __init__(self, llm_client: LLMClient | None = None):
        """Initialize the Match Insights Agent.
        
        Parameters
        ----------
        llm_client : LLMClient | None
            LLM client instance. If None, creates a new one.
        """
        super().__init__(
            name="MatchInsightsAgent",
            instructions=MATCH_INSIGHTS_AGENT_INSTRUCTIONS,
            llm_client=llm_client,
            output_model=MatchAnalysisReport
        )
    
    async def analyze(self, match_analysis_json: str) -> MatchAnalysisReport:
        """Analyze match data and generate insights report.
        
        Parameters
        ----------
        match_analysis_json : str
            Serialized MatchAnalysis JSON string.
            
        Returns
        -------
        MatchAnalysisReport
            Comprehensive match analysis report.
        """
        prompt = self.create_prompt(match_analysis_json)
        
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
        report = self.parse_structured_output(response_content)
        logger.info(f"Generated match insights report for {report.match_info.home} vs {report.match_info.away}")
        
        return report
    
    def create_prompt(self, context: str) -> str:
        """Create the prompt for match analysis.
        
        Parameters
        ----------
        context : str
            Serialized MatchAnalysis JSON string.
            
        Returns
        -------
        str
            Complete prompt string.
        """
        return f"""Analyze the following match data and generate a comprehensive match analysis report.

MATCH DATA (JSON):
{context}

Provide your analysis as a JSON object matching the required structure. Focus on:
1. Key insights that stand out from the data
2. Detailed team analysis for both home and away teams
3. Statistical probabilities based on the data
4. Head-to-head insights
5. Match context summary

Output only the JSON object, no additional text."""
