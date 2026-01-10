import os

from semantic_kernel import Kernel
from semantic_kernel.agents import AgentGroupChat, ChatCompletionAgent
from semantic_kernel.agents.strategies import (
    KernelFunctionSelectionStrategy,
    KernelFunctionTerminationStrategy,
)
from semantic_kernel.connectors.ai.open_ai import OpenAIChatCompletion
from semantic_kernel.contents import ChatHistoryTruncationReducer
from semantic_kernel.functions import KernelFunctionFromPrompt
from semantic_kernel.prompt_template import PromptTemplateConfig, InputVariable

from .research_agent import RESEARCH_AGENT_NAME, create_research_agent
from .analytics_agent import ANALYTICS_AGENT_NAME, create_analytics_agent
from .betting_agent import BETTING_AGENT_NAME, create_betting_agent
from .critic_agent import CRITIC_AGENT_NAME, create_critic_agent
from .filters import plugin_usage_logger_filter

def create_kernel() -> Kernel:
    """Create a shared Kernel instance with OpenAI service.
    
    Returns
    -------
    Kernel
        Configured kernel with OpenAI chat completion.
    """
    kernel = Kernel()
    kernel.add_filter('function_invocation', plugin_usage_logger_filter)
    kernel.add_service(OpenAIChatCompletion(
        api_key=os.getenv("OPENAI_API_KEY"),
        ai_model_id=os.getenv("OPENAI_MODEL")
    ))
    return kernel


def create_agents(kernel: Kernel) -> tuple[ChatCompletionAgent, ChatCompletionAgent, ChatCompletionAgent, ChatCompletionAgent]:
    """Create all four agents with their respective plugins.
    
    Parameters
    ----------
    kernel : Kernel
        Shared kernel instance.
        
    Returns
    -------
    tuple
        Tuple of (research_agent, analytics_agent, betting_agent, critic_agent).
    """

    research_agent = create_research_agent(kernel.clone())
    analytics_agent = create_analytics_agent(kernel.clone())
    betting_agent = create_betting_agent(kernel.clone())
    critic_agent = create_critic_agent(kernel.clone())
    
    return research_agent, analytics_agent, betting_agent, critic_agent


def create_selection_strategy(kernel: Kernel) -> KernelFunctionSelectionStrategy:
    """Create the selection strategy for determining next speaker.
    
    The flow is:
    - User input → Research Agent
    - Research Agent → Analytics Agent
    - Analytics Agent → Betting Agent
    - Betting Agent → Critic Agent
    - Critic Agent → Research Agent (if rejection) or terminate (if approved)
    
    Parameters
    ----------
    kernel : Kernel
        Kernel for executing the selection function.
        
    Returns
    -------
    KernelFunctionSelectionStrategy
        Configured selection strategy.
    """
    selection_prompt = f"""
Examine the RESPONSE and determine which agent should speak next.
Return ONLY the agent name, nothing else.

AGENTS:
- {RESEARCH_AGENT_NAME}: Searches for news, injuries, insider info
- {ANALYTICS_AGENT_NAME}: Analyzes statistics and historical data  
- {BETTING_AGENT_NAME}: Creates betting coupons
- {CRITIC_AGENT_NAME}: Reviews and approves/rejects coupons

SELECTION RULES:
1. If RESPONSE is from user (initial query) → {RESEARCH_AGENT_NAME}
2. If RESPONSE is from {RESEARCH_AGENT_NAME} → {ANALYTICS_AGENT_NAME}
3. If RESPONSE is from {ANALYTICS_AGENT_NAME} → {BETTING_AGENT_NAME}
4. If RESPONSE is from {BETTING_AGENT_NAME} → {CRITIC_AGENT_NAME}
5. If RESPONSE is from {CRITIC_AGENT_NAME} and contains "APPROVED" → {CRITIC_AGENT_NAME} (will terminate)
6. If RESPONSE is from {CRITIC_AGENT_NAME} and contains rejection/request → {RESEARCH_AGENT_NAME}

RESPONSE:
{{{{$lastmessage}}}}

Next agent:"""

    selection_config = PromptTemplateConfig(
        template=selection_prompt,
        input_variables=[
            InputVariable(name="lastmessage", allow_dangerously_set_content=True)
        ]
    )
    
    selection_function = KernelFunctionFromPrompt(
        function_name="select_next_agent",
        prompt_template_config=selection_config,
    )
    
    history_reducer = ChatHistoryTruncationReducer(target_count=5)
    
    return KernelFunctionSelectionStrategy(
        function=selection_function,
        kernel=kernel,
        result_parser=lambda result: str(result.value[0]).strip() if result.value and result.value[0] else RESEARCH_AGENT_NAME,
        history_variable_name="lastmessage",
        history_reducer=history_reducer,
    )


def create_termination_strategy(
    kernel: Kernel,
    critic_agent: ChatCompletionAgent
) -> KernelFunctionTerminationStrategy:
    """Create the termination strategy for ending the chat.
    
    The chat terminates when:
    - Critic Agent says "APPROVED"
    - Maximum iterations reached (12)
    
    Parameters
    ----------
    kernel : Kernel
        Kernel for executing the termination function.
    critic_agent : ChatCompletionAgent
        The critic agent (only this agent can approve).
        
    Returns
    -------
    KernelFunctionTerminationStrategy
        Configured termination strategy.
    """
    termination_keyword = "approved"
    
    termination_prompt = f"""
Examine the RESPONSE and determine if the betting coupon has been approved.

The conversation should TERMINATE if:
- The response contains the word "APPROVED" (case insensitive)
- The Critic Agent has given final approval

The conversation should CONTINUE if:
- The Critic requests more research
- The Critic identifies issues that need addressing
- The coupon has not been reviewed yet

RESPONSE:
{{{{$lastmessage}}}}

If the response indicates APPROVAL, respond with exactly: {termination_keyword}
Otherwise, respond with: continue"""

    termination_config = PromptTemplateConfig(
        template=termination_prompt,
        input_variables=[
            InputVariable(name="lastmessage", allow_dangerously_set_content=True)
        ]
    )
    
    termination_function = KernelFunctionFromPrompt(
        function_name="check_termination",
        prompt_template_config=termination_config,
    )
    
    history_reducer = ChatHistoryTruncationReducer(target_count=5)
    
    return KernelFunctionTerminationStrategy(
        agents=[critic_agent],
        function=termination_function,
        kernel=kernel,
        result_parser=lambda result: termination_keyword in str(result.value[0]).lower() if result.value and result.value[0] else False,
        history_variable_name="lastmessage",
        maximum_iterations=5,
        history_reducer=history_reducer,
    )


def create_group_chat() -> AgentGroupChat:
    """Create and configure the AgentGroupChat for betting analysis.
    
    Returns
    -------
    AgentGroupChat
        Fully configured group chat with all agents and strategies.
    """
    # Create shared kernel
    kernel = create_kernel()
    
    # Create agents
    research_agent, analytics_agent, betting_agent, critic_agent = create_agents(kernel)
    
    # Create strategies
    selection_strategy = create_selection_strategy(kernel.clone())
    termination_strategy = create_termination_strategy(kernel.clone(), critic_agent)
    
    # Create group chat
    chat = AgentGroupChat(
        agents=[research_agent, analytics_agent, betting_agent, critic_agent],
        selection_strategy=selection_strategy,
        termination_strategy=termination_strategy,
    )
    
    return chat


async def run_betting_analysis(query: str, verbose: bool = True) -> str:
    """Run the betting analysis group chat for a given query.
    
    Parameters
    ----------
    query : str
        The match or betting query (e.g., "Analyze Arsenal vs Liverpool match").
    verbose : bool
        If True, print agent responses as they occur. Default is True.
        
    Returns
    -------
    str
        The final approved betting coupon or last response.
    """
    chat = create_group_chat()
    
    if verbose:
        print(f"\n{'='*70}")
        print(f"BETTING ANALYSIS: {query}")
        print(f"{'='*70}\n")
    
    # Add the user query to start the conversation
    await chat.add_chat_message(message=query)
    
    final_response = ""
    chat_history = []
    is_approved = False
    
    try:
        async for response in chat.invoke():
            if response is None or not response.name:
                continue
            
            final_response = response.content
            chat_history.append(response)
            
            # Check if coupon was approved
            if response.name == CRITIC_AGENT_NAME and "APPROVED" in response.content.upper():
                is_approved = True
            
            if verbose:
                print(f"\n{'='*50}")
                print(f"[{response.name.upper()}]")
                print(f"{'='*50}")
                print(response.content)
                print()
                
    except Exception as e:
        error_msg = f"Error during chat: {str(e)}"
        if verbose:
            print(error_msg)
        return error_msg
    
    return final_response