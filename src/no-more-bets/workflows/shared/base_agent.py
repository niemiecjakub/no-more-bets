"""Base agent class with common functionality."""
import json
import logging
from abc import ABC, abstractmethod
from typing import TypeVar, Generic
from pydantic import BaseModel, ValidationError
from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import OpenAIPromptExecutionSettings
from semantic_kernel.agents import ChatCompletionAgent
from semantic_kernel.functions import KernelArguments
from semantic_kernel.connectors.ai import FunctionChoiceBehavior

from .llm_client import LLMClient

logger = logging.getLogger(__name__)

T = TypeVar('T', bound=BaseModel)


class BaseAgent(ABC, Generic[T]):
    """Base agent class providing common functionality for workflow agents.
    
    This class handles:
    - Common agent initialization
    - Shared prompt template utilities
    - Structured output parsing (JSON schema validation)
    - Error handling patterns
    """
    
    def __init__(
        self,
        name: str,
        instructions: str,
        llm_client: LLMClient | None = None,
        output_model: type[T] | None = None
    ):
        """Initialize the base agent.
        
        Parameters
        ----------
        name : str
            Agent name identifier.
        instructions : str
            System instructions for the agent.
        llm_client : LLMClient | None
            LLM client instance. If None, creates a new one.
        output_model : type[T] | None
            Pydantic model class for structured output validation.
        """
        self.name = name
        self.instructions = instructions
        self.llm_client = llm_client or LLMClient()
        self.output_model = output_model
        self._kernel: Kernel | None = None
        self._agent: ChatCompletionAgent | None = None
        self._agent: ChatCompletionAgent | None = None
    
    @property
    def kernel(self) -> Kernel:
        """Get or create the Kernel instance.
        
        Returns
        -------
        Kernel
            Semantic Kernel instance.
        """
        if self._kernel is None:
            self._kernel = self.llm_client.create_kernel()
        return self._kernel
    
    @property
    def agent(self) -> ChatCompletionAgent:
        """Get or create the ChatCompletionAgent instance.
        
        Returns
        -------
        ChatCompletionAgent
            Configured chat completion agent.
        """
        if self._agent is None:
            settings = OpenAIPromptExecutionSettings()
            settings.function_choice_behavior = FunctionChoiceBehavior.Auto()
            
            self._agent = ChatCompletionAgent(
                kernel=self.kernel,
                name=self.name,
                instructions=self.instructions,
                arguments=KernelArguments(settings=settings)
            )
        return self._agent
    
    def parse_structured_output(self, response: str) -> T:
        """Parse LLM response into structured Pydantic model.
        
        Parameters
        ----------
        response : str
            Raw LLM response text.
            
        Returns
        -------
        T
            Parsed Pydantic model instance.
            
        Raises
        ------
        ValueError
            If output_model is not set or parsing fails.
        """
        if self.output_model is None:
            raise ValueError(f"output_model not set for agent {self.name}")
        
        try:
            # Try to extract JSON from response (handle markdown code blocks)
            json_str = self._extract_json(response)
            data = json.loads(json_str)
            return self.output_model.model_validate(data)
        except json.JSONDecodeError as e:
            logger.error(f"Failed to parse JSON from response: {e}")
            logger.debug(f"Response content: {response[:500]}")
            raise ValueError(f"Invalid JSON in agent response: {e}")
        except ValidationError as e:
            logger.error(f"Failed to validate output model: {e}")
            raise ValueError(f"Output validation failed: {e}")
    
    def _extract_json(self, text: str) -> str:
        """Extract JSON from text, handling markdown code blocks.
        
        Parameters
        ----------
        text : str
            Text that may contain JSON in code blocks.
            
        Returns
        -------
        str
            Extracted JSON string.
        """
        # Remove markdown code blocks if present
        if "```json" in text:
            start = text.find("```json") + 7
            end = text.find("```", start)
            return text[start:end].strip()
        elif "```" in text:
            start = text.find("```") + 3
            end = text.find("```", start)
            return text[start:end].strip()
        
        # Try to find JSON object boundaries
        start = text.find("{")
        end = text.rfind("}") + 1
        
        if start >= 0 and end > start:
            return text[start:end]
        
        return text.strip()
    
    @abstractmethod
    def create_prompt(self, context: str) -> str:
        """Create the prompt for the agent.
        
        Parameters
        ----------
        context : str
            Context data (e.g., serialized MatchAnalysis).
            
        Returns
        -------
        str
            Complete prompt string.
        """
        pass
