"""Serialization utilities for MatchAnalysis."""
import json
import logging
from typing import Optional
from models.match_analysis import MatchAnalysis

logger = logging.getLogger(__name__)


def serialize_match_analysis(match_analysis: MatchAnalysis) -> str:
    """Convert MatchAnalysis to JSON string for LLM context.
    
    Parameters
    ----------
    match_analysis : MatchAnalysis
        The match analysis to serialize.
        
    Returns
    -------
    str
        JSON string representation of the match analysis.
    """
    try:
        # Use Pydantic's model_dump_json for proper serialization
        json_str = match_analysis.model_dump_json(indent=2)
        return json_str
    except Exception as e:
        logger.error(f"Failed to serialize MatchAnalysis: {e}")
        raise


def validate_match_analysis(match_analysis: MatchAnalysis) -> bool:
    """Check if MatchAnalysis has sufficient data for processing.
    
    Parameters
    ----------
    match_analysis : MatchAnalysis
        The match analysis to validate.
        
    Returns
    -------
    bool
        True if match analysis has sufficient data, False otherwise.
    """
    # At minimum, we need match_info
    if not match_analysis.match_info:
        logger.warning("MatchAnalysis missing match_info")
        return False
    
    # Check if we have at least some data for analysis
    has_data = (
        match_analysis.lineup is not None or
        match_analysis.head_to_head is not None or
        match_analysis.match_preview is not None or
        match_analysis.betting_events is not None or
        match_analysis.fbref_home is not None or
        match_analysis.fbref_away is not None
    )
    
    if not has_data:
        logger.warning("MatchAnalysis has no additional data beyond match_info")
    
    return True
