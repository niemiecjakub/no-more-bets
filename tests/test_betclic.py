"""Tests for Betclic scraper."""
import sys
import pytest
from pathlib import Path
from unittest.mock import Mock, patch, MagicMock
from bs4 import BeautifulSoup

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.betclic import Betclic
from models.betclic import UpcomingGame, BookmakerEvent, EventOption

class TestBetclicGetUpcomingGames:
    """Test Betclic.get_upcoming_games() method."""
    
    def test_get_upcoming_games_success(self, temp_cache_dir, betclic_premier_league_html):
        """Test successful parsing of upcoming games using real fixture."""
        scraper = Betclic(use_cache=False, n_retries=1)
        
        with patch.object(scraper, '_get_page_html', return_value=betclic_premier_league_html):
            games = scraper.get_upcoming_games()
        
        # Assert on structure and types, not exact values (which may vary)
        assert len(games) > 0
        assert all(isinstance(game, UpcomingGame) for game in games)
        
        # Check first game has required fields
        first_game = games[0]
        assert first_game.home_team is not None and first_game.home_team != ""
        assert first_game.away_team is not None and first_game.away_team != ""
        # URL should be a valid Betclic URL (fixture should have URLs)
        assert first_game.url is not None, "Fixture should contain games with URLs"
        assert first_game.url.startswith("https://www.betclic.pl")
    
    def test_get_upcoming_games_no_group_events(self, temp_cache_dir):
        """Test handling when groupEvents div is not found."""
        scraper = Betclic(use_cache=False, n_retries=1)
        
        html = "<html><body></body></html>"
        
        with patch.object(scraper, '_get_page_html', return_value=html):
            games = scraper.get_upcoming_games()
        
        assert games == []
    
    def test_get_upcoming_games_retries_on_empty(self, temp_cache_dir, monkeypatch):
        """Test retry logic when empty results are returned."""
        scraper = Betclic(use_cache=False, n_retries=2)
        
        empty_html = "<html><body></body></html>"
        valid_html = """
        <html>
            <body>
                <div class="groupEvents">
                    <sports-events-event-card class="groupEvents_card">
                        <div data-qa="contestant-1-label">Arsenal</div>
                        <div data-qa="contestant-2-label">Chelsea</div>
                    </sports-events-event-card>
                </div>
            </body>
        </html>
        """
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        monkeypatch.setattr("random.uniform", lambda a, b: 5.0)
        
        with patch.object(scraper, '_get_page_html') as mock_get:
            mock_get.side_effect = [empty_html, valid_html]
            games = scraper.get_upcoming_games()
        
        assert len(games) == 1
        assert mock_get.call_count == 2


class TestBetclicParseOdds:
    """Test Betclic._parse_odds() method."""
    
    def test_parse_odds_european_format(self, temp_cache_dir):
        """Test parsing odds with European format (comma decimal)."""
        scraper = Betclic()
        
        assert scraper._parse_odds("2,10") == 2.10
        assert scraper._parse_odds("3,50") == 3.50
        assert scraper._parse_odds("1,95") == 1.95
    
    def test_parse_odds_dot_format(self, temp_cache_dir):
        """Test parsing odds with dot decimal."""
        scraper = Betclic()
        
        assert scraper._parse_odds("2.10") == 2.10
        assert scraper._parse_odds("3.50") == 3.50
    
    def test_parse_odds_invalid(self, temp_cache_dir):
        """Test parsing invalid odds."""
        scraper = Betclic()
        
        assert scraper._parse_odds("") is None
        assert scraper._parse_odds("invalid") is None
        assert scraper._parse_odds(None) is None


class TestBetclicExtractEvents:
    """Test Betclic._extract_events() method."""
    
    def test_extract_events_matrix_market(self, temp_cache_dir, betclic_match_page_html):
        """Test extracting matrix market events using real fixture."""
        scraper = Betclic(use_cache=False)
        
        events = scraper._extract_events(betclic_match_page_html)
        
        # Assert on structure - real fixture may have different markets
        assert len(events) > 0
        assert all(isinstance(event, BookmakerEvent) for event in events)
        
        # Check that events have required structure
        for event in events:
            assert event.title is not None and event.title != ""
            assert len(event.options) > 0
            for option in event.options:
                assert option.label is not None
                assert option.odds is not None and option.odds > 0
    
    def test_extract_events_grouped_market(self, temp_cache_dir):
        """Test extracting grouped market events."""
        scraper = Betclic(use_cache=False)
        
        html = """
        <html>
            <body>
                <div class="marketBox is-groupedMarket">
                    <h2 class="marketBox_headTitle">First Goal</h2>
                    <div class="marketBox_list">
                        <span class="marketBox_itemValue">First</span>
                        <span class="marketBox_itemValue">Last</span>
                        <div class="marketBox_lineSelection">
                            <p class="marketBox_label">Arsenal</p>
                            <div class="marketBox_item">
                                <span class="btn_label">2,10</span>
                            </div>
                            <div class="marketBox_item">
                                <span class="btn_label">3,50</span>
                            </div>
                        </div>
                    </div>
                </div>
            </body>
        </html>
        """
        
        events = scraper._extract_events(html)
        
        assert len(events) >= 1
        assert any(e.title == "First Goal" for e in events)
    
    def test_extract_events_split_card_goalscorer(self, temp_cache_dir):
        """Test extracting goalscorer market (split cards)."""
        scraper = Betclic(use_cache=False)
        
        html = """
        <html>
            <body>
                <div class="marketBox">
                    <h2 class="marketBox_headTitle">Anytime Goalscorer</h2>
                    <sports-split-card>
                        <div class="marketBox_bodyTitle">Arsenal</div>
                        <div class="marketBox_lineSelection">
                            <p class="marketBox_label">Saka</p>
                            <span class="btn_label">2,50</span>
                        </div>
                    </sports-split-card>
                    <sports-split-card>
                        <div class="marketBox_bodyTitle">Chelsea</div>
                        <div class="marketBox_lineSelection">
                            <p class="marketBox_label">Sterling</p>
                            <span class="btn_label">3,00</span>
                        </div>
                    </sports-split-card>
                </div>
            </body>
        </html>
        """
        
        events = scraper._extract_events(html)
        
        assert len(events) == 2
        assert any("Arsenal" in e.title for e in events)
        assert any("Chelsea" in e.title for e in events)


class TestBetclicAggregateEvents:
    """Test Betclic._aggregate_events() method."""
    
    def test_aggregate_events_same_title(self, temp_cache_dir):
        """Test aggregating events with same title."""
        scraper = Betclic()
        
        events = [
            BookmakerEvent(
                title="Total Goals",
                options=[EventOption(label="Over 2.5", odds=1.85)]
            ),
            BookmakerEvent(
                title="Total Goals",
                options=[EventOption(label="Under 2.5", odds=1.95)]
            )
        ]
        
        aggregated = scraper._aggregate_events(events)
        
        assert len(aggregated) == 1
        assert aggregated[0].title == "Total Goals"
        assert len(aggregated[0].options) == 2
    
    def test_aggregate_events_different_titles(self, temp_cache_dir):
        """Test that events with different titles are not aggregated."""
        scraper = Betclic()
        
        events = [
            BookmakerEvent(
                title="Total Goals",
                options=[EventOption(label="Over 2.5", odds=1.85)]
            ),
            BookmakerEvent(
                title="Match Winner",
                options=[EventOption(label="Arsenal", odds=2.10)]
            )
        ]
        
        aggregated = scraper._aggregate_events(events)
        
        assert len(aggregated) == 2
    
    def test_aggregate_events_empty_list(self, temp_cache_dir):
        """Test aggregating empty list."""
        scraper = Betclic()
        
        aggregated = scraper._aggregate_events([])
        
        assert aggregated == []


class TestBetclicMergeEvents:
    """Test Betclic._merge_events() method."""
    
    def test_merge_events_success(self, temp_cache_dir):
        """Test merging multiple events."""
        scraper = Betclic()
        
        events = [
            BookmakerEvent(
                title="Total Goals",
                options=[
                    EventOption(label="Over 2.5", odds=1.85),
                    EventOption(label="Under 2.5", odds=1.95)
                ]
            ),
            BookmakerEvent(
                title="Total Goals",
                options=[EventOption(label="Exactly 2.5", odds=5.00)]
            )
        ]
        
        merged = scraper._merge_events(events)
        
        assert merged.title == "Total Goals"
        assert len(merged.options) == 3
    
    def test_merge_events_single_event(self, temp_cache_dir):
        """Test merging single event (should return as-is)."""
        scraper = Betclic()
        
        event = BookmakerEvent(
            title="Total Goals",
            options=[EventOption(label="Over 2.5", odds=1.85)]
        )
        
        merged = scraper._merge_events([event])
        
        assert merged == event
    
    def test_merge_events_empty_list_raises(self, temp_cache_dir):
        """Test that merging empty list raises ValueError."""
        scraper = Betclic()
        
        with pytest.raises(ValueError, match="Cannot merge empty event list"):
            scraper._merge_events([])


class TestBetclicGetMatchEvents:
    """Test Betclic.get_match_events() method."""
    
    def test_get_match_events_success(self, temp_cache_dir, betclic_match_page_html):
        """Test successful extraction of match events using real fixture."""
        scraper = Betclic(use_cache=False, n_retries=1)
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=betclic_match_page_html):
            events = scraper.get_match_events("https://www.betclic.pl/match/12345")
        
        # Assert on structure - real fixture may have different events
        assert len(events) > 0
        assert all(isinstance(event, BookmakerEvent) for event in events)
        
        # Check that events have required structure
        for event in events:
            assert event.title is not None and event.title != ""
            assert len(event.options) > 0
    
    def test_get_match_events_retries_on_empty(self, temp_cache_dir, monkeypatch):
        """Test retry logic when empty events are returned."""
        scraper = Betclic(use_cache=False, n_retries=2)
        
        empty_html = "<html><body></body></html>"
        valid_html = """
        <html>
            <body>
                <div class="marketBox">
                    <h2 class="marketBox_headTitle">Match Winner</h2>
                    <sports-matrix-markets>
                        <div class="marketBox_lineSelection">
                            <p class="marketBox_label">Arsenal</p>
                            <span class="btn_label">2,10</span>
                        </div>
                    </sports-matrix-markets>
                </div>
            </body>
        </html>
        """
        
        mock_sleep = Mock()
        monkeypatch.setattr("time.sleep", mock_sleep)
        monkeypatch.setattr("random.uniform", lambda a, b: 10.0)
        
        with patch.object(scraper, '_get_page_html_selenium') as mock_get:
            mock_get.side_effect = [empty_html, valid_html]
            events = scraper.get_match_events("https://www.betclic.pl/match/12345")
        
        assert len(events) == 1
        assert mock_get.call_count == 2
