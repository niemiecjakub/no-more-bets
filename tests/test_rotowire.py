"""Tests for Rotowire scraper."""
import sys
import pytest
from pathlib import Path
from unittest.mock import Mock, patch
from bs4 import BeautifulSoup

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.rotowire import Rotowire
from models.rotowire import GameLineup, TeamLineup, PlayerInLineup, InjuryEntry, GameOdds, WeatherInfo


class TestRotowireInitialization:
    """Test Rotowire initialization."""
    
    def test_init_default_parameters(self, temp_cache_dir):
        """Test initialization with default parameters."""
        scraper = Rotowire()
        
        assert scraper.base_url == "https://www.rotowire.com"
        assert scraper.delay == 5.0
        assert scraper.retry_count == 3


class TestRotowireParseLineups:
    """Test Rotowire._parse_lineups() method."""
    
    def test_parse_lineups_success(self, temp_cache_dir, rotowire_lineups_html):
        """Test successful parsing of lineups using real fixture."""
        scraper = Rotowire(use_cache=False)
        
        games = scraper._parse_lineups(rotowire_lineups_html)
        
        # Assert on structure - real fixture has multiple games
        assert len(games) > 0
        assert all(isinstance(game, GameLineup) for game in games)
        
        # Check first game has required structure
        first_game = games[0]
        assert first_game.home_team is not None
        assert first_game.away_team is not None
        assert first_game.home_team.team_code is not None
        assert first_game.away_team.team_code is not None
    
    def test_parse_lineups_multiple_games(self, temp_cache_dir, rotowire_lineups_html):
        """Test parsing multiple games using real fixture."""
        scraper = Rotowire(use_cache=False)
        
        games = scraper._parse_lineups(rotowire_lineups_html)
        
        # Real fixture should have multiple games
        assert len(games) > 0
        # Verify all games have valid structure
        for game in games:
            assert game.home_team is not None
            assert game.away_team is not None


class TestRotowireParseTeamLineup:
    """Test Rotowire._parse_team_lineup() method."""
    
    def test_parse_team_lineup_confirmed(self, temp_cache_dir):
        """Test parsing confirmed lineup."""
        scraper = Rotowire(use_cache=False)
        
        html = """
        <div class="lineup is-soccer">
            <div class="lineup__team is-home">
                <div class="lineup__abbr">ARS</div>
            </div>
            <ul class="lineup__list is-home">
                <li class="lineup__status">Confirmed Lineup</li>
                <li class="lineup__player">
                    <div class="lineup__pos">GK</div>
                    <a>Ramsdale</a>
                </li>
                <li class="lineup__player">
                    <div class="lineup__pos">DF</div>
                    <a>White</a>
                </li>
            </ul>
        </div>
        """
        
        soup = BeautifulSoup(html, 'lxml')
        section = soup.find('div', class_='lineup')
        
        lineup = scraper._parse_team_lineup(section, "ARS", "Arsenal")
        
        assert isinstance(lineup, TeamLineup)
        assert lineup.team_name == "Arsenal"
        assert lineup.team_code == "ARS"
        assert lineup.lineup_type == "Confirmed Lineup"
        assert len(lineup.players) == 2
        assert lineup.players[0].position == "GK"
        assert lineup.players[0].player_name == "Ramsdale"
    
    def test_parse_team_lineup_with_injuries(self, temp_cache_dir):
        """Test parsing lineup with injuries section."""
        scraper = Rotowire(use_cache=False)
        
        html = """
        <div class="lineup is-soccer">
            <div class="lineup__team is-home">
                <div class="lineup__abbr">ARS</div>
            </div>
            <ul class="lineup__list is-home">
                <li class="lineup__status">Confirmed Lineup</li>
                <li class="lineup__player">
                    <div class="lineup__pos">GK</div>
                    <a>Ramsdale</a>
                </li>
                <li class="lineup__title">Injuries</li>
                <li class="lineup__player">
                    <div class="lineup__pos">FW</div>
                    <a>Jesus</a>
                    <span class="lineup__inj">Doubtful</span>
                </li>
            </ul>
        </div>
        """
        
        soup = BeautifulSoup(html, 'lxml')
        section = soup.find('div', class_='lineup')
        
        lineup = scraper._parse_team_lineup(section, "ARS", "Arsenal")
        
        assert len(lineup.players) == 1  # Only players before Injuries separator
        assert len(lineup.injuries) == 1
        assert lineup.injuries[0].player == "Jesus"
        assert lineup.injuries[0].position == "FW"
        assert lineup.injuries[0].status == "Doubtful"


class TestRotowireParseOdds:
    """Test Rotowire._parse_odds() method."""
    
    def test_parse_odds_success(self, temp_cache_dir):
        """Test successful parsing of odds."""
        scraper = Rotowire(use_cache=False)
        
        html = """
        <div class="lineup is-soccer">
            <div class="lineup__team is-home">
                <div class="lineup__abbr">ARS</div>
            </div>
            <div class="lineup__team is-visit">
                <div class="lineup__abbr">CHE</div>
            </div>
            <div class="lineup__odds">
                <div class="lineup__odds-item">
                    ARS: <span class="is-selected">2.10</span>
                </div>
                <div class="lineup__odds-item">
                    Draw: <span class="is-selected">3.50</span>
                </div>
                <div class="lineup__odds-item">
                    CHE: <span class="is-selected">3.20</span>
                </div>
            </div>
        </div>
        """
        
        soup = BeautifulSoup(html, 'lxml')
        section = soup.find('div', class_='lineup')
        
        odds = scraper._parse_odds(section)
        
        assert isinstance(odds, GameOdds)
        assert odds.home_odds == "2.10"
        assert odds.draw_odds == "3.50"
        assert odds.away_odds == "3.20"
    
    def test_parse_odds_handles_dashes(self, temp_cache_dir):
        """Test handling of dash values in odds."""
        scraper = Rotowire(use_cache=False)
        
        html = """
        <div class="lineup is-soccer">
            <div class="lineup__team is-home">
                <div class="lineup__abbr">ARS</div>
            </div>
            <div class="lineup__odds">
                <div class="lineup__odds-item">
                    ARS: <span class="is-selected">–</span>
                </div>
            </div>
        </div>
        """
        
        soup = BeautifulSoup(html, 'lxml')
        section = soup.find('div', class_='lineup')
        
        odds = scraper._parse_odds(section)
        
        # Dashes should be skipped
        assert odds is None or odds.home_odds is None


class TestRotowireParseWeather:
    """Test Rotowire._parse_weather() method."""
    
    def test_parse_weather_success(self, temp_cache_dir):
        """Test successful parsing of weather."""
        scraper = Rotowire(use_cache=False)
        
        html = """
        <div class="lineup is-soccer">
            <div class="lineup__weather">
                <img class="lineup__weather-icon" alt="Clear">
                <div class="lineup__weather-text">
                    49° 10% Precipitation Wind 5 mph 153.3
                </div>
            </div>
        </div>
        """
        
        soup = BeautifulSoup(html, 'lxml')
        section = soup.find('div', class_='lineup')
        
        weather = scraper._parse_weather(section)
        
        assert isinstance(weather, WeatherInfo)
        assert weather.condition == "Clear"
        assert weather.precipitation == "10%"
        assert weather.temperature == "49°"
        assert weather.wind == "5 mph"
    
    def test_parse_weather_missing_section(self, temp_cache_dir):
        """Test handling of missing weather section."""
        scraper = Rotowire(use_cache=False)
        
        html = """
        <div class="lineup is-soccer">
        </div>
        """
        
        soup = BeautifulSoup(html, 'lxml')
        section = soup.find('div', class_='lineup')
        
        weather = scraper._parse_weather(section)
        
        assert weather is None


class TestRotowireGetSoccerLineups:
    """Test Rotowire.get_soccer_lineups() method."""
    
    def test_get_soccer_lineups_success(self, temp_cache_dir, rotowire_lineups_html):
        """Test successful fetching and parsing of lineups using real fixture."""
        scraper = Rotowire(use_cache=False)
        
        with patch.object(scraper, '_get_page_html', return_value=rotowire_lineups_html):
            games = scraper.get_soccer_lineups()
        
        assert len(games) > 0
        assert all(isinstance(game, GameLineup) for game in games)
        # Verify structure
        for game in games:
            assert game.home_team is not None
            assert game.away_team is not None
