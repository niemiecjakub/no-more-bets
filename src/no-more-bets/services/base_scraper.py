from curl_cffi import requests
from curl_cffi.requests.exceptions import ConnectionError, Timeout
import time
import random
import os
from urllib.parse import urlparse, quote


class BaseScraper:
    def __init__(
        self,
        impersonate: str = "chrome110",
        delay: float = 5.0,
        retry_count: int = 3,
        retry_delay: float = 2.0,
        timeout: float = 15.0,
        store: bool = True,
        use_cache: bool = True,
    ):
        self.impersonate = impersonate
        self.base_url = ""
        self.delay = delay
        self.retry_count = retry_count
        self.retry_delay = retry_delay
        self.timeout = timeout
        self.store = store  
        self.use_cache = use_cache
        self.store_folder = "html_cache"
        self.last_fetch_time = None
        self.session = requests.Session()
        
        # Create store folder if storing or caching is enabled
        if self.store or self.use_cache:
            os.makedirs(self.store_folder, exist_ok=True)

    def _rate_limit(self):
        if self.last_fetch_time is None:
            return

        elapsed = time.time() - self.last_fetch_time
        if elapsed < self.delay:
            time.sleep(self.delay - elapsed)

    def _url_to_filename(self, url: str) -> str:
        """Convert URL to a safe filename.
        
        Parameters
        ----------
        url : str
            URL to convert.
            
        Returns
        -------
        str
            Safe filename derived from URL.
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
        
        # Add .html extension if not present
        if not filename.endswith(".html"):
            filename += ".html"
        
        return filename

    def _get_cached_filepath(self, url: str) -> str:
        """Get the filepath for a cached URL.
        
        Parameters
        ----------
        url : str
            URL to get cache filepath for.
            
        Returns
        -------
        str
            Full filepath to the cached file.
        """
        filename = self._url_to_filename(url)
        return os.path.join(self.store_folder, filename)

    def _load_cached_html(self, url: str) -> str | None:
        """Load HTML from cache if available.
        
        Parameters
        ----------
        url : str
            URL to load from cache.
            
        Returns
        -------
        str | None
            Cached HTML content if found, None otherwise.
        """
        if not self.use_cache:
            return None
        
        filepath = self._get_cached_filepath(url)
        
        if os.path.exists(filepath):
            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    return f.read()
            except Exception as e:
                # If reading cache fails, continue with network request
                print(f"Warning: Failed to read cache from {filepath}: {e}")
                return None
        
        return None

    def _save_html(self, url: str, html: str):
        """Save HTML content to file if storing is enabled.
        
        Parameters
        ----------
        url : str
            URL that was fetched.
        html : str
            HTML content to save.
        """
        if not self.store:
            return
        
        filepath = self._get_cached_filepath(url)
        
        try:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(html)
        except Exception as e:
            # Don't fail the request if saving fails
            print(f"Warning: Failed to save HTML to {filepath}: {e}")

    def _get_page_html(self, url: str) -> str:
        # Check cache first if enabled
        if self.use_cache:
            cached_html = self._load_cached_html(url)
            if cached_html is not None:
                return cached_html
        
        # If not in cache, fetch from network
        response = self._get_page_response(url)
        html = response.text
        
        # Save HTML if storing is enabled
        if self.store:
            self._save_html(url, html)
        
        return html

    def _get_page_response(self, url: str) -> requests.Response:
        last_exception = None

        for attempt in range(1, self.retry_count + 1):
            self._rate_limit()

            try:
                response = self.session.get(
                    url,
                    impersonate=self.impersonate,
                    timeout=self.timeout,
                )

                self.last_fetch_time = time.time()

                if response.ok:
                    return response

                if response.status_code in {403, 404, 410}:
                    raise Exception(
                        f"Permanent failure ({response.status_code}) for {url}"
                    )

                last_exception = Exception(f"HTTP {response.status_code} for {url}")

            except (ConnectionError, Timeout) as e:
                last_exception = e

            # Retry with exponential backoff 
            if attempt < self.retry_count:
                backoff = self.retry_delay * attempt
                jitter = random.uniform(0.5, 1.5)
                time.sleep(backoff * jitter)

        raise Exception(
            f"Failed to fetch {url} after {self.retry_count} attempts"
        ) from last_exception
