"""Tests for WebSearch service."""
import sys
import pytest
from pathlib import Path
from unittest.mock import Mock, patch, MagicMock

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.web_search import WebSearch
from models.web import TextSearchResult, NewsSearchResult


class TestWebSearchInitialization:
    """Test WebSearch initialization."""
    
    def test_init_default_parameters(self):
        """Test initialization with default parameters."""
        search = WebSearch()
        
        assert search.max_results == 10
        assert search.region == "us-en"
        assert search.safesearch == "moderate"
    
    def test_init_custom_parameters(self):
        """Test initialization with custom parameters."""
        search = WebSearch(
            max_results=20,
            region="uk-en",
            safesearch="off"
        )
        
        assert search.max_results == 20
        assert search.region == "uk-en"
        assert search.safesearch == "off"
    
    def test_football_sites_constant(self):
        """Test that FOOTBALL_SITES constant is defined."""
        assert hasattr(WebSearch, 'FOOTBALL_SITES')
        assert isinstance(WebSearch.FOOTBALL_SITES, list)
        assert len(WebSearch.FOOTBALL_SITES) > 0
        assert "bbc.com" in WebSearch.FOOTBALL_SITES
        assert "skysports.com" in WebSearch.FOOTBALL_SITES


class TestWebSearchSearch:
    """Test WebSearch.search() method."""
    
    @patch('services.web_search.DDGS')
    def test_search_success(self, mock_ddgs_class):
        """Test successful search."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        
        mock_results = [
            {
                "title": "Test Article",
                "href": "https://example.com/article",
                "body": "Article body text",
                "date": "2025-01-15"
            },
            {
                "title": "Another Article",
                "href": "https://example.com/another",
                "body": "Another body",
                "date": None
            }
        ]
        mock_ddgs.text.return_value = mock_results
        
        results = search.search("test query")
        
        assert len(results) == 2
        assert isinstance(results[0], TextSearchResult)
        assert results[0].title == "Test Article"
        assert results[0].href == "https://example.com/article"
        assert results[0].body == "Article body text"
        assert results[0].date == "2025-01-15"
        
        assert results[1].title == "Another Article"
        assert results[1].date is None
        
        mock_ddgs.text.assert_called_once_with(
            query="test query",
            region="us-en",
            safesearch="moderate",
            timelimit=None,
            max_results=10,
            backend="auto"
        )
    
    @patch('services.web_search.DDGS')
    def test_search_with_custom_max_results(self, mock_ddgs_class):
        """Test search with custom max_results."""
        search = WebSearch(max_results=10)
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = []
        
        search.search("query", max_results=5)
        
        mock_ddgs.text.assert_called_once()
        assert mock_ddgs.text.call_args[1]['max_results'] == 5
    
    @patch('services.web_search.DDGS')
    def test_search_with_timelimit(self, mock_ddgs_class):
        """Test search with timelimit."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = []
        
        search.search("query", timelimit="d")
        
        mock_ddgs.text.assert_called_once()
        assert mock_ddgs.text.call_args[1]['timelimit'] == "d"
    
    @patch('services.web_search.DDGS')
    def test_search_with_backend(self, mock_ddgs_class):
        """Test search with custom backend."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = []
        
        search.search("query", backend="google")
        
        mock_ddgs.text.assert_called_once()
        assert mock_ddgs.text.call_args[1]['backend'] == "google"
    
    @patch('services.web_search.DDGS')
    def test_search_handles_exception(self, mock_ddgs_class):
        """Test that exceptions are handled gracefully."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.side_effect = Exception("Search failed")
        
        results = search.search("query")
        
        # Should return empty list on error
        assert results == []
    
    @patch('services.web_search.DDGS')
    def test_search_empty_results(self, mock_ddgs_class):
        """Test search with no results."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = []
        
        results = search.search("nonexistent query")
        
        assert results == []


class TestWebSearchNewsSearch:
    """Test WebSearch.news_search() method."""
    
    @patch('services.web_search.DDGS')
    def test_news_search_success(self, mock_ddgs_class):
        """Test successful news search."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        
        mock_results = [
            {
                "title": "News Article",
                "url": "https://example.com/news",
                "body": "News body",
                "date": "2025-01-15",
                "image": "https://example.com/image.jpg",
                "source": "Example News"
            }
        ]
        mock_ddgs.news.return_value = mock_results
        
        results = search.news_search("news query")
        
        assert len(results) == 1
        assert isinstance(results[0], NewsSearchResult)
        assert results[0].title == "News Article"
        assert results[0].url == "https://example.com/news"
        assert results[0].body == "News body"
        assert results[0].date == "2025-01-15"
        assert results[0].image == "https://example.com/image.jpg"
        assert results[0].source == "Example News"
        
        mock_ddgs.news.assert_called_once_with(
            query="news query",
            region="us-en",
            safesearch="moderate",
            timelimit=None,
            max_results=10
        )
    
    @patch('services.web_search.DDGS')
    def test_news_search_handles_exception(self, mock_ddgs_class):
        """Test that exceptions are handled gracefully."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.news.side_effect = Exception("News search failed")
        
        results = search.news_search("query")
        
        assert results == []
    
    @patch('services.web_search.DDGS')
    def test_news_search_with_custom_max_results(self, mock_ddgs_class):
        """Test news search with custom max_results."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.news.return_value = []
        
        search.news_search("query", max_results=20)
        
        assert mock_ddgs.news.call_args[1]['max_results'] == 20


class TestWebSearchSiteSearch:
    """Test WebSearch.site_search() method."""
    
    @patch('services.web_search.DDGS')
    def test_site_search_success(self, mock_ddgs_class):
        """Test successful site search."""
        search = WebSearch(max_results=10)
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        
        mock_results = [
            {
                "title": "Site Article",
                "href": "https://bbc.com/article",
                "body": "Article body",
                "date": "2025-01-15"
            }
        ]
        mock_ddgs.text.return_value = mock_results
        
        results = search.site_search("query", sites=["bbc.com"])
        
        assert len(results) == 1
        assert isinstance(results[0], TextSearchResult)
        assert results[0].title == "Site Article"
        
        # Verify site: prefix was added
        mock_ddgs.text.assert_called()
        assert "site:bbc.com" in mock_ddgs.text.call_args[1]['query']
    
    @patch('services.web_search.DDGS')
    def test_site_search_multiple_sites(self, mock_ddgs_class):
        """Test site search across multiple sites."""
        search = WebSearch(max_results=10)
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = [
            {"title": "Result 1", "href": "url1", "body": "body1", "date": None}
        ]
        
        results = search.site_search("query", sites=["bbc.com", "espn.com"])
        
        # Should search each site
        assert mock_ddgs.text.call_count == 2
        # Results per site should be distributed
        assert mock_ddgs.text.call_args[1]['max_results'] == 5  # 10 / 2 sites
    
    @patch('services.web_search.DDGS')
    def test_site_search_empty_sites_list(self, mock_ddgs_class):
        """Test site search with empty sites list."""
        search = WebSearch()
        
        results = search.site_search("query", sites=[])
        
        assert results == []
        mock_ddgs_class.assert_not_called()
    
    @patch('services.web_search.DDGS')
    def test_site_search_handles_site_error(self, mock_ddgs_class):
        """Test that errors for one site don't stop other sites."""
        search = WebSearch(max_results=10)
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        
        # First site fails, second succeeds
        mock_ddgs.text.side_effect = [
            Exception("Site 1 failed"),
            [{"title": "Result", "href": "url", "body": "body", "date": None}]
        ]
        
        results = search.site_search("query", sites=["bbc.com", "espn.com"])
        
        # Should have results from second site
        assert len(results) == 1
        assert mock_ddgs.text.call_count == 2


class TestWebSearchFootballSearch:
    """Test WebSearch.football_search() method."""
    
    @patch('services.web_search.DDGS')
    def test_football_search_uses_football_sites(self, mock_ddgs_class):
        """Test that football_search uses FOOTBALL_SITES."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = []
        
        with patch.object(search, 'site_search') as mock_site_search:
            search.football_search("Arsenal")
            
            mock_site_search.assert_called_once()
            # Verify it was called with FOOTBALL_SITES
            call_args = mock_site_search.call_args
            assert call_args[1]['sites'] == WebSearch.FOOTBALL_SITES
            assert call_args[0][0] == "Arsenal"
    
    @patch('services.web_search.DDGS')
    def test_football_search_passes_parameters(self, mock_ddgs_class):
        """Test that football_search passes parameters correctly."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = []
        
        with patch.object(search, 'site_search') as mock_site_search:
            search.football_search(
                "query",
                max_results=20,
                timelimit="w",
                backend="google"
            )
            
            mock_site_search.assert_called_once()
            call_args = mock_site_search.call_args
            assert call_args[1]['max_results'] == 20
            assert call_args[1]['timelimit'] == "w"
            assert call_args[1]['backend'] == "google"
