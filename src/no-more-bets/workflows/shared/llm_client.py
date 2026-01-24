"""LLM client configuration and base client for workflows."""
import logging
from semantic_kernel import Kernel
from semantic_kernel.connectors.ai.open_ai import OpenAIChatCompletion
from config import Config

logger = logging.getLogger(__name__)


class LLMClient:
    """LLM client for workflow agents.
    
    Provides a configured Semantic Kernel instance with OpenAI service.
    Handles retries and error handling for LLM interactions.
    """
    
    def __init__(self, api_key: str | None = None, model: str | None = None):
        """Initialize the LLM client.
        
        Parameters
        ----------
        api_key : str | None
            OpenAI API key. If None, uses Config.OPENAI_API_KEY.
        model : str | None
            OpenAI model ID. If None, uses Config.OPENAI_MODEL.
        """
        self.api_key = api_key or Config.OPENAI_API_KEY
        self.model = model or Config.OPENAI_MODEL
        
        if not self.api_key:
            raise ValueError("OpenAI API key is required. Set OPENAI_API_KEY in environment or pass api_key parameter.")
    
    def create_kernel(self) -> Kernel:
        """Create a configured Kernel instance.
        
        Returns
        -------
        Kernel
            Configured Semantic Kernel with OpenAI chat completion service.
        """
        kernel = Kernel()
        kernel.add_service(OpenAIChatCompletion(
            api_key=self.api_key,
            ai_model_id=self.model
        ))
        logger.debug(f"Created Kernel with model: {self.model}")
        return kernel
    
    def get_model_id(self) -> str:
        """Get the configured model ID.
        
        Returns
        -------
        str
            The model ID being used.
        """
        return self.model
