import os
import re
import time
from typing import Optional
from urllib.parse import urlparse, quote
from .base_cache import BaseCache


class HtmlCache(BaseCache):
    """HTML cache manager with TTL support.
    
    Handles saving and loading HTML content from disk cache with
    time-to-live (TTL) expiration.
    """
    
    def __init__(
        self,
        store_folder: str = "cache/html",
        store: bool = True,
        use_cache: bool = True,
        cache_ttl: float = 3600.0,
    ):
        """Initialize HTML cache manager.
        
        Parameters
        ----------
        store_folder : str
            Folder where cached HTML files are stored. Default is "cache/html".
        store : bool
            Whether to save fetched HTML to cache folder. Default is True.
        use_cache : bool
            Whether to use cached HTML if available. Default is True.
        cache_ttl : float
            Cache time-to-live in seconds. Default is 3600.0 (1 hour).
        """
        super().__init__(store_folder, store, use_cache, cache_ttl)
    
    def _url_to_filename(self, url: str, include_timestamp: bool = False) -> str:
        """Convert URL to a safe filename.
        
        Parameters
        ----------
        url : str
            URL to convert.
        include_timestamp : bool
            Whether to include timestamp in filename. Default is False.
            
        Returns
        -------
        str
            Safe filename derived from URL, optionally with timestamp.
        """
        parsed = urlparse(url)
        # Combine domain and path, replace invalid characters
        path = parsed.netloc + parsed.path
        if parsed.query:
            path += "?" + parsed.query
        
        # Replace invalid filesystem characters
        filename = path.replace("/", "_").replace("\\", "_").replace(":", "_")
        filename = filename.replace("?", "_").replace("*", "_").replace('"', "_")
        filename = filename.replace("<", "_").replace(">", "_").replace("|", "_")
        
        # URL encode to handle special characters
        filename = quote(filename, safe="")
        
        # Add timestamp if requested
        if include_timestamp:
            timestamp = int(time.time())
            filename = f"{filename}-{timestamp}"
        
        # Add .html extension if not present
        if not filename.endswith(".html"):
            filename += ".html"
        
        return filename
    
    def _get_cache_key_to_filename(self, key: str, timestamp: Optional[int] = None) -> str:
        """Generate cache filename from URL key.
        
        Parameters
        ----------
        key : str
            URL to convert to filename.
        timestamp : Optional[int]
            Unix timestamp. If None, uses current time.
            
        Returns
        -------
        str
            Cache filename.
        """
        if timestamp is None:
            timestamp = int(time.time())
        # Use _url_to_filename with timestamp included
        base_filename = self._url_to_filename(key, include_timestamp=False)
        base_name_without_ext = base_filename.replace(".html", "")
        return f"{base_name_without_ext}-{timestamp}.html"
    
    def _extract_timestamp_from_filename(self, filename: str) -> Optional[int]:
        """Extract timestamp from filename.
        
        Parameters
        ----------
        filename : str
            Filename with format: {base}-{timestamp}.html
            
        Returns
        -------
        Optional[int]
            Timestamp if found, None otherwise.
        """
        # Match pattern: {base}-{timestamp}.html
        match = re.search(r'-(\d+)\.html$', filename)
        if match:
            try:
                return int(match.group(1))
            except ValueError:
                return None
        return None
    
    def _get_base_filename(self, url: str) -> str:
        """Get base filename without timestamp.
        
        Parameters
        ----------
        url : str
            URL to get base filename for.
            
        Returns
        -------
        str
            Base filename without timestamp.
        """
        return self._url_to_filename(url, include_timestamp=False)
    
    def _find_cached_files(self, key: str) -> list[str]:
        """Find all cached files matching the base filename for a URL.
        
        Parameters
        ----------
        key : str
            URL to find cached files for.
            
        Returns
        -------
        list[str]
            List of full filepaths to cached files.
        """
        base_filename = self._get_base_filename(key)
        base_name_without_ext = base_filename.replace(".html", "")
        
        cached_files = []
        if os.path.exists(self.store_folder):
            for filename in os.listdir(self.store_folder):
                # Check if filename starts with base name and has timestamp pattern
                if filename.startswith(base_name_without_ext + "-") and filename.endswith(".html"):
                    filepath = os.path.join(self.store_folder, filename)
                    cached_files.append(filepath)
        
        return cached_files
    
    def _read_file(self, filepath: str) -> str:
        """Read HTML file content.
        
        Parameters
        ----------
        filepath : str
            Path to the cached file.
            
        Returns
        -------
        str
            HTML content.
        """
        with open(filepath, 'r', encoding='utf-8') as f:
            return f.read()
    
    def _write_file(self, filepath: str, data: str):
        """Write HTML content to file.
        
        Parameters
        ----------
        filepath : str
            Path where to save the file.
        data : str
            HTML content to write.
        """
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(data)
    
    def _get_cached_filepath(self, url: str, include_timestamp: bool = True) -> str:
        """Get the filepath for a cached URL.
        
        Parameters
        ----------
        url : str
            URL to get cache filepath for.
        include_timestamp : bool
            Whether to include timestamp in filename. Default is True.
            
        Returns
        -------
        str
            Full filepath to the cached file.
        """
        filename = self._url_to_filename(url, include_timestamp=include_timestamp)
        return os.path.join(self.store_folder, filename)
    
    def load(self, url: str) -> Optional[str]:
        """Load HTML from cache if available and not expired.
        
        Parameters
        ----------
        url : str
            URL to load from cache.
            
        Returns
        -------
        Optional[str]
            Cached HTML content if found and valid, None otherwise.
        """
        result = super().load(url)
        return result if isinstance(result, str) else None
    
    def save(self, url: str, html: str):
        """Save HTML content to file if storing is enabled.
        
        Removes old cached files for the same URL before saving new one.
        
        Parameters
        ----------
        url : str
            URL that was fetched.
        html : str
            HTML content to save.
        """
        super().save(url, html)
    
    def clear(self, url: str) -> int:
        """Clear all cached files for a specific URL.
        
        Parameters
        ----------
        url : str
            URL to clear cache for.
            
        Returns
        -------
        int
            Number of files removed.
        """
        return super().clear(url)