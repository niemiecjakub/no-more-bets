"""Tests for BaseScraper class."""
import sys
import time
import pytest
from pathlib import Path
from unittest.mock import Mock, MagicMock, patch, call
from curl_cffi.requests.exceptions import ConnectionError, Timeout

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.base_scraper import BaseScraper
from utils.html_cache import HtmlCache


class TestBaseScraperInitialization:
    """Test BaseScraper initialization."""
    
    def test_init_default_parameters(self, temp_cache_dir):
        """Test initialization with default parameters."""
        scraper = BaseScraper()
        assert scraper.impersonate == "chrome110"
        assert scraper.base_url == ""
        assert scraper.delay == 5.0
        assert scraper.retry_count == 3
        assert scraper.retry_delay == 2.0
        assert scraper.timeout == 15.0
        assert scraper.last_fetch_time is None
        assert scraper.session is not None
        assert isinstance(scraper.cache, HtmlCache)
        assert scraper.cache.store is True
        assert scraper.cache.use_cache is True
        assert scraper.cache.cache_ttl == 3600.0
    
    def test_init_custom_parameters(self, temp_cache_dir):
        """Test initialization with custom parameters."""
        scraper = BaseScraper(
            impersonate="chrome120",
            delay=3.0,
            retry_count=5,
            retry_delay=1.0,
            timeout=10.0,
            store=False,
            use_cache=False,
            cache_ttl=7200.0
        )
        assert scraper.impersonate == "chrome120"
        assert scraper.delay == 3.0
        assert scraper.retry_count == 5
        assert scraper.retry_delay == 1.0
        assert scraper.timeout == 10.0
        assert scraper.cache.store is False
        assert scraper.cache.use_cache is False
        assert scraper.cache.cache_ttl == 7200.0


class TestBaseScraperRateLimit:
    """Test BaseScraper rate limiting."""
    
    def test_rate_limit_no_previous_fetch(self, temp_cache_dir, monkeypatch):
        """Test rate limit when no previous fetch has occurred."""
        scraper = BaseScraper(delay=5.0)
        scraper.last_fetch_time = None
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        
        scraper._rate_limit()
        
        # Should not sleep if last_fetch_time is None
        mock_sleep.assert_not_called()
    
    def test_rate_limit_elapsed_greater_than_delay(self, temp_cache_dir, monkeypatch):
        """Test rate limit when enough time has elapsed."""
        scraper = BaseScraper(delay=5.0)
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        
        # Set last fetch to 10 seconds ago
        scraper.last_fetch_time = 990.0
        
        scraper._rate_limit()
        
        # Should not sleep if enough time has passed
        mock_sleep.assert_not_called()
    
    def test_rate_limit_elapsed_less_than_delay(self, temp_cache_dir, monkeypatch):
        """Test rate limit when not enough time has elapsed."""
        scraper = BaseScraper(delay=5.0)
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        
        # Set last fetch to 2 seconds ago (need to wait 3 more seconds)
        scraper.last_fetch_time = 998.0
        
        scraper._rate_limit()
        
        # Should sleep for the remaining delay
        mock_sleep.assert_called_once_with(3.0)


class TestBaseScraperGetPageResponse:
    """Test BaseScraper._get_page_response() method."""
    
    def test_get_page_response_success(self, temp_cache_dir, monkeypatch):
        """Test successful page fetch."""
        scraper = BaseScraper()
        
        mock_response = Mock()
        mock_response.ok = True
        mock_response.status_code = 200
        mock_response.text = "<html>Test</html>"
        
        mock_session = Mock()
        mock_session.get.return_value = mock_response
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        result = scraper._get_page_response("https://example.com")
        
        assert result == mock_response
        assert scraper.last_fetch_time == 1000.0
        mock_session.get.assert_called_once_with(
            "https://example.com",
            impersonate="chrome110",
            timeout=15.0
        )
    
    def test_get_page_response_retry_on_connection_error(self, temp_cache_dir, monkeypatch):
        """Test retry on ConnectionError."""
        scraper = BaseScraper(retry_count=3, retry_delay=0.1)
        
        mock_response = Mock()
        mock_response.ok = True
        
        mock_session = Mock()
        # First two calls fail, third succeeds
        mock_session.get.side_effect = [
            ConnectionError("Connection failed"),
            ConnectionError("Connection failed"),
            mock_response
        ]
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        monkeypatch.setattr("random.uniform", lambda a, b: 1.0)  # No jitter for deterministic test
        
        result = scraper._get_page_response("https://example.com")
        
        assert result == mock_response
        assert mock_session.get.call_count == 3
        # Should have slept between retries
        assert mock_sleep.call_count == 2
    
    def test_get_page_response_retry_on_timeout(self, temp_cache_dir, monkeypatch):
        """Test retry on Timeout."""
        scraper = BaseScraper(retry_count=2, retry_delay=0.1)
        
        mock_response = Mock()
        mock_response.ok = True
        
        mock_session = Mock()
        mock_session.get.side_effect = [
            Timeout("Request timeout"),
            mock_response
        ]
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        monkeypatch.setattr("random.uniform", lambda a, b: 1.0)
        
        result = scraper._get_page_response("https://example.com")
        
        assert result == mock_response
        assert mock_session.get.call_count == 2
    
    def test_get_page_response_no_retry_on_403(self, temp_cache_dir, monkeypatch):
        """Test that 403 errors are not retried."""
        scraper = BaseScraper(retry_count=3)
        
        mock_response = Mock()
        mock_response.ok = False
        mock_response.status_code = 403
        
        mock_session = Mock()
        mock_session.get.return_value = mock_response
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        with pytest.raises(Exception, match="Permanent failure \\(403\\)"):
            scraper._get_page_response("https://example.com")
        
        # Should only try once for permanent errors
        assert mock_session.get.call_count == 1
    
    def test_get_page_response_no_retry_on_404(self, temp_cache_dir, monkeypatch):
        """Test that 404 errors are not retried."""
        scraper = BaseScraper(retry_count=3)
        
        mock_response = Mock()
        mock_response.ok = False
        mock_response.status_code = 404
        
        mock_session = Mock()
        mock_session.get.return_value = mock_response
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        with pytest.raises(Exception, match="Permanent failure \\(404\\)"):
            scraper._get_page_response("https://example.com")
        
        assert mock_session.get.call_count == 1
    
    def test_get_page_response_no_retry_on_410(self, temp_cache_dir, monkeypatch):
        """Test that 410 errors are not retried."""
        scraper = BaseScraper(retry_count=3)
        
        mock_response = Mock()
        mock_response.ok = False
        mock_response.status_code = 410
        
        mock_session = Mock()
        mock_session.get.return_value = mock_response
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        with pytest.raises(Exception, match="Permanent failure \\(410\\)"):
            scraper._get_page_response("https://example.com")
        
        assert mock_session.get.call_count == 1
    
    def test_get_page_response_retry_on_500(self, temp_cache_dir, monkeypatch):
        """Test retry on 500 errors."""
        scraper = BaseScraper(retry_count=2, retry_delay=0.1)
        
        mock_response_500 = Mock()
        mock_response_500.ok = False
        mock_response_500.status_code = 500
        
        mock_response_200 = Mock()
        mock_response_200.ok = True
        
        mock_session = Mock()
        mock_session.get.side_effect = [mock_response_500, mock_response_200]
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        monkeypatch.setattr("random.uniform", lambda a, b: 1.0)
        
        result = scraper._get_page_response("https://example.com")
        
        assert result == mock_response_200
        assert mock_session.get.call_count == 2
    
    def test_get_page_response_fails_after_all_retries(self, temp_cache_dir, monkeypatch):
        """Test that exception is raised after all retries are exhausted."""
        scraper = BaseScraper(retry_count=2, retry_delay=0.1)
        
        mock_session = Mock()
        mock_session.get.side_effect = ConnectionError("Connection failed")
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        monkeypatch.setattr("random.uniform", lambda a, b: 1.0)
        
        with pytest.raises(Exception, match="Failed to fetch.*after 2 attempts"):
            scraper._get_page_response("https://example.com")
        
        assert mock_session.get.call_count == 2


class TestBaseScraperGetPageHtml:
    """Test BaseScraper._get_page_html() method."""
    
    def test_get_page_html_from_cache(self, temp_cache_dir, monkeypatch):
        """Test loading HTML from cache."""
        scraper = BaseScraper(use_cache=True, store=True)
        
        cached_html = "<html>Cached content</html>"
        scraper.cache.save("https://example.com", cached_html)
        
        result = scraper._get_page_html("https://example.com")
        
        assert result == cached_html
     
    def test_get_page_html_no_cache_no_store(self, temp_cache_dir, monkeypatch):
        """Test fetching HTML without caching."""
        scraper = BaseScraper(use_cache=False, store=False)
        
        html_content = "<html>Fresh content</html>"
        mock_response = Mock()
        mock_response.ok = True
        mock_response.text = html_content
        
        mock_session = Mock()
        mock_session.get.return_value = mock_response
        scraper.session = mock_session
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        result = scraper._get_page_html("https://example.com")
        
        assert result == html_content
        # Verify it was not saved to cache
        cached = scraper.cache.load("https://example.com")
        assert cached is None


class TestBaseScraperSelenium:
    """Test BaseScraper._get_page_html_selenium() method."""
    
    def test_get_page_html_selenium_from_cache(self, temp_cache_dir):
        """Test loading HTML from cache for selenium."""
        scraper = BaseScraper(use_cache=True)
        
        cached_html = "<html>Cached selenium content</html>"
        scraper.cache.save("https://example.com", cached_html)
        
        result = scraper._get_page_html_selenium("https://example.com")
        
        assert result == cached_html
    
    def test_get_page_html_selenium_import_error(self, temp_cache_dir, monkeypatch):
        """Test ImportError when selenium is not installed."""
        scraper = BaseScraper(use_cache=False)
        
        # Mock ImportError when trying to import selenium
        def mock_import_error(name, *args, **kwargs):
            if name == 'selenium':
                raise ImportError("No module named 'selenium'")
            return __import__(name, *args, **kwargs)
        
        monkeypatch.setattr("builtins.__import__", mock_import_error)
        
        with pytest.raises(ImportError, match="selenium is required"):
            scraper._get_page_html_selenium("https://example.com")
    
    def test_get_page_html_selenium_retry_on_timeout(self, temp_cache_dir, monkeypatch):
        """Test retry on Selenium TimeoutException."""
        scraper = BaseScraper(retry_count=2, retry_delay=0.1, use_cache=False)
        
        from selenium.common.exceptions import TimeoutException
        
        html_content = "<html>Success</html>"
        mock_driver = Mock()
        mock_driver.page_source = html_content
        mock_driver.get = Mock()
        mock_driver.quit = Mock()
        
        mock_time = Mock()
        mock_time.return_value = 1000.0
        monkeypatch.setattr("time.time", mock_time)
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        monkeypatch.setattr("random.uniform", lambda a, b: 1.0)
        
        # Mock selenium by patching sys.modules before the import happens
        import sys
        mock_selenium = MagicMock()
        # On first attempt, Chrome raises exception (before WebDriverWait is called)
        # On second attempt, Chrome returns driver, WebDriverWait should succeed
        wait_mock = Mock()
        wait_mock.until.return_value = True  # Second attempt succeeds
        
        mock_selenium.webdriver.Chrome.side_effect = [
            TimeoutException("Page load timeout"),  # First attempt fails
            mock_driver  # Second attempt succeeds
        ]
        mock_selenium.webdriver.chrome.options.Options.return_value = Mock()
        mock_selenium.webdriver.common.by.By = Mock()
        mock_selenium.webdriver.support.ui.WebDriverWait.return_value = wait_mock
        mock_selenium.webdriver.support.expected_conditions = Mock()
        mock_selenium.common.exceptions.TimeoutException = TimeoutException
        mock_selenium.common.exceptions.WebDriverException = Exception
        
        with patch.dict(sys.modules, {
            'selenium': mock_selenium,
            'selenium.webdriver': mock_selenium.webdriver,
            'selenium.webdriver.chrome': mock_selenium.webdriver.chrome,
            'selenium.webdriver.chrome.options': mock_selenium.webdriver.chrome.options,
            'selenium.webdriver.common': mock_selenium.webdriver.common,
            'selenium.webdriver.common.by': mock_selenium.webdriver.common.by,
            'selenium.webdriver.support': mock_selenium.webdriver.support,
            'selenium.webdriver.support.ui': mock_selenium.webdriver.support.ui,
            'selenium.webdriver.support.expected_conditions': mock_selenium.webdriver.support.expected_conditions,
            'selenium.common': mock_selenium.common,
            'selenium.common.exceptions': mock_selenium.common.exceptions,
        }):
            result = scraper._get_page_html_selenium("https://example.com")
            
            assert result == html_content
            assert mock_selenium.webdriver.Chrome.call_count == 2


class TestBaseScraperClearCache:
    """Test BaseScraper.clear_cache() method."""
    
    def test_clear_cache(self, temp_cache_dir):
        """Test clearing cache for a URL."""
        scraper = BaseScraper()
        
        url = "https://example.com"
        scraper.cache.save(url, "<html>Content</html>")
        
        # Verify cache exists
        assert scraper.cache.load(url) is not None
        
        # Clear cache
        result = scraper.clear_cache(url)
        
        assert result == 1
        # Verify cache is cleared
        assert scraper.cache.load(url) is None
    
    def test_clear_cache_no_files(self, temp_cache_dir):
        """Test clearing cache when no files exist."""
        scraper = BaseScraper()
        
        result = scraper.clear_cache("https://nonexistent.com")
        
        assert result == 0
