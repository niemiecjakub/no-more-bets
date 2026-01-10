from typing import Annotated
from semantic_kernel.functions import kernel_function
from services.web_search import WebSearch
from models import TextSearchResult, NewsSearchResult


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
    ) -> list[TextSearchResult]:
        """Search for football news from major sports sites.
        
        Returns a list of TextSearchResult objects.
        """
        return self._web_search.football_search(
            query=query,
            timelimit=timelimit,
            max_results=8
        )

    
    @kernel_function(
        name="search_news",
        description="Search for general news articles. Use this for injury reports, transfer news, team news, manager comments, and insider information that may affect match outcomes."
    )
    def search_news(
        self,
        query: Annotated[str, "Search query for news (e.g., 'Arsenal injuries January 2026')"],
        timelimit: Annotated[str, "Time limit: 'd' for day, 'w' for week, 'm' for month. Default is 'd'"] = "d"
    ) -> list[NewsSearchResult]:
        """Search for general news articles.
        
        Returns a list of NewsSearchResult objects.
        """
        return self._web_search.news_search(
            query=query,
            timelimit=timelimit,
        )

    
    @kernel_function(
        name="search_web",
        description="General web search for any football-related information. Use when you need broader search results beyond news articles."
    )
    def search_web(
        self,
        query: Annotated[str, "Search query"],
        timelimit: Annotated[str, "Time limit: 'd' for day, 'w' for week, 'm' for month, 'y' for year. Default is 'w'"] = "w"
    ) -> list[TextSearchResult]:
        """Perform a general web search.
        
        Returns a list of TextSearchResult objects.
        """
        return self._web_search.search(
            query=query,
            timelimit=timelimit,
        )

