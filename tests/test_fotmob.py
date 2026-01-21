"""Tests for FotMob scraper."""
import sys
import pytest
from pathlib import Path
from unittest.mock import patch
from bs4 import BeautifulSoup

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.fotmob import FotMob
from models.fotmob import Club, XgStats


class TestFotMobGetPremierLeagueTable:
    """Test FotMob.get_premier_league_table() method."""
    
    def test_get_premier_league_table_success(self, temp_cache_dir, fotmob_table_html):
        """Test successful parsing of Premier League table using real fixture."""
        scraper = FotMob(use_cache=False)
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=fotmob_table_html):
            clubs = scraper.get_premier_league_table()
        
        # Assert on structure and types - real fixture has all Premier League teams
        assert len(clubs) > 0
        assert all(isinstance(club, Club) for club in clubs)
        
        # Check first club has required fields with valid types
        first_club = clubs[0]
        assert first_club.position > 0
        assert first_club.team_name is not None and first_club.team_name != ""
        assert isinstance(first_club.team_id, int) and first_club.team_id > 0
        assert isinstance(first_club.matches_played, int) and first_club.matches_played >= 0
        assert isinstance(first_club.wins, int) and first_club.wins >= 0
        assert isinstance(first_club.draws, int) and first_club.draws >= 0
        assert isinstance(first_club.losses, int) and first_club.losses >= 0
        assert isinstance(first_club.goals_for, int) and first_club.goals_for >= 0
        assert isinstance(first_club.goals_against, int) and first_club.goals_against >= 0
        assert isinstance(first_club.points, int) and first_club.points >= 0
        assert isinstance(first_club.goal_difference, str)
        
        # Check that all clubs have valid data
        for club in clubs:
            assert club.position > 0
            assert club.team_name != ""
            assert club.team_id > 0
    
    def test_get_premier_league_table_missing_container(self, temp_cache_dir):
        """Test error when table container is not found."""
        scraper = FotMob(use_cache=False)
        
        html = "<html><body></body></html>"
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=html):
            with pytest.raises(ValueError, match="Table container not found"):
                scraper.get_premier_league_table()
    
    def test_get_premier_league_table_empty_rows(self, temp_cache_dir):
        """Test handling when no valid rows found."""
        scraper = FotMob(use_cache=False)
        
        html = """
        <html>
            <body>
                <article class="TableContainer">
                </article>
            </body>
        </html>
        """
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=html):
            clubs = scraper.get_premier_league_table()
        
        assert clubs == []


class TestFotMobGetHomeStats:
    """Test FotMob.get_home_stats() method."""
    
    def test_get_home_stats_success(self, temp_cache_dir, fotmob_home_html):
        """Test successful parsing of home stats using real fixture."""
        scraper = FotMob(use_cache=False)
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=fotmob_home_html):
            clubs = scraper.get_home_stats()
        
        # Assert on structure and types
        assert len(clubs) > 0
        assert all(isinstance(club, Club) for club in clubs)
        
        # Check first club has required fields with valid types
        first_club = clubs[0]
        assert first_club.position > 0
        assert first_club.team_name is not None and first_club.team_name != ""
        assert isinstance(first_club.team_id, int) and first_club.team_id > 0
        assert isinstance(first_club.matches_played, int) and first_club.matches_played >= 0
        assert isinstance(first_club.points, int) and first_club.points >= 0
        
        # Check that all clubs have valid data
        for club in clubs:
            assert club.position > 0
            assert club.team_name != ""
            assert club.team_id > 0
    
    def test_get_home_stats_missing_container(self, temp_cache_dir):
        """Test error when table container is not found."""
        scraper = FotMob(use_cache=False)
        
        html = "<html><body></body></html>"
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=html):
            with pytest.raises(ValueError, match="Table container not found"):
                scraper.get_home_stats()


class TestFotMobGetAwayStats:
    """Test FotMob.get_away_stats() method."""
    
    def test_get_away_stats_success(self, temp_cache_dir, fotmob_away_html):
        """Test successful parsing of away stats using real fixture."""
        scraper = FotMob(use_cache=False)
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=fotmob_away_html):
            clubs = scraper.get_away_stats()
        
        # Assert on structure and types
        assert len(clubs) > 0
        assert all(isinstance(club, Club) for club in clubs)
        
        # Check first club has required fields with valid types
        first_club = clubs[0]
        assert first_club.position > 0
        assert first_club.team_name is not None and first_club.team_name != ""
        assert isinstance(first_club.team_id, int) and first_club.team_id > 0
        assert isinstance(first_club.matches_played, int) and first_club.matches_played >= 0
        assert isinstance(first_club.points, int) and first_club.points >= 0
        
        # Check that all clubs have valid data
        for club in clubs:
            assert club.position > 0
            assert club.team_name != ""
            assert club.team_id > 0
    
    def test_get_away_stats_missing_container(self, temp_cache_dir):
        """Test error when table container is not found."""
        scraper = FotMob(use_cache=False)
        
        html = "<html><body></body></html>"
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=html):
            with pytest.raises(ValueError, match="Table container not found"):
                scraper.get_away_stats()


class TestFotMobGetLast5GamesStats:
    """Test FotMob.get_lat_5_games_stats() method."""
    
    def test_get_lat_5_games_stats_success(self, temp_cache_dir, fotmob_last_5_games_html):
        """Test successful parsing of last 5 games stats using real fixture."""
        scraper = FotMob(use_cache=False)
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=fotmob_last_5_games_html):
            clubs = scraper.get_lat_5_games_stats()
        
        # Assert on structure and types
        assert len(clubs) > 0
        assert all(isinstance(club, Club) for club in clubs)
        
        # Check first club has required fields with valid types
        first_club = clubs[0]
        assert first_club.position > 0
        assert first_club.team_name is not None and first_club.team_name != ""
        assert isinstance(first_club.team_id, int) and first_club.team_id > 0
        assert isinstance(first_club.matches_played, int) and first_club.matches_played >= 0
        assert isinstance(first_club.points, int) and first_club.points >= 0
        
        # Check that all clubs have valid data
        for club in clubs:
            assert club.position > 0
            assert club.team_name != ""
            assert club.team_id > 0
    
    def test_get_lat_5_games_stats_missing_container(self, temp_cache_dir):
        """Test error when table container is not found."""
        scraper = FotMob(use_cache=False)
        
        html = "<html><body></body></html>"
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=html):
            with pytest.raises(ValueError, match="Table container not found"):
                scraper.get_lat_5_games_stats()


class TestFotMobGetXgStats:
    """Test FotMob.get_xg_stats() method."""
    
    def test_get_xg_stats_success(self, temp_cache_dir, fotmob_xg_html):
        """Test successful parsing of xG stats using real fixture."""
        scraper = FotMob(use_cache=False)
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=fotmob_xg_html):
            xg_stats = scraper.get_xg_stats()
        
        # Assert on structure and types
        assert len(xg_stats) > 0
        assert all(isinstance(stat, XgStats) for stat in xg_stats)
        
        # Check first stat has required fields with valid types
        first_stat = xg_stats[0]
        assert first_stat.position > 0
        assert first_stat.team_name is not None and first_stat.team_name != ""
        assert isinstance(first_stat.team_id, int) and first_stat.team_id > 0
        assert isinstance(first_stat.xg, float) and first_stat.xg >= 0
        assert isinstance(first_stat.xga, float) and first_stat.xga >= 0
        assert isinstance(first_stat.xpts, float) and first_stat.xpts >= 0
        
        # Check that all stats have valid data
        for stat in xg_stats:
            assert stat.position > 0
            assert stat.team_name != ""
            assert stat.team_id > 0
            assert stat.xg is not None
            assert stat.xga is not None
            assert stat.xpts is not None
    
    def test_get_xg_stats_missing_container(self, temp_cache_dir):
        """Test error when table container is not found."""
        scraper = FotMob(use_cache=False)
        
        html = "<html><body></body></html>"
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=html):
            with pytest.raises(ValueError, match="Table container not found"):
                scraper.get_xg_stats()
    
    def test_get_xg_stats_missing_values(self, temp_cache_dir):
        """Test handling when xG values cannot be extracted."""
        scraper = FotMob(use_cache=False)
        
        html = """
        <html>
            <body>
                <article class="TableContainer">
                    <div class="TableRowCSS">
                        <td>1</td>
                        <td><div class="ChevronWrapper"><span>0</span></div></td>
                        <td><a class="TeamLink" href="/teams/9825/overview/arsenal">Arsenal</a></td>
                        <td>20</td>
                        <td></td>
                        <td></td>
                        <td></td>
                    </div>
                </article>
            </body>
        </html>
        """
        
        with patch.object(scraper, '_get_page_html_selenium', return_value=html):
            xg_stats = scraper.get_xg_stats()
        
        # Should skip rows where xG values cannot be extracted
        assert len(xg_stats) == 0


class TestFotMobExtractInt:
    """Test FotMob._extract_int() helper method."""
    
    def test_extract_int_valid_number(self, temp_cache_dir):
        """Test extracting valid integers."""
        scraper = FotMob(use_cache=False)
        soup = BeautifulSoup("<div>42</div>", 'lxml')
        element = soup.find('div')
        
        result = scraper._extract_int(element)
        
        assert result == 42
    
    def test_extract_int_with_whitespace(self, temp_cache_dir):
        """Test handling whitespace."""
        scraper = FotMob(use_cache=False)
        soup = BeautifulSoup("<div>  25  </div>", 'lxml')
        element = soup.find('div')
        
        result = scraper._extract_int(element)
        
        assert result == 25
    
    def test_extract_int_with_non_digits(self, temp_cache_dir):
        """Test handling non-digit characters."""
        scraper = FotMob(use_cache=False)
        soup = BeautifulSoup("<div>Score: 15 points</div>", 'lxml')
        element = soup.find('div')
        
        result = scraper._extract_int(element)
        
        assert result == 15
    
    def test_extract_int_negative(self, temp_cache_dir):
        """Test extracting negative numbers."""
        scraper = FotMob(use_cache=False)
        soup = BeautifulSoup("<div>-5</div>", 'lxml')
        element = soup.find('div')
        
        result = scraper._extract_int(element)
        
        assert result == -5
    
    def test_extract_int_invalid(self, temp_cache_dir):
        """Test handling invalid/non-numeric text."""
        scraper = FotMob(use_cache=False)
        soup = BeautifulSoup("<div>abc</div>", 'lxml')
        element = soup.find('div')
        
        result = scraper._extract_int(element)
        
        assert result == 0
    
    def test_extract_int_none_element(self, temp_cache_dir):
        """Test handling None element."""
        scraper = FotMob(use_cache=False)
        
        result = scraper._extract_int(None)
        
        assert result == 0
    
    def test_extract_int_empty_string(self, temp_cache_dir):
        """Test handling empty string."""
        scraper = FotMob(use_cache=False)
        soup = BeautifulSoup("<div></div>", 'lxml')
        element = soup.find('div')
        
        result = scraper._extract_int(element)
        
        assert result == 0
