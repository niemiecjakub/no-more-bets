from curl_cffi import requests
from curl_cffi.requests.exceptions import ConnectionError, Timeout
import time
import random
from utils.html_cache import HtmlCache


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
        cache_ttl: float = 3600.0,
    ):
        self.impersonate = impersonate
        self.base_url = ""
        self.delay = delay
        self.retry_count = retry_count
        self.retry_delay = retry_delay
        self.timeout = timeout
        self.last_fetch_time = None
        self.session = requests.Session()
        
        # Initialize HTML cache
        self.cache = HtmlCache(
            store_folder="html_cache",
            store=store,
            use_cache=use_cache,
            cache_ttl=cache_ttl,
        )

    def _rate_limit(self):
        if self.last_fetch_time is None:
            return

        elapsed = time.time() - self.last_fetch_time
        if elapsed < self.delay:
            time.sleep(self.delay - elapsed)


    def _get_page_html(self, url: str) -> str:
        # Check cache first if enabled
        if self.cache.use_cache:
            cached_html = self.cache.load(url)
            if cached_html is not None:
                return cached_html
        
        # If not in cache, fetch from network
        response = self._get_page_response(url)
        html = response.text
        
        # Save HTML if storing is enabled
        if self.cache.store:
            self.cache.save(url, html)
        
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
