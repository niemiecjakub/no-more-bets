import os
import time
import json
import re
import logging
from typing import Optional, Dict, Any, Union, List
from .base_cache import BaseCache

logger = logging.getLogger(__name__)


class SoccerDataCache(BaseCache):
    """SoccerData cache manager with TTL support.
    
    Handles saving and loading JSON responses from disk cache with
    time-to-live (TTL) expiration.
    """
    
    def __init__(
        self,
        store_folder: str = "cache/soccerdata",
        store: bool = True,
        use_cache: bool = True,
        cache_ttl: float = 86400.0,
    ):
        """Initialize SoccerData cache manager.
        
        Parameters
        ----------
        store_folder : str
            Folder where cached JSON files are stored. Default is "cache/soccerdata".
        store : bool
            Whether to save fetched responses to cache folder. Default is True.
        use_cache : bool
            Whether to use cached responses if available. Default is True.
        cache_ttl : float
            Cache time-to-live in seconds. Default is 86400.0 (1 day).
        """
        super().__init__(store_folder, store, use_cache, cache_ttl)
    
    def _get_cache_key_to_filename(self, key: str, timestamp: Optional[int] = None) -> str:
        """Generate cache filename in ENDPOINT_DATE format.
        
        Parameters
        ----------
        key : str
            Cache key for the endpoint.
        timestamp : Optional[int]
            Unix timestamp. If None, uses current time.
            
        Returns
        -------
        str
            Cache filename.
        """
        if timestamp is None:
            timestamp = int(time.time())
        
        return f"{key}_{timestamp}.json"
    
    def _extract_timestamp_from_filename(self, filename: str) -> Optional[int]:
        """Extract timestamp from cache filename.
        
        Parameters
        ----------
        filename : str
            Filename with format: {cache_key}_{timestamp}.json
            
        Returns
        -------
        Optional[int]
            Timestamp if found, None otherwise.
        """
        # Match pattern: {cache_key}_{timestamp}.json
        match = re.search(r'_(\d+)\.json$', filename)
        if match:
            try:
                return int(match.group(1))
            except ValueError:
                return None
        return None
    
    def _find_cached_files(self, key: str) -> list[str]:
        """Find all cached files matching the cache key.
        
        Parameters
        ----------
        key : str
            Cache key for the endpoint.
            
        Returns
        -------
        list[str]
            List of full filepaths to cached files.
        """
        cached_files = []
        if not os.path.exists(self.store_folder):
            return cached_files
        
        for filename in os.listdir(self.store_folder):
            if filename.startswith(f"{key}_") and filename.endswith(".json"):
                filepath = os.path.join(self.store_folder, filename)
                cached_files.append(filepath)
        
        return cached_files
    
    def _read_file(self, filepath: str) -> Union[Dict[str, Any], List[Any]]:
        """Read and parse JSON file content.
        
        Parameters
        ----------
        filepath : str
            Path to the cached file.
            
        Returns
        -------
        Union[Dict[str, Any], List[Any]]
            Parsed JSON content (can be a dict or list).
        """
        with open(filepath, 'r', encoding='utf-8') as f:
            return json.load(f)
    
    def _write_file(self, filepath: str, data: Union[Dict[str, Any], List[Any]]):
        """Write JSON data to file.
        
        Parameters
        ----------
        filepath : str
            Path where to save the file.
        data : Union[Dict[str, Any], List[Any]]
            JSON data to write (can be a dict or list).
        """
        with open(filepath, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
    
    def load(self, cache_key: str) -> Optional[Union[Dict[str, Any], List[Any]]]:
        """Load cached response if available and not expired.
        
        Parameters
        ----------
        cache_key : str
            Cache key for the endpoint.
            
        Returns
        -------
        Optional[Union[Dict[str, Any], List[Any]]]
            Cached JSON response (dict or list) if found and valid, None otherwise.
        """
        result = super().load(cache_key)
        # Accept both dicts and lists (some endpoints return lists, e.g., /matches/)
        return result if isinstance(result, (dict, list)) else None
    
    def save(self, cache_key: str, data: Union[Dict[str, Any], List[Any]]):
        """Save response to cache.
        
        Removes old cached files for the same cache key before saving new one.
        
        Parameters
        ----------
        cache_key : str
            Cache key for the endpoint.
        data : Union[Dict[str, Any], List[Any]]
            JSON response data to cache (can be a dict or list).
        """
        super().save(cache_key, data)
    
    def clear(self, cache_key: str) -> int:
        """Clear all cached files for a specific cache key.
        
        Parameters
        ----------
        cache_key : str
            Cache key to clear cache for.
            
        Returns
        -------
        int
            Number of files removed.
        """
        return super().clear(cache_key)
