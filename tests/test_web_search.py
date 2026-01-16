"""Tests for WebSearch service."""
import sys
from pathlib import Path
from unittest.mock import Mock, patch

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.web_search import WebSearch
from models.web import TextSearchResult, NewsSearchResult


class TestWebSearchSearch:
    """Test WebSearch.search() method."""
    
    @patch('services.web_search.DDGS')
    def test_search_success(self, mock_ddgs_class, web_search_results_sample):
        """Test successful search."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = web_search_results_sample
        
        results = search.search("test query")
        
        assert len(results) == 2
        assert isinstance(results[0], TextSearchResult)
        assert results[0].title == "Arsenal vs Chelsea Preview"
        assert results[0].href == "https://www.bbc.com/sport/football/12345"
        assert results[0].body == "Arsenal host Chelsea in a crucial Premier League match..."
        assert results[0].date == "2025-01-15"
        
        assert results[1].title == "Premier League Match Report"
        assert results[1].date == "2025-01-16"
        
        mock_ddgs.text.assert_called_once_with(
            query="test query",
            region="us-en",
            safesearch="moderate",
            timelimit=None,
            max_results=10,
            backend="auto"
        )

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
    def test_news_search_success(self, mock_ddgs_class, web_search_news_results_sample):
        """Test successful news search."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.news.return_value = web_search_news_results_sample
        
        results = search.news_search("news query")
        
        assert len(results) == 1
        assert isinstance(results[0], NewsSearchResult)
        assert results[0].title == "Arsenal Transfer News"
        assert results[0].url == "https://www.theguardian.com/football/12345"
        assert results[0].body == "Arsenal are reportedly interested in signing..."
        assert results[0].date == "2025-01-15"
        assert results[0].image == "https://example.com/image.jpg"
        assert results[0].source == "The Guardian"
        
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

class TestWebSearchSiteSearch:
    """Test WebSearch.site_search() method."""
    
    @patch('services.web_search.DDGS')
    def test_site_search_success(self, mock_ddgs_class, web_search_results_sample):
        """Test successful site search."""
        search = WebSearch(max_results=10)
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        # Return only first result for single site search
        mock_ddgs.text.return_value = [web_search_results_sample[0]]
        
        results = search.site_search("query", sites=["bbc.com"])
        
        assert len(results) == 1
        assert isinstance(results[0], TextSearchResult)
        assert results[0].title == "Arsenal vs Chelsea Preview"
        
        # Verify site: prefix was added
        mock_ddgs.text.assert_called()
        assert "site:bbc.com" in mock_ddgs.text.call_args[1]['query']
    
    @patch('services.web_search.DDGS')
    def test_site_search_multiple_sites(self, mock_ddgs_class, web_search_results_sample):
        """Test site search across multiple sites."""
        search = WebSearch(max_results=10)
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        # Return first result for each site
        mock_ddgs.text.return_value = [web_search_results_sample[0]]
        
        results = search.site_search("query", sites=["bbc.com", "espn.com"])
        
        # Should have results from both sites
        assert len(results) == 2
        assert all(isinstance(r, TextSearchResult) for r in results)
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
    def test_site_search_handles_site_error(self, mock_ddgs_class, web_search_results_sample):
        """Test that errors for one site don't stop other sites."""
        search = WebSearch(max_results=10)
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        
        # First site fails, second succeeds
        mock_ddgs.text.side_effect = [
            Exception("Site 1 failed"),
            [web_search_results_sample[0]]
        ]
        
        results = search.site_search("query", sites=["bbc.com", "espn.com"])
        
        # Should have results from second site
        assert len(results) == 1
        assert results[0].title == "Arsenal vs Chelsea Preview"
        assert mock_ddgs.text.call_count == 2


class TestWebSearchFootballSearch:
    """Test WebSearch.football_search() method."""
    
    @patch('services.web_search.DDGS')
    def test_football_search_uses_football_sites(self, mock_ddgs_class, web_search_results_sample):
        """Test that football_search uses FOOTBALL_SITES."""
        search = WebSearch()
        
        mock_ddgs = Mock()
        mock_ddgs_class.return_value = mock_ddgs
        mock_ddgs.text.return_value = web_search_results_sample
        
        with patch.object(search, 'site_search') as mock_site_search:
            search.football_search("Arsenal")
            
            mock_site_search.assert_called_once()
            # Verify it was called with FOOTBALL_SITES
            call_args = mock_site_search.call_args
            assert call_args[1]['sites'] == WebSearch.FOOTBALL_SITES
            assert call_args[0][0] == "Arsenal"
