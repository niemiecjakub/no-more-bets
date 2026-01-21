"""Configuration management for the application.

This module provides a centralized Config class that loads environment variables
from a .env file and exposes them as class attributes.
"""
import os
from dotenv import load_dotenv


class Config:
    """Centralized configuration class that loads and exposes environment variables.
    
    This class automatically loads environment variables from a .env file on import
    and provides access to configuration values via class attributes.
    """
    
    # Load environment variables from .env file
    load_dotenv()
    
    # API Keys
    SOCCERDATA_API_KEY: str | None = os.getenv("SOCCERDATA_API_KEY")
    OPENAI_API_KEY: str | None = os.getenv("OPENAI_API_KEY")
    
    # OpenAI Configuration
    OPENAI_MODEL: str = os.getenv("OPENAI_MODEL", "gpt-4o")
    
    # Logging Configuration
    LOG_AGENT_FUNCTION_CALLS: bool = os.getenv("LOG_AGENT_FUNCTION_CALLS", "false").lower() == "true"
