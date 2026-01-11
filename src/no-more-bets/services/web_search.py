from typing import Optional
from ddgs import DDGS
from models.web import TextSearchResult, NewsSearchResult
import logging

logger = logging.getLogger(__name__)


class WebSearch:
    """Web search class using DuckDuckGo Search (DDGS) for internet searches."""
    
    FOOTBALL_SITES = [
        "bbc.com",
        "skysports.com",
        "theguardian.com",
        "theathletic.com",
        "premierleague.com",
        "espn.com",
    ]
    
    def __init__(
        self,
        max_results: int = 10,
        region: str = "us-en",
        safesearch: str = "moderate",
    ):
        """Initialize WebSearch instance.
        
        Parameters
        ----------
        max_results : int
            Default maximum number of results to return. Default is 10.
        region : str
            Region code for localized search (e.g., 'us-en', 'uk-en'). Default is 'us-en'.
        safesearch : str
            Safe search setting: 'on', 'moderate', or 'off'. Default is 'moderate'.
        """
        self.max_results = max_results
        self.region = region
        self.safesearch = safesearch

    def search(
        self,
        query: str,
        max_results: Optional[int] = None,
        timelimit: Optional[str] = None,
        backend: str = "auto"
    ) -> list[TextSearchResult]:
        """Perform a general web search.
        
        Parameters
        ----------
        query : str
            Search query string.
        max_results : Optional[int]
            Maximum number of results to return. If None, uses instance default.
        timelimit : Optional[str]
            Time limit for results ('d' for day, 'w' for week, 'm' for month, 'y' for year).
        backend : str
            Search backend to use (e.g., 'auto', 'google', 'bing'). Default is 'auto'.
            
        Returns
        -------
        list[TextSearchResult]
            List of TextSearchResult objects from the search.
        """
        max_results = max_results or self.max_results
        
        try:
            results = DDGS().text(
                query=query,
                region=self.region,
                safesearch=self.safesearch,
                timelimit=timelimit,
                max_results=max_results,
                backend=backend
            )
            return [
                TextSearchResult(
                    title=r.get("title", ""),
                    href=r.get("href", ""),
                    body=r.get("body", ""),
                    date=r.get("date")
                )
                for r in results
            ]
        except Exception as e:
            logger.error(f"Error in search for query '{query}': {e}")
            return []
    
    def news_search(
        self,
        query: str,
        max_results: Optional[int] = None,
        timelimit: Optional[str] = None
    ) -> list[NewsSearchResult]:
        """Search for news articles.
        
        Parameters
        ----------
        query : str
            Search query string.
        max_results : Optional[int]
            Maximum number of results to return. If None, uses instance default.
        timelimit : Optional[str]
            Time limit for results ('d' for day, 'w' for week, 'm' for month, 'y' for year).
            
        Returns
        -------
        list[NewsSearchResult]
            List of NewsSearchResult objects from the news search.
        """
        max_results = max_results or self.max_results
        
        try:
            results = DDGS().news(
                query=query,
                region=self.region,
                safesearch=self.safesearch,
                timelimit=timelimit,
                max_results=max_results
            )
            return [
                NewsSearchResult(
                    title=r.get("title", ""),
                    url=r.get("url", ""),
                    body=r.get("body", ""),
                    date=r.get("date"),
                    image=r.get("image"),
                    source=r.get("source")
                )
                for r in results
            ]
        except Exception as e:
            logger.error(f"Error in news_search for query '{query}': {e}")
            return []
    
    def site_search(
        self,
        query: str,
        sites: list[str],
        max_results: Optional[int] = None,
        timelimit: Optional[str] = None,
        backend: str = "auto"
    ) -> list[TextSearchResult]:
        """Search for content on specific sites.
        
        Parameters
        ----------
        query : str
            Search query string.
        sites : list[str]
            List of site domains to search (e.g., ['bbc.com', 'espn.com']).
        max_results : Optional[int]
            Maximum number of results to return. If None, uses instance default.
        timelimit : Optional[str]
            Time limit for results ('d' for day, 'w' for week, 'm' for month, 'y' for year).
        backend : str
            Search backend to use (e.g., 'auto', 'google', 'bing'). Default is 'auto'.
            
        Returns
        -------
        list[TextSearchResult]
            List of TextSearchResult objects from the specified sites.
        """
        if not sites:
            return []
        
        max_results = max_results or self.max_results
        results_per_site = max(1, max_results // len(sites))
        all_results: list[TextSearchResult] = []
        
        for site in sites:
            try:
                site_query = f"site:{site} {query}"
                results = DDGS().text(
                    query=site_query,
                    region=self.region,
                    safesearch=self.safesearch,
                    timelimit=timelimit,
                    max_results=results_per_site,
                    backend=backend
                )
                
                for r in results:
                    all_results.append(
                        TextSearchResult(
                            title=r.get("title", ""),
                            href=r.get("href", ""),
                            body=r.get("body", ""),
                            date=r.get("date")
                        )
                    )
            except Exception as e:
                logger.warning(f"Error searching site '{site}' for query '{query}': {e}")
                continue
        
        return all_results
    
    def football_search(
        self,
        query: str,
        max_results: Optional[int] = None,
        timelimit: Optional[str] = None,
        backend: str = "auto"
    ) -> list[TextSearchResult]:
        """Search for football news and analytics on major football sites.
        
        Searches across: BBC, Sky Sports, The Guardian, The Athletic, Premier League, ESPN.
        
        Parameters
        ----------
        query : str
            Search query string.
        max_results : Optional[int]
            Maximum number of results to return. If None, uses instance default.
        timelimit : Optional[str]
            Time limit for results ('d' for day, 'w' for week, 'm' for month, 'y' for year).
        backend : str
            Search backend to use (e.g., 'auto', 'google', 'bing'). Default is 'auto'.
            
        Returns
        -------
        list[TextSearchResult]
            List of TextSearchResult objects from football news/analytics sites.
        """
        return self.site_search(
            query=query,
            sites=self.FOOTBALL_SITES,
            max_results=max_results,
            timelimit=timelimit,
            backend=backend
        )
    