"""Persistence handler for match analysis results."""
import json
import logging
import os
from datetime import datetime
from typing import List, Optional
from models.match_analysis import MatchAnalysis

logger = logging.getLogger(__name__)


class MatchAnalysisPersistence:
    """Handles persistence of match analysis results to files."""
    
    def __init__(self, output_dir: Optional[str] = None):
        """Initialize persistence handler.
        
        Parameters
        ----------
        output_dir : Optional[str]
            Output directory for saving results. If None, defaults to cache/output
            relative to the package directory.
        """
        if output_dir is None:
            package_dir = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
            output_dir = os.path.join(package_dir, "cache", "no-more-bets", "output")
        
        self.output_dir = output_dir
    
    def save_results(self, results: List[MatchAnalysis]) -> str:
        """Save match analysis results to a JSON file.
        
        Parameters
        ----------
        results : List[MatchAnalysis]
            List of match analysis results to save.
            
        Returns
        -------
        str
            Path to the saved file.
        """
        os.makedirs(self.output_dir, exist_ok=True)

        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        output_file = os.path.join(self.output_dir, f"match_analysis_{timestamp}.json")
        
        serialized_results = [result.model_dump() for result in results]
        
        with open(output_file, "w", encoding="utf-8") as f:
            json.dump(serialized_results, f, indent=2, ensure_ascii=False)
        
        logger.info(f"Results saved to: {output_file}")
        
        return output_file
