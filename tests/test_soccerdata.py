"""Tests for SoccerData API client."""
import sys
import os
import pytest
from pathlib import Path
from unittest.mock import Mock, patch, MagicMock
from requests.exceptions import ConnectionError, Timeout, RequestException

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.soccerdata import SoccerData
from utils.json_cache import JsonCache


class TestSoccerDataEndpointToCacheKey:
    """Test SoccerData._endpoint_to_cache_key() method."""
    
    def test_endpoint_to_cache_key_simple(self, temp_cache_dir, monkeypatch):
        """Test cache key generation for simple endpoint."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData()
        
        key = service._endpoint_to_cache_key('/match-previews-upcoming/')
        assert key == 'match-previews-upcoming_'
    
    def test_endpoint_to_cache_key_with_params(self, temp_cache_dir, monkeypatch):
        """Test cache key generation with parameters."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData()
        
        params = {'match_id': 954577, 'team_1_id': 2916, 'team_2_id': 4148}
        key = service._endpoint_to_cache_key('/match-preview/', params)
        
        # Should be deterministic and sorted
        assert 'match-preview_' in key
        assert 'match_id_954577' in key
        assert 'team_1_id_2916' in key
        assert 'team_2_id_4148' in key
    
    def test_endpoint_to_cache_key_excludes_auth_token(self, temp_cache_dir, monkeypatch):
        """Test that auth_token is excluded from cache key."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData()
        
        params = {'match_id': 954577, 'auth_token': 'secret_key'}
        key = service._endpoint_to_cache_key('/match-preview/', params)
        
        assert 'auth_token' not in key
        assert 'match_id_954577' in key


class TestSoccerDataMakeRequest:
    """Test SoccerData._make_request() method."""
    
    def test_make_request_success(self, temp_cache_dir, monkeypatch):
        """Test successful API request."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        mock_response = Mock()
        mock_response.ok = True
        mock_response.json.return_value = {'results': []}
        
        with patch('services.soccerdata.requests.get', return_value=mock_response):
            result = service._make_request('/match-previews-upcoming/')
        
        assert result == {'results': []}
    
    def test_make_request_with_params(self, temp_cache_dir, monkeypatch):
        """Test API request with parameters."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        mock_response = Mock()
        mock_response.ok = True
        mock_response.json.return_value = {'match_id': 954577}
        
        with patch('services.soccerdata.requests.get', return_value=mock_response) as mock_get:
            params = {'match_id': 954577}
            result = service._make_request('/match-preview/', params)
        
        assert result == {'match_id': 954577}
        # Verify auth_token was added to params
        call_args = mock_get.call_args
        assert 'auth_token' in call_args[1]['params']
        assert call_args[1]['params']['auth_token'] == 'test_key'
        assert call_args[1]['params']['match_id'] == 954577
    
    def test_make_request_from_cache(self, temp_cache_dir, monkeypatch):
        """Test loading response from cache."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=True, store_cache=True)
        
        cached_data = {'results': [{'league_id': 39}]}
        cache_key = service._endpoint_to_cache_key('/match-previews-upcoming/')
        service.cache.save(cache_key, cached_data)
        
        result = service._make_request('/match-previews-upcoming/')
        
        assert result == cached_data
    
    def test_make_request_retry_on_connection_error(self, temp_cache_dir, monkeypatch):
        """Test retry on ConnectionError."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(retry_count=3, retry_delay=0.1, use_cache=False)
        
        mock_response = Mock()
        mock_response.ok = True
        mock_response.json.return_value = {'success': True}
        
        with patch('services.soccerdata.requests.get') as mock_get:
            mock_get.side_effect = [
                ConnectionError("Connection failed"),
                ConnectionError("Connection failed"),
                mock_response
            ]
            
            mock_sleep = Mock()
            monkeypatch.setattr("time.sleep", mock_sleep)
            monkeypatch.setattr("random.uniform", lambda a, b: 1.0)
            
            result = service._make_request('/match-previews-upcoming/')
        
        assert result == {'success': True}
        assert mock_get.call_count == 3
    
    def test_make_request_retry_on_timeout(self, temp_cache_dir, monkeypatch):
        """Test retry on Timeout."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(retry_count=2, retry_delay=0.1, use_cache=False)
        
        mock_response = Mock()
        mock_response.ok = True
        mock_response.json.return_value = {'success': True}
        
        with patch('services.soccerdata.requests.get') as mock_get:
            mock_get.side_effect = [Timeout("Request timeout"), mock_response]
            
            mock_sleep = Mock()
            monkeypatch.setattr("time.sleep", mock_sleep)
            monkeypatch.setattr("random.uniform", lambda a, b: 1.0)
            
            result = service._make_request('/match-previews-upcoming/')
        
        assert result == {'success': True}
        assert mock_get.call_count == 2
    
    
    def test_make_request_saves_to_cache(self, temp_cache_dir, monkeypatch):
        """Test that successful response is saved to cache."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        # Use temp_cache_dir to ensure fresh cache
        service = SoccerData(use_cache=True, store_cache=True)
        service.cache.store_folder = temp_cache_dir
        
        response_data = {'results': [{'league_id': 39}]}
        mock_response = Mock()
        mock_response.ok = True
        mock_response.json.return_value = response_data
        
        # Clear any existing cache first
        cache_key = service._endpoint_to_cache_key('/match-previews-upcoming/')
        service.cache.clear(cache_key)
        
        with patch('services.soccerdata.requests.get', return_value=mock_response):
            service._make_request('/match-previews-upcoming/')
        
        # Verify it was saved to cache
        cached = service.cache.load(cache_key)
        assert cached == response_data


class TestSoccerDataGetMatchPreviewsUpcoming:
    """Test SoccerData.get_match_previews_upcoming() method."""
    
    def test_get_match_previews_upcoming_all(self, temp_cache_dir, monkeypatch, soccerdata_match_previews_upcoming):
        """Test getting all upcoming match previews using real fixture."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        # Fixture is a list, but service expects dict with 'results' key
        response_data = {'results': soccerdata_match_previews_upcoming}
        
        with patch.object(service, '_make_request', return_value=response_data):
            results = service.get_match_previews_upcoming()
        
        # Assert on structure - real fixture has multiple leagues
        assert len(results) > 0
        assert all(hasattr(r, 'league_id') for r in results)
        assert all(hasattr(r, 'league_name') for r in results)
    
 
    
    def test_get_match_previews_upcoming_handles_parse_errors(self, temp_cache_dir, monkeypatch):
        """Test that parse errors are handled gracefully."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        response_data = {
            'results': [
                {
                    'league_id': 39,
                    'league_name': 'Premier League',
                    'match_previews': []  # Valid - has all required fields
                },
                {'invalid': 'data'},  # Invalid - missing required fields
            ]
        }
        
        with patch.object(service, '_make_request', return_value=response_data):
            results = service.get_match_previews_upcoming()
        
        # Should only return valid results
        assert len(results) == 1
        assert results[0].league_id == 39


class TestSoccerDataGetMatchPreview:
    """Test SoccerData.get_match_preview() method."""
    
    def test_get_match_preview_success(self, temp_cache_dir, monkeypatch, soccerdata_match_preview):
        """Test getting match preview using real fixture."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        # Fixture uses 'id' field
        match_id = soccerdata_match_preview.get('id', 955509)
        
        with patch.object(service, '_make_request', return_value=soccerdata_match_preview):
            result = service.get_match_preview(match_id)
        
        # Assert on structure
        assert result.id is not None
        assert result.teams is not None
        assert hasattr(result, 'match_data')


class TestSoccerDataGetHeadToHead:
    """Test SoccerData.get_head_to_head() method."""
    
    def test_get_head_to_head_success(self, temp_cache_dir, monkeypatch, soccerdata_head_to_head):
        """Test getting head-to-head data using real fixture."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        # Fixture uses 'team1' and 'team2', not 'team_1' and 'team_2'
        team_1_id = soccerdata_head_to_head.get('team1', {}).get('id', 2916)
        team_2_id = soccerdata_head_to_head.get('team2', {}).get('id', 4148)
        
        with patch.object(service, '_make_request', return_value=soccerdata_head_to_head):
            result = service.get_head_to_head(team_1_id, team_2_id)
        
        # Assert on structure
        assert result.team_1 is not None
        assert result.team_2 is not None
        assert result.team_1.id == team_1_id
        assert result.team_2.id == team_2_id


class TestSoccerDataGetMatches:
    """Test SoccerData.get_matches() method."""
    
    def test_get_matches_by_date(self, temp_cache_dir, monkeypatch):
        """Test getting matches by date."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        response_data = [
            {
                'league_id': 39,
                'league_name': 'Premier League',
                'country': {'id': 42, 'name': 'England'},
                'is_cup': False,
                'season': {'is_active': True, 'year': '2024-2025'},
                'stage': []
            }
        ]
        
        with patch.object(service, '_make_request', return_value=response_data):
            results = service.get_matches(date='2025-01-15')
        
        assert len(results) == 1
        assert results[0].league_id == 39
    
    def test_get_matches_by_league_id(self, temp_cache_dir, monkeypatch):
        """Test getting matches by league_id using real fixture."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        response_data = [
            {
                'league_id': 39,
                'league_name': 'Premier League',
                'country': {'id': 42, 'name': 'England'},
                'is_cup': False,
                'season': {'is_active': True, 'year': '2024-2025'},
                'stage': []
            }
        ]
        
        with patch.object(service, '_make_request', return_value=response_data):
            results = service.get_matches(league_id=39)
        
        # Assert on structure
        assert len(results) > 0
        assert all(hasattr(r, 'league_id') for r in results)
        # All results should be for league 39
        assert all(r.league_id == 39 for r in results)
    
    def test_get_matches_by_league_and_season(self, temp_cache_dir, monkeypatch):
        """Test getting matches by league and season."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        response_data = [
            {
                'league_id': 39,
                'league_name': 'Premier League',
                'country': {'id': 42, 'name': 'England'},
                'is_cup': False,
                'season': {'is_active': True, 'year': '2024-2025'},
                'stage': []
            }
        ]
        
        with patch.object(service, '_make_request', return_value=response_data):
            results = service.get_matches(league_id=39, season='2024-2025')
        
        assert len(results) == 1
    
    def test_get_matches_by_league_and_date(self, temp_cache_dir, monkeypatch):
        """Test getting matches by league and date."""
        monkeypatch.setenv("SOCCERDATA_API_KEY", "test_key")
        service = SoccerData(use_cache=False)
        
        response_data = [
            {
                'league_id': 39,
                'league_name': 'Premier League',
                'country': {'id': 42, 'name': 'England'},
                'is_cup': False,
                'season': {'is_active': True, 'year': '2024-2025'},
                'stage': []
            }
        ]
        
        with patch.object(service, '_make_request', return_value=response_data):
            results = service.get_matches(league_id=39, date='2025-01-15')
        
        assert len(results) == 1
