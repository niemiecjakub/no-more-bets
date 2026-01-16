import os
import time
import random
import logging
from typing import Optional, Dict, Any, List
import requests
from requests.exceptions import ConnectionError, Timeout, RequestException
from utils.json_cache import JsonCache
from models.soccerdata import HeadToHead, MatchPreview, LeagueMatchPreviews, LeagueMatches

logger = logging.getLogger(__name__)


class SoccerData:
    """SoccerData API service class for fetching data from api.soccerdataapi.com."""
    
    BASE_URL = "https://api.soccerdataapi.com"
    
    def __init__(
        self,
        retry_count: int = 3,
        retry_delay: float = 2.0,
        timeout: float = 15.0,
        use_cache: bool = True,
        store_cache: bool = True,
        cache_ttl: float = 86400.0,
    ):
        """Initialize SoccerData service.
        
        Parameters
        ----------
        retry_count : int
            Number of retry attempts if request fails. Default is 3.
        retry_delay : float
            Delay in seconds between retry attempts. Default is 2.0.
        timeout : float
            Request timeout in seconds. Default is 15.0.
        use_cache : bool
            Whether to use cached responses if available. Default is True.
        store_cache : bool
            Whether to save responses to cache. Default is True.
        cache_ttl : float
            Cache time-to-live in seconds. Default is 86400.0 (1 day).
            
        Raises
        ------
        ValueError
            If SOCCERDATA_API_KEY is not set in environment variables.
        """
        self.api_key = os.getenv("SOCCERDATA_API_KEY")
        if not self.api_key:
            raise ValueError(
                "SOCCERDATA_API_KEY is required. "
                "Set it in environment variables."
            )
        
        self.retry_count = retry_count
        self.retry_delay = retry_delay
        self.timeout = timeout
        
        # Initialize cache
        self.cache = JsonCache(
            store=store_cache,
            use_cache=use_cache,
            cache_ttl=cache_ttl,
        )
        
        # Default headers for all requests
        self.headers = {
            'Accept-Encoding': 'gzip',
            'Content-Type': 'application/json'
        }
    
    def get_match_previews_upcoming(self, league_id: Optional[int] = None) -> List[LeagueMatchPreviews]:
        """Get upcoming match previews from the SoccerData API.
        
        Parameters
        ----------
        league_id : Optional[int]
            If provided, only return matches for this league ID. Default is None (return all matches).
        
        Returns
        -------
        List[LeagueMatchPreviews]
            List of league match previews grouped by league.
            
        Raises
        ------
        RequestException
            If the API request fails.
        """
        try:
            response = self._make_request('/match-previews-upcoming/')
            results = []
            for idx, item in enumerate(response.get('results', [])):
                try:
                    league_preview = LeagueMatchPreviews(**item)
                    results.append(league_preview)
                except Exception as parse_error:
                    logger.warning(f"Failed to parse league item {idx}: {parse_error}")
            
            # Filter by league_id if provided
            if league_id is not None:
                filtered = [
                    league_preview for league_preview in results
                    if league_preview.league_id == league_id
                ]
                return filtered
            
            return results
        except Exception as e:
            logger.error(f"Error fetching match previews upcoming: {e}")
            raise
    
    def get_match_preview(self, match_id: int) -> MatchPreview:
        """Get match preview from the SoccerData API.
        
        Parameters
        ----------
        match_id : int
            Match ID to get preview for. Required parameter.
            
        Returns
        -------
        MatchPreview
            Match preview model containing match information, teams, weather, and preview content.
            
        Raises
        ------
        RequestException
            If the API request fails.
        """
        try:
            params = {'match_id': match_id}
            response = self._make_request('/match-preview/', params=params)
            return MatchPreview(**response)
        except Exception as e:
            logger.error(f"Error fetching match preview: {e}")
            raise
    
    def get_head_to_head(self, team_1_id: int, team_2_id: int) -> HeadToHead:
        """Get head-to-head data from the SoccerData API.
        
        Parameters
        ----------
        team_1_id : int
            First team ID. Required parameter.
        team_2_id : int
            Second team ID. Required parameter.
            
        Returns
        -------
        HeadToHead
            Head-to-head data model containing team information and statistics.
            
        Raises
        ------
        RequestException
            If the API request fails.
        """
        try:
            params = {
                'team_1_id': team_1_id,
                'team_2_id': team_2_id
            }
            response = self._make_request('/head-to-head/', params=params)
            return HeadToHead(**response)
        except Exception as e:
            logger.error(f"Error fetching head-to-head data: {e}")
            raise
    
    def get_matches(
        self,
        date: Optional[str] = None,
        league_id: Optional[int] = None,
        season: Optional[str] = None
    ) -> List[LeagueMatches]:
        """Get matches from the SoccerData API.
        
        Parameters can be used in the following combinations:
        - date alone: Get matches by date
        - league_id alone: Get matches by league_id for current season
        - league_id + season: Get matches by league and season
        - league_id + date: Get matches by league and date
        
        Parameters
        ----------
        date : Optional[str]
            Date to filter matches by. Can be used alone or with league_id.
        league_id : Optional[int]
            League ID to filter matches by. Can be used alone, with season, or with date.
        season : Optional[str]
            Season to filter matches by. Must be used together with league_id.
            
        Returns
        -------
        List[LeagueMatches]
            List of league matches models containing matches data grouped by league.
            
        Raises
        ------
        RequestException
            If the API request fails.
        """
        try:
            params = {}
            if date is not None:
                params['date'] = date
            if league_id is not None:
                params['league_id'] = league_id
            if season is not None:
                params['season'] = season
            
            response = self._make_request('/matches/', params=params if params else None)
            results = []
            for idx, item in enumerate(response):
                try:
                    league_matches = LeagueMatches(**item)
                    results.append(league_matches)
                except Exception as parse_error:
                    logger.warning(f"Failed to parse league matches item {idx}: {parse_error}")
            return results
        except Exception as e:
            logger.error(f"Error fetching matches: {e}")
            raise
    
    def _endpoint_to_cache_key(self, endpoint: str, params: Optional[Dict[str, Any]] = None) -> str:
        """Generate cache key from endpoint and parameters.
        
        Parameters
        ----------
        endpoint : str
            API endpoint path (e.g., '/country/').
        params : Optional[Dict[str, Any]]
            Additional query parameters (excluding auth_token).
            
        Returns
        -------
        str
            Cache key string.
        """
        # Normalize endpoint (remove leading/trailing slashes)
        endpoint_clean = endpoint.strip('/').replace('/', '_')
        
        # Add params to key if present (excluding auth_token)
        if params:
            # Create a sorted string representation of params for consistent keys
            filtered_params = {k: v for k, v in params.items() if k != 'auth_token'}
            if filtered_params:
                param_str = '_'.join(f"{k}_{v}" for k, v in sorted(filtered_params.items()))
                endpoint_clean = f"{endpoint_clean}_{param_str}"
        else:
            # Add trailing underscore when no params
            endpoint_clean = f"{endpoint_clean}_"
        
        return endpoint_clean
    
    def _make_request(
        self,
        endpoint: str,
        params: Optional[Dict[str, Any]] = None,
        method: str = "GET"
    ) -> Dict[str, Any]:
        """Make a request to the SoccerData API.
        
        Parameters
        ----------
        endpoint : str
            API endpoint path (e.g., '/country/').
        params : Optional[Dict[str, Any]]
            Additional query parameters. auth_token will be added automatically.
        method : str
            HTTP method. Default is "GET".
            
        Returns
        -------
        Dict[str, Any]
            JSON response from the API.
            
        Raises
        ------
        ValueError
            If endpoint is invalid.
        RequestException
            If request fails after all retry attempts.
        """
        if not endpoint.startswith('/'):
            endpoint = '/' + endpoint
        
        # Generate cache key
        cache_key = self._endpoint_to_cache_key(endpoint, params)
        
        # Check cache first
        cached_response = self.cache.load(cache_key)
        if cached_response is not None:
            logger.info(f"Cache hit for endpoint: {endpoint} (cache_key: {cache_key})")
            return cached_response
        
        url = f"{self.BASE_URL}{endpoint}"
        
        # Add auth_token to query parameters
        query_params = params.copy() if params else {}
        query_params['auth_token'] = self.api_key
        
        last_exception = None
        
        for attempt in range(1, self.retry_count + 1):
            try:
                if method.upper() == "GET":
                    response = requests.get(
                        url,
                        headers=self.headers,
                        params=query_params,
                        timeout=self.timeout
                    )
                else:
                    raise ValueError(f"Unsupported HTTP method: {method}")
                
                # Check if request was successful
                if response.ok:
                    json_response = response.json()
                    # Save to cache
                    self.cache.save(cache_key, json_response)
                    return json_response
                
                # Handle specific HTTP errors
                if response.status_code in {401, 403}:
                    raise ValueError(
                        f"Authentication failed ({response.status_code}). "
                        "Check your API key."
                    )
                
                if response.status_code == 404:
                    raise ValueError(
                        f"Endpoint not found ({response.status_code}): {endpoint}"
                    )
                
                # For other errors, log and retry
                last_exception = RequestException(
                    f"HTTP {response.status_code} for {url}: {response.text}"
                )
                
            except (ConnectionError, Timeout) as e:
                last_exception = e
                logger.warning(
                    f"Connection error on attempt {attempt}/{self.retry_count}: {e}"
                )
            
            # Retry with exponential backoff
            if attempt < self.retry_count:
                backoff = self.retry_delay * attempt
                jitter = random.uniform(0.5, 1.5)
                time.sleep(backoff * jitter)
        
        raise RequestException(
            f"Failed to fetch {url} after {self.retry_count} attempts"
        ) from last_exception