import os
import time
import re
import logging
from abc import ABC, abstractmethod
from typing import Optional, Any

logger = logging.getLogger(__name__)


class BaseCache(ABC):
    """Base cache manager with TTL support.
    
    Abstract base class for cache implementations that handle saving and loading
    content from disk cache with time-to-live (TTL) expiration.
    """
    
    def __init__(
        self,
        store_folder: str,
        store: bool = True,
        use_cache: bool = True,
        cache_ttl: float = 3600.0,
    ):
        """Initialize base cache manager.
        
        Parameters
        ----------
        store_folder : str
            Folder where cached files are stored.
        store : bool
            Whether to save fetched content to cache folder. Default is True.
        use_cache : bool
            Whether to use cached content if available. Default is True.
        cache_ttl : float
            Cache time-to-live in seconds. Default is 3600.0 (1 hour).
        """
        self.store_folder = store_folder
        self.store = store
        self.use_cache = use_cache
        self.cache_ttl = cache_ttl
        
        # Create store folder if storing or caching is enabled
        if self.store or self.use_cache:
            os.makedirs(self.store_folder, exist_ok=True)
    
    @abstractmethod
    def _get_cache_key_to_filename(self, key: str, timestamp: Optional[int] = None) -> str:
        """Generate cache filename from key.
        
        Parameters
        ----------
        key : str
            Cache key (e.g., URL or endpoint key).
        timestamp : Optional[int]
            Unix timestamp. If None, uses current time.
            
        Returns
        -------
        str
            Cache filename.
        """
        pass
    
    @abstractmethod
    def _extract_timestamp_from_filename(self, filename: str) -> Optional[int]:
        """Extract timestamp from cache filename.
        
        Parameters
        ----------
        filename : str
            Cache filename.
            
        Returns
        -------
        Optional[int]
            Timestamp if found, None otherwise.
        """
        pass
    
    @abstractmethod
    def _find_cached_files(self, key: str) -> list[str]:
        """Find all cached files matching the key.
        
        Parameters
        ----------
        key : str
            Cache key to find files for.
            
        Returns
        -------
        list[str]
            List of full filepaths to cached files.
        """
        pass
    
    @abstractmethod
    def _read_file(self, filepath: str) -> Any:
        """Read and parse file content.
        
        Parameters
        ----------
        filepath : str
            Path to the cached file.
            
        Returns
        -------
        Any
            Parsed file content (string, dict, etc.).
            
        Raises
        ------
        Exception
            If file reading or parsing fails.
        """
        pass
    
    @abstractmethod
    def _write_file(self, filepath: str, data: Any):
        """Write data to file.
        
        Parameters
        ----------
        filepath : str
            Path where to save the file.
        data : Any
            Data to write (string, dict, etc.).
            
        Raises
        ------
        Exception
            If file writing fails.
        """
        pass
    
    def load(self, key: str) -> Optional[Any]:
        """Load cached content if available and not expired.
        
        Parameters
        ----------
        key : str
            Cache key to load.
            
        Returns
        -------
        Optional[Any]
            Cached content if found and valid, None otherwise.
        """
        if not self.use_cache:
            return None
        
        # Find all cached files for this key
        cached_files = self._find_cached_files(key)
        
        if not cached_files:
            return None
        
        # Find the most recent valid cache file
        current_time = time.time()
        valid_filepath = None
        best_timestamp = None
        
        for filepath in cached_files:
            filename = os.path.basename(filepath)
            timestamp = self._extract_timestamp_from_filename(filename)
            
            if timestamp is None:
                continue
            
            # Check if cache is still valid (within TTL)
            age = current_time - timestamp
            if age < self.cache_ttl:
                # Cache is valid, use the most recent one
                if best_timestamp is None or timestamp > best_timestamp:
                    valid_filepath = filepath
                    best_timestamp = timestamp
        
        if valid_filepath:
            try:
                data = self._read_file(valid_filepath)
                logger.info(f"Cache loaded for key: {key}")
                return data
            except Exception as e:
                logger.warning(f"Failed to load cache from {valid_filepath}: {e}")
                return None
        
        return None
    
    def save(self, key: str, data: Any):
        """Save content to cache.
        
        Removes old cached files for the same key before saving new one.
        
        Parameters
        ----------
        key : str
            Cache key.
        data : Any
            Content to cache.
        """
        if not self.store:
            return
        
        # Remove old cached files for this key
        old_files = self._find_cached_files(key)
        for old_file in old_files:
            try:
                os.remove(old_file)
            except Exception as e:
                logger.warning(f"Failed to remove old cache file {old_file}: {e}")
        
        # Save new cache file
        cache_filename = self._get_cache_key_to_filename(key)
        cache_filepath = os.path.join(self.store_folder, cache_filename)
        
        try:
            self._write_file(cache_filepath, data)
        except Exception as e:
            logger.warning(f"Failed to save cache to {cache_filepath}: {e}")
    
    def clear(self, key: str) -> int:
        """Clear all cached files for a specific key.
        
        Parameters
        ----------
        key : str
            Cache key to clear cache for.
            
        Returns
        -------
        int
            Number of files removed.
        """
        cached_files = self._find_cached_files(key)
        removed_count = 0
        
        for filepath in cached_files:
            try:
                if os.path.exists(filepath):
                    os.remove(filepath)
                    removed_count += 1
            except Exception as e:
                logger.warning(f"Failed to remove cache file {filepath}: {e}")
        
        return removed_count
