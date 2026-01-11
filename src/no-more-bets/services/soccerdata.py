import os
import time
import random
import logging
from typing import Optional, Dict, Any
import requests
from requests.exceptions import ConnectionError, Timeout, RequestException
from utils.soccerdata_cache import SoccerDataCache

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
        self.cache = SoccerDataCache(
            store=store_cache,
            use_cache=use_cache,
            cache_ttl=cache_ttl,
        )
        
        # Default headers for all requests
        self.headers = {
            'Accept-Encoding': 'gzip',
            'Content-Type': 'application/json'
        }
    
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
    
    def get_country(self, params: Optional[Dict[str, Any]] = None) -> Dict[str, Any]:
        """Get country data from the SoccerData API.
        
        Parameters
        ----------
        params : Optional[Dict[str, Any]]
            Additional query parameters to pass to the API.
            
        Returns
        -------
        Dict[str, Any]
            JSON response containing country data.
            
        Raises
        ------
        RequestException
            If the API request fails.
        """
        try:
            return self._make_request('/country/', params=params)
        except Exception as e:
            logger.error(f"Error fetching country data: {e}")
            raise
