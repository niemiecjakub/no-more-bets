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

    def _get_page_html_selenium(
        self,
        url: str,
        headless: bool = True,
        wait_time: float = 5.0,
        implicit_wait: float = 10.0,
    ) -> str:
        """Get page HTML using Selenium WebDriver.
        
        Parameters
        ----------
        url : str
            URL to fetch.
        headless : bool
            Whether to run browser in headless mode. Default is True.
        wait_time : float
            Time to wait for page to load in seconds. Default is 5.0.
        implicit_wait : float
            Implicit wait time for WebDriver in seconds. Default is 10.0.
            
        Returns
        -------
        str
            HTML content of the page.
            
        Raises
        ------
        ImportError
            If selenium is not installed.
        Exception
            If page fetch fails after retry attempts.
        """
        # Check cache first if enabled
        if self.cache.use_cache:
            cached_html = self.cache.load(url)
            if cached_html is not None:
                return cached_html
        
        # Import selenium here to avoid requiring it if not used
        try:
            from selenium import webdriver
            from selenium.webdriver.chrome.options import Options
            from selenium.webdriver.common.by import By
            from selenium.webdriver.support.ui import WebDriverWait
            from selenium.webdriver.support import expected_conditions as EC
            from selenium.common.exceptions import TimeoutException, WebDriverException
        except ImportError:
            raise ImportError(
                "selenium is required for _get_page_html_selenium. "
                "Install it with: pip install selenium"
            )
        
        last_exception = None
        driver = None
        
        for attempt in range(1, self.retry_count + 1):
            self._rate_limit()
            
            try:
                # Setup Chrome options
                chrome_options = Options()
                if headless:
                    chrome_options.add_argument('--headless')
                chrome_options.add_argument('--no-sandbox')
                chrome_options.add_argument('--disable-dev-shm-usage')
                chrome_options.add_argument('--disable-blink-features=AutomationControlled')
                chrome_options.add_argument(
                    'user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) '
                    'AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
                )
                
                # Create WebDriver instance
                driver = webdriver.Chrome(options=chrome_options)
                driver.implicitly_wait(implicit_wait)
                driver.set_page_load_timeout(self.timeout)
                
                # Navigate to URL
                driver.get(url)
                
                # Wait for page to load
                WebDriverWait(driver, wait_time).until(
                    EC.presence_of_element_located((By.TAG_NAME, "body"))
                )
                
                # Get page HTML
                html = driver.page_source
                
                self.last_fetch_time = time.time()
                
                # Close driver
                driver.quit()
                driver = None
                
                # Save HTML if storing is enabled
                if self.cache.store:
                    self.cache.save(url, html)
                
                return html
                
            except (TimeoutException, WebDriverException) as e:
                last_exception = e
                if driver:
                    try:
                        driver.quit()
                    except Exception:
                        pass
                    driver = None
                
            # Retry with exponential backoff
            if attempt < self.retry_count:
                backoff = self.retry_delay * attempt
                jitter = random.uniform(0.5, 1.5)
                time.sleep(backoff * jitter)
        
        # Ensure driver is closed if still open
        if driver:
            try:
                driver.quit()
            except Exception:
                pass
        
        raise Exception(
            f"Failed to fetch {url} using Selenium after {self.retry_count} attempts"
        ) from last_exception

    def clear_cache(self, url: str) -> int:
        """Clear cached files for a specific URL.
        
        Parameters
        ----------
        url : str
            URL to clear cache for.
            
        Returns
        -------
        int
            Number of cache files removed.
        """
        return self.cache.clear(url)