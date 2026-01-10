from semantic_kernel import Kernel
from semantic_kernel.agents import ChatCompletionAgent
from semantic_kernel.connectors.ai.open_ai import  OpenAIPromptExecutionSettings
from semantic_kernel.connectors.ai import FunctionChoiceBehavior
from semantic_kernel.functions import KernelArguments
from .plugins import WebSearchPlugin

# Agent name constant for use in group chat
RESEARCH_AGENT_NAME = "ResearchAgent"

# Precise instructions for the Research Agent
RESEARCH_AGENT_INSTRUCTIONS = """You are a Football Research Specialist focused on gathering pre-match intelligence.

YOUR PRIMARY RESPONSIBILITIES:
1. Search for the latest news about upcoming matches
2. Find injury reports and team news for both teams
3. Discover lineup predictions and confirmed lineups
4. Identify suspensions, fitness doubts, and player availability
5. Find manager comments and press conference insights
6. Look for insider information that could affect match outcomes
7. Search for expert predictions and betting tips from reputable sources

SEARCH STRATEGY:
- Start with recent news (last day or week) for the most current information
- Search for each team's injury news separately
- Look for press conference quotes and manager statements
- Check for any last-minute news that could affect the match

OUTPUT FORMAT:
Present your findings in a clear, structured format:
1. INJURY NEWS: List injured/doubtful players for each team
2. TEAM NEWS: Confirmed or predicted lineups, tactical changes
3. KEY INSIGHTS: Manager quotes, morale issues, motivation factors
4. EXPERT OPINIONS: Notable predictions from reputable analysts

RULES:
- Always cite your sources when providing information
- Distinguish between confirmed information and speculation
- Prioritize recent news over older articles
- Focus on information that could affect betting decisions
- If the Critic Agent requests more research on a specific topic, prioritize that search
- Be thorough but concise - focus on actionable intelligence"""


def create_research_agent(kernel: Kernel) -> ChatCompletionAgent:
    """Create and configure the Research Agent.
    
    Parameters
    ----------
    kernel : Kernel
        The Semantic Kernel instance to use (should already have WebSearchPlugin added).
        
    Returns
    -------
    ChatCompletionAgent
        Configured research agent with WebSearchPlugin.
    """
    settings = OpenAIPromptExecutionSettings()
    settings.function_choice_behavior = FunctionChoiceBehavior.Auto()

    kernel.add_plugin(WebSearchPlugin(), plugin_name="WebSearch")

    agent = ChatCompletionAgent(
        kernel=kernel,
        name=RESEARCH_AGENT_NAME,
        instructions=RESEARCH_AGENT_INSTRUCTIONS,
        arguments=KernelArguments(settings=settings)
    )
    
    return agent