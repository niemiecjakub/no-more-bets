import os
import re
import time
from urllib.parse import urlparse, quote


class HtmlCache:
    """HTML cache manager with TTL support.
    
    Handles saving and loading HTML content from disk cache with
    time-to-live (TTL) expiration.
    """
    
    def __init__(
        self,
        store_folder: str = "html_cache",
        store: bool = True,
        use_cache: bool = True,
        cache_ttl: float = 3600.0,
    ):
        """Initialize HTML cache manager.
        
        Parameters
        ----------
        store_folder : str
            Folder where cached HTML files are stored. Default is "html_cache".
        store : bool
            Whether to save fetched HTML to cache folder. Default is True.
        use_cache : bool
            Whether to use cached HTML if available. Default is True.
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
    
    def _extract_timestamp_from_filename(self, filename: str) -> int | None:
        """Extract timestamp from filename.
        
        Parameters
        ----------
        filename : str
            Filename with format: {base}-{timestamp}.html
            
        Returns
        -------
        int | None
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
    
    def _find_cached_files(self, url: str) -> list[str]:
        """Find all cached files matching the base filename for a URL.
        
        Parameters
        ----------
        url : str
            URL to find cached files for.
            
        Returns
        -------
        list[str]
            List of full filepaths to cached files.
        """
        base_filename = self._get_base_filename(url)
        base_name_without_ext = base_filename.replace(".html", "")
        
        cached_files = []
        if os.path.exists(self.store_folder):
            for filename in os.listdir(self.store_folder):
                # Check if filename starts with base name and has timestamp pattern
                if filename.startswith(base_name_without_ext + "-") and filename.endswith(".html"):
                    filepath = os.path.join(self.store_folder, filename)
                    cached_files.append(filepath)
        
        return cached_files
    
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
    
    def load(self, url: str) -> str | None:
        """Load HTML from cache if available and not expired.
        
        Parameters
        ----------
        url : str
            URL to load from cache.
            
        Returns
        -------
        str | None
            Cached HTML content if found and valid, None otherwise.
        """
        if not self.use_cache:
            return None
        
        # Find all cached files for this URL
        cached_files = self._find_cached_files(url)
        
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
                with open(valid_filepath, 'r', encoding='utf-8') as f:
                    return f.read()
            except Exception as e:
                # If reading cache fails, continue with network request
                print(f"Warning: Failed to read cache from {valid_filepath}: {e}")
                return None
        
        return None
    
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
        if not self.store:
            return
        
        # Remove old cached files for this URL
        old_files = self._find_cached_files(url)
        for old_file in old_files:
            try:
                os.remove(old_file)
            except Exception as e:
                print(f"Warning: Failed to remove old cache file {old_file}: {e}")
        
        # Save new file with timestamp
        filepath = self._get_cached_filepath(url, include_timestamp=True)
        
        try:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(html)
        except Exception as e:
            # Don't fail the request if saving fails
            print(f"Warning: Failed to save HTML to {filepath}: {e}")
    
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
        cached_files = self._find_cached_files(url)
        removed_count = 0
        
        for filepath in cached_files:
            try:
                if os.path.exists(filepath):
                    os.remove(filepath)
                    removed_count += 1
            except Exception as e:
                print(f"Warning: Failed to remove cache file {filepath}: {e}")
        
        return removed_count