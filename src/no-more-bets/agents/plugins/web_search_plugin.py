"""Web Search Plugin for Semantic Kernel agents."""

import sys
import os
from typing import Annotated

# Add parent directory to path for imports
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))

from semantic_kernel.functions import kernel_function
from services.web_search import WebSearch
from models import FootballNewsSearchResponse, GeneralNewsSearchResponse, WebSearchResponse


class WebSearchPlugin:
    """Plugin for searching football news and general information on the internet.
    
    Provides functions for agents to search for pre-match news, injury reports,
    insider information, and expert opinions from major football news sources.
    """
    
    def __init__(self):
        """Initialize the WebSearchPlugin with a WebSearch instance."""
        self._web_search = WebSearch(region="uk-en", max_results=10)
    
    @kernel_function(
        name="search_football_news",
        description="Search for football news and analysis from major sports sites (BBC, Sky Sports, The Guardian, The Athletic, Premier League, ESPN). Use this for match previews, tactical analysis, and expert opinions."
    )
    def search_football_news(
        self,
        query: Annotated[str, "Search query for football news (e.g., 'Arsenal vs Liverpool preview')"],
        timelimit: Annotated[str, "Time limit: 'd' for day, 'w' for week, 'm' for month. Default is 'w'"] = "w"
    ) -> FootballNewsSearchResponse:
        """Search for football news from major sports sites.
        
        Returns structured response with search results from major football news sources.
        """
        results = self._web_search.football_search(
            query=query,
            timelimit=timelimit,
            max_results=8
        )
        
        return FootballNewsSearchResponse(
            query=query,
            timelimit=timelimit,
            results=results if results else [],
            result_count=len(results) if results else 0
        )
    
    @kernel_function(
        name="search_news",
        description="Search for general news articles. Use this for injury reports, transfer news, team news, manager comments, and insider information that may affect match outcomes."
    )
    def search_news(
        self,
        query: Annotated[str, "Search query for news (e.g., 'Arsenal injuries January 2026')"],
        timelimit: Annotated[str, "Time limit: 'd' for day, 'w' for week, 'm' for month. Default is 'd'"] = "d"
    ) -> GeneralNewsSearchResponse:
        """Search for general news articles.
        
        Returns structured response with news articles including titles, snippets, sources, and dates.
        """
        results = self._web_search.news_search(
            query=query,
            timelimit=timelimit,
            max_results=10
        )
        
        return GeneralNewsSearchResponse(
            query=query,
            timelimit=timelimit,
            results=results if results else [],
            result_count=len(results) if results else 0
        )
    
    @kernel_function(
        name="search_web",
        description="General web search for any football-related information. Use when you need broader search results beyond news articles."
    )
    def search_web(
        self,
        query: Annotated[str, "Search query"],
        timelimit: Annotated[str, "Time limit: 'd' for day, 'w' for week, 'm' for month, 'y' for year. Default is 'w'"] = "w"
    ) -> WebSearchResponse:
        """Perform a general web search.
        
        Returns structured response with web search results including titles, snippets, and URLs.
        """
        results = self._web_search.search(
            query=query,
            timelimit=timelimit,
            max_results=10
        )
        
        return WebSearchResponse(
            query=query,
            timelimit=timelimit,
            results=results if results else [],
            result_count=len(results) if results else 0
        )
