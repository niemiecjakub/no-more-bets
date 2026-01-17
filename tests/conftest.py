"""Shared pytest fixtures for cache utility tests."""
import os
import json
from pathlib import Path 
import pytest
from unittest.mock import Mock


@pytest.fixture
def temp_cache_dir(tmp_path: Path) -> str:
    """Create a temporary directory for cache files.
    
    Parameters
    ----------
    tmp_path : Path
        Pytest's temporary path fixture.
        
    Yields
    ------
    str
        Path to temporary cache directory.
    """
    cache_dir = tmp_path / "cache"
    cache_dir.mkdir()
    yield str(cache_dir)


@pytest.fixture
def mock_time(monkeypatch):
    """Mock time.time() for TTL testing.
    
    Parameters
    ----------
    monkeypatch : pytest.MonkeyPatch
        Pytest's monkeypatch fixture.
        
    Yields
    ------
    Mock
        Mock object for time.time() that can be controlled.
    """
    mock_time_obj = Mock()
    mock_time_obj.return_value = 1000.0  # Default timestamp
    monkeypatch.setattr("time.time", mock_time_obj)
    yield mock_time_obj


@pytest.fixture
def fixtures_dir() -> Path:
    """Get the path to the fixtures directory.
    
    Returns
    -------
    Path
        Path to tests/fixtures directory.
    """
    return Path(__file__).parent / "fixtures"


def load_fixture_html(fixture_path: Path) -> str:
    """Load HTML fixture from file.
    
    Parameters
    ----------
    fixture_path : Path
        Path to the fixture file.
        
    Returns
    -------
    str
        HTML content from fixture file.
    """
    if not fixture_path.exists():
        pytest.skip(f"Fixture file not found: {fixture_path}")
    return fixture_path.read_text(encoding='utf-8')


def load_fixture_json(fixture_path: Path):
    """Load JSON fixture from file.
    
    Parameters
    ----------
    fixture_path : Path
        Path to the fixture file.
        
    Returns
    -------
    dict | list
        JSON content from fixture file (can be dict or list).
    """
    if not fixture_path.exists():
        pytest.skip(f"Fixture file not found: {fixture_path}")
    return json.loads(fixture_path.read_text(encoding='utf-8'))


def create_test_file(filepath: str, content: str) -> None:
    """Helper function to create a test file with content.
    
    Parameters
    ----------
    filepath : str
        Path where to create the file.
    content : str
        Content to write to the file.
    """
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)


@pytest.fixture
def betclic_premier_league_html(fixtures_dir):
    """Load real Betclic Premier League page HTML."""
    fixture_path = fixtures_dir / "betclic" / "premier_league_page.html"
    return load_fixture_html(fixture_path)


@pytest.fixture
def betclic_match_page_html(fixtures_dir):
    """Load real Betclic match page HTML."""
    fixture_path = fixtures_dir / "betclic" / "match_page.html"
    return load_fixture_html(fixture_path)


@pytest.fixture
def fbref_premier_league_stats_html(fixtures_dir):
    """Load real FBref Premier League stats HTML."""
    fixture_path = fixtures_dir / "fbref" / "premier_league_stats.html"
    return load_fixture_html(fixture_path)


@pytest.fixture
def fbref_club_page_arsenal_html(fixtures_dir):
    """Load real FBref Arsenal club page HTML."""
    fixture_path = fixtures_dir / "fbref" / "club_page_arsenal.html"
    return load_fixture_html(fixture_path)


@pytest.fixture
def rotowire_lineups_html(fixtures_dir):
    """Load real Rotowire lineups page HTML."""
    fixture_path = fixtures_dir / "rotowire" / "lineups_page.html"
    return load_fixture_html(fixture_path)


@pytest.fixture
def soccerdata_match_previews_upcoming(fixtures_dir):
    """Load real SoccerData match previews upcoming JSON."""
    fixture_path = fixtures_dir / "soccerdata" / "match_previews_upcoming.json"
    return load_fixture_json(fixture_path)


@pytest.fixture
def soccerdata_match_preview(fixtures_dir):
    """Load real SoccerData match preview JSON."""
    fixture_path = fixtures_dir / "soccerdata" / "match_preview.json"
    return load_fixture_json(fixture_path)


@pytest.fixture
def soccerdata_head_to_head(fixtures_dir):
    """Load real SoccerData head-to-head JSON."""
    fixture_path = fixtures_dir / "soccerdata" / "head_to_head.json"
    return load_fixture_json(fixture_path)


@pytest.fixture
def soccerdata_matches_league_39(fixtures_dir):
    """Load real SoccerData matches for league JSON."""
    fixture_path = fixtures_dir / "soccerdata" / "matches_league.json"
    return load_fixture_json(fixture_path)


@pytest.fixture
def web_search_results_sample(fixtures_dir):
    """Load real web search results sample JSON."""
    fixture_path = fixtures_dir / "web_search" / "search_results_sample.json"
    return load_fixture_json(fixture_path)


@pytest.fixture
def web_search_news_results_sample(fixtures_dir):
    """Load real web search news results sample JSON."""
    fixture_path = fixtures_dir / "web_search" / "news_results_sample.json"
    return load_fixture_json(fixture_path)