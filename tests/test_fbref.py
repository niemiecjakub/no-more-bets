"""Tests for FBref scraper."""
import sys
import pytest
from pathlib import Path
from unittest.mock import Mock, patch, MagicMock
from bs4 import BeautifulSoup

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.fbref import FBref
from models.fbref import Club, Player, Game


class TestFBrefInitialization:
    """Test FBref initialization."""
    
    def test_init_default_parameters(self, temp_cache_dir):
        """Test initialization with default parameters."""
        scraper = FBref()
        
        assert scraper.base_url == "https://fbref.com"
        assert scraper.delay == 5.0
        assert scraper.retry_count == 3
    
    def test_init_custom_parameters(self, temp_cache_dir):
        """Test initialization with custom parameters."""
        scraper = FBref(
            delay=3.0,
            retry_count=5,
            use_cache=False
        )
        
        assert scraper.delay == 3.0
        assert scraper.retry_count == 5
        assert scraper.cache.use_cache is False


class TestFBrefGetPremierLeagueStats:
    """Test FBref.get_premier_league_stats() method."""
    
    def test_get_premier_league_stats_success(self, temp_cache_dir, fbref_premier_league_stats_html):
        """Test successful parsing of Premier League stats using real fixture."""
        scraper = FBref(use_cache=False)
        
        with patch.object(scraper, '_get_page_html', return_value=fbref_premier_league_stats_html):
            clubs = scraper.get_premier_league_stats()
        
        # Assert on structure and types - real fixture has all Premier League teams
        assert len(clubs) > 0
        assert all(isinstance(club, Club) for club in clubs)
        
        # Check first club has required fields with valid types
        first_club = clubs[0]
        assert first_club.rank > 0
        assert first_club.team is not None and first_club.team != ""
        assert isinstance(first_club.games, int) and first_club.games >= 0
        assert isinstance(first_club.wins, int) and first_club.wins >= 0
        assert isinstance(first_club.points, int) and first_club.points >= 0
        # Check that all clubs have valid data
        for club in clubs:
            assert club.rank > 0
            assert club.team != ""
    
    def test_get_premier_league_stats_missing_selector(self, temp_cache_dir):
        """Test error when selector is not found."""
        scraper = FBref(use_cache=False)
        
        html = "<html><body></body></html>"
        
        with patch.object(scraper, '_get_page_html', return_value=html):
            with pytest.raises(ValueError, match="Selector.*not found"):
                scraper.get_premier_league_stats()
    
    def test_get_premier_league_stats_handles_missing_fields(self, temp_cache_dir):
        """Test handling of missing fields in table."""
        scraper = FBref(use_cache=False)
        
        html = """
        <html>
            <body>
                <table id="results2025-202691_overall">
                    <tbody>
                        <tr>
                            <th data-stat="rank">1</th>
                            <td data-stat="team"><a>Arsenal</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        with patch.object(scraper, '_get_page_html', return_value=html):
            clubs = scraper.get_premier_league_stats()
        
        assert len(clubs) == 1
        assert clubs[0].rank == 1
        assert clubs[0].team == "Arsenal"
        # Missing fields should default to 0 or empty string
        assert clubs[0].games == 0
        assert clubs[0].wins == 0


class TestFBrefGetClubPlayers:
    """Test FBref.get_club_players() method."""
    
    def test_get_club_players_success(self, temp_cache_dir, fbref_premier_league_stats_html, fbref_club_page_arsenal_html):
        """Test successful parsing of club players using real fixtures."""
        scraper = FBref(use_cache=False)
        
        with patch.object(scraper, '_get_page_html') as mock_get:
            mock_get.side_effect = [fbref_premier_league_stats_html, fbref_club_page_arsenal_html]
            players = scraper.get_club_players("Arsenal")
        
        # Assert on structure - real fixture has multiple players
        assert len(players) > 0
        assert all(isinstance(player, Player) for player in players)
        
        # Check first player has required fields with valid types
        first_player = players[0]
        assert first_player.player is not None and first_player.player != ""
        assert isinstance(first_player.games, int) and first_player.games >= 0
        assert isinstance(first_player.goals, int) and first_player.goals >= 0
        assert isinstance(first_player.minutes, int) and first_player.minutes >= 0
    
    def test_get_club_players_club_not_found(self, temp_cache_dir):
        """Test error when club is not found."""
        scraper = FBref(use_cache=False)
        
        league_html = """
        <html>
            <body>
                <table id="results2025-202691_overall">
                    <tbody>
                        <tr>
                            <td data-stat="team"><a href="/en/squads/18bb7c10/Arsenal-Stats">Arsenal</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        with patch.object(scraper, '_get_page_html', return_value=league_html):
            with pytest.raises(ValueError, match="Club 'Chelsea' not found"):
                scraper.get_club_players("Chelsea")
    
    def test_get_club_players_handles_iz_class(self, temp_cache_dir):
        """Test handling of 'iz' class for zero values."""
        scraper = FBref(use_cache=False)
        
        league_html = """
        <html>
            <body>
                <table id="results2025-202691_overall">
                    <tbody>
                        <tr>
                            <td data-stat="team"><a href="/en/squads/18bb7c10/Arsenal-Stats">Arsenal</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        club_html = """
        <html>
            <body>
                <table id="stats_standard_9">
                    <tbody>
                        <tr>
                            <th data-stat="player"><a>Player Name</a></th>
                            <td data-stat="nationality"><span>en ENG</span></td>
                            <td data-stat="position">GK</td>
                            <td data-stat="age">25</td>
                            <td data-stat="games" class="iz">0</td>
                            <td data-stat="goals" class="iz">0</td>
                            <td data-stat="minutes" class="iz">0</td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        with patch.object(scraper, '_get_page_html') as mock_get:
            mock_get.side_effect = [league_html, club_html]
            players = scraper.get_club_players("Arsenal")
        
        assert len(players) == 1
        assert players[0].games == 0
        assert players[0].goals == 0
        assert players[0].minutes == 0


class TestFBrefGetClubGames:
    """Test FBref.get_club_games() method."""
    
    def test_get_club_games_success(self, temp_cache_dir, fbref_premier_league_stats_html, fbref_club_page_arsenal_html):
        """Test successful parsing of club games using real fixtures."""
        scraper = FBref(use_cache=False)
        
        with patch.object(scraper, '_get_page_html') as mock_get:
            mock_get.side_effect = [fbref_premier_league_stats_html, fbref_club_page_arsenal_html]
            games = scraper.get_club_games("Arsenal")
        
        # Assert on structure - real fixture has multiple games
        assert len(games) > 0
        assert all(isinstance(game, Game) for game in games)
        
        # Check first game has required fields with valid types
        first_game = games[0]
        assert first_game.date is not None and first_game.date != ""
        assert first_game.opponent is not None and first_game.opponent != ""
        # Check that games have valid data structure
        for game in games:
            assert game.date != ""
            assert game.opponent != ""
    
    def test_get_club_games_epl_only_filter(self, temp_cache_dir):
        """Test filtering by epl_only."""
        scraper = FBref(use_cache=False)
        
        league_html = """
        <html>
            <body>
                <table id="results2025-202691_overall">
                    <tbody>
                        <tr>
                            <td data-stat="team"><a href="/en/squads/18bb7c10/Arsenal-Stats">Arsenal</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        club_html = """
        <html>
            <body>
                <table id="matchlogs_for">
                    <tbody>
                        <tr>
                            <th data-stat="date"><a>2025-01-15</a></th>
                            <td data-stat="comp"><a>Premier League</a></td>
                            <td data-stat="opponent"><a>Chelsea</a></td>
                        </tr>
                        <tr>
                            <th data-stat="date"><a>2025-01-10</a></th>
                            <td data-stat="comp"><a>Champions League</a></td>
                            <td data-stat="opponent"><a>Barcelona</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        with patch.object(scraper, '_get_page_html') as mock_get:
            mock_get.side_effect = [league_html, club_html]
            games = scraper.get_club_games("Arsenal", epl_only=True)
        
        assert len(games) == 1
        assert games[0].comp == "Premier League"
    
    def test_get_club_games_limit(self, temp_cache_dir):
        """Test limiting number of games returned."""
        scraper = FBref(use_cache=False)
        
        league_html = """
        <html>
            <body>
                <table id="results2025-202691_overall">
                    <tbody>
                        <tr>
                            <td data-stat="team"><a href="/en/squads/18bb7c10/Arsenal-Stats">Arsenal</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        club_html = """
        <html>
            <body>
                <table id="matchlogs_for">
                    <tbody>
                        <tr>
                            <th data-stat="date"><a>2025-01-15</a></th>
                            <td data-stat="comp"><a>Premier League</a></td>
                            <td data-stat="opponent"><a>Chelsea</a></td>
                        </tr>
                        <tr>
                            <th data-stat="date"><a>2025-01-10</a></th>
                            <td data-stat="comp"><a>Premier League</a></td>
                            <td data-stat="opponent"><a>Liverpool</a></td>
                        </tr>
                        <tr>
                            <th data-stat="date"><a>2025-01-05</a></th>
                            <td data-stat="comp"><a>Premier League</a></td>
                            <td data-stat="opponent"><a>Manchester City</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        with patch.object(scraper, '_get_page_html') as mock_get:
            mock_get.side_effect = [league_html, club_html]
            games = scraper.get_club_games("Arsenal", limit=2)
        
        assert len(games) == 2
    
    def test_get_club_games_only_finished(self, temp_cache_dir):
        """Test filtering by only_finished."""
        scraper = FBref(use_cache=False)
        
        league_html = """
        <html>
            <body>
                <table id="results2025-202691_overall">
                    <tbody>
                        <tr>
                            <td data-stat="team"><a href="/en/squads/18bb7c10/Arsenal-Stats">Arsenal</a></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        club_html = """
        <html>
            <body>
                <table id="matchlogs_for">
                    <tbody>
                        <tr>
                            <th data-stat="date"><a>2025-01-15</a></th>
                            <td data-stat="comp"><a>Premier League</a></td>
                            <td data-stat="opponent"><a>Chelsea</a></td>
                            <td data-stat="result">W</td>
                        </tr>
                        <tr>
                            <th data-stat="date"><a>2025-01-20</a></th>
                            <td data-stat="comp"><a>Premier League</a></td>
                            <td data-stat="opponent"><a>Liverpool</a></td>
                            <td data-stat="result"></td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        with patch.object(scraper, '_get_page_html') as mock_get:
            mock_get.side_effect = [league_html, club_html]
            games = scraper.get_club_games("Arsenal", only_finished=True)
        
        assert len(games) == 1
        assert games[0].result == "W"
