"""Tests for PremierInjuries scraper."""
import sys
import pytest
from pathlib import Path
from unittest.mock import Mock, patch
from bs4 import BeautifulSoup

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from services.premierinjuries import PremierInjuries
from models.premierinjuries import InjuryData, TeamInjury, PlayerInjury


class TestPremierInjuriesParsePremierLeagueInjuries:
    """Test PremierInjuries._parse_premier_league_injuries() method."""
    
    def test_parse_premier_league_injuries_success(self, temp_cache_dir, premierinjuries_premier_league_html):
        """Test successful parsing of Premier League injuries using real fixture."""
        scraper = PremierInjuries(use_cache=False)
        
        result = scraper._parse_premier_league_injuries(premierinjuries_premier_league_html)
        
        # Assert on structure - real fixture has multiple teams
        assert isinstance(result, InjuryData)
        assert len(result.teams) > 0
        
        # Check that all teams have required structure
        for team in result.teams:
            assert isinstance(team, TeamInjury)
            assert team.team_name is not None and team.team_name != ""
            assert team.team_id is not None
            assert isinstance(team.players, list)
            
            # Check players have required fields
            for player in team.players:
                assert isinstance(player, PlayerInjury)
                assert player.player is not None and player.player != ""
                assert player.reason is not None
                assert player.team_id == team.team_id
    
    def test_parse_premier_league_injuries_missing_content(self, temp_cache_dir):
        """Test handling of missing article content."""
        scraper = PremierInjuries(use_cache=False)
        
        html = "<html><body></body></html>"
        
        result = scraper._parse_premier_league_injuries(html)
        
        assert isinstance(result, InjuryData)
        assert len(result.teams) == 0
    
    def test_parse_premier_league_injuries_team_id_generation(self, temp_cache_dir):
        """Test that team IDs are generated consistently."""
        scraper = PremierInjuries(use_cache=False)
        
        html = """
        <html>
            <body>
                <div class="article__content">
                    <h2>Arsenal</h2>
                    <table class="article__table article__table-scrollable">
                        <tbody>
                            <tr>
                                <th>Player</th>
                                <td>Injury</td>
                                <td>-</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </body>
        </html>
        """
        
        result1 = scraper._parse_premier_league_injuries(html)
        result2 = scraper._parse_premier_league_injuries(html)
        
        # Team IDs should be consistent across calls
        assert len(result1.teams) > 0, "Fixture should produce at least one team"
        assert len(result2.teams) > 0, "Fixture should produce at least one team"
        assert result1.teams[0].team_id == result2.teams[0].team_id
    
    def test_parse_premier_league_injuries_handles_missing_table(self, temp_cache_dir):
        """Test handling of teams without tables."""
        scraper = PremierInjuries(use_cache=False)
        
        html = """
        <html>
            <body>
                <div class="article__content">
                    <h2>Arsenal</h2>
                    <h2>Chelsea</h2>
                    <table class="article__table article__table-scrollable">
                        <tbody>
                            <tr>
                                <th>Player</th>
                                <td>Injury</td>
                                <td>-</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </body>
        </html>
        """
        
        result = scraper._parse_premier_league_injuries(html)
        
        # Arsenal should have no players (no table)
        assert len(result.teams) == 2
        assert result.teams[0].team_name == "Arsenal"
        assert len(result.teams[0].players) == 0
        
        # Chelsea should have players
        assert result.teams[1].team_name == "Chelsea"
        assert len(result.teams[1].players) == 1


class TestPremierInjuriesParseInjuryTable:
    """Test PremierInjuries._parse_injury_table() method."""
    
    def test_parse_injury_table_success(self, temp_cache_dir):
        """Test successful parsing of injury table format."""
        scraper = PremierInjuries(use_cache=False)
        
        html = """
        <html>
            <body>
                <table class="injury-table">
                    <tbody>
                        <tr class="heading" data-team-id="1">
                            <div class="injury-team">Arsenal</div>
                            <span class="injury-count2-num">2</span>
                        </tr>
                        <tr class="team_1 player-row">
                            <td>Bukayo Saka</td>
                            <td>Hamstring</td>
                            <td>Strain</td>
                            <td>2025-02-01</td>
                            <td>Doubtful</td>
                            <td>Training</td>
                        </tr>
                        <tr class="team_1 player-row">
                            <td>Gabriel Jesus</td>
                            <td>Knee</td>
                            <td>Injury</td>
                            <td>2025-02-15</td>
                            <td>Out</td>
                            <td>Recovery</td>
                        </tr>
                    </tbody>
                </table>
            </body>
        </html>
        """
        
        result = scraper._parse_injury_table(html)
        
        assert isinstance(result, InjuryData)
        assert len(result.teams) == 1
        
        team = result.teams[0]
        assert team.team_name == "Arsenal"
        assert team.team_id == 1
        assert team.injury_count == 2
        assert len(team.players) == 2
        
        assert team.players[0].player == "Bukayo Saka"
        assert team.players[0].reason == "Hamstring"
        assert team.players[0].further_detail == "Strain"
        assert team.players[0].potential_return == "2025-02-01"
        assert team.players[0].condition == "Doubtful"
        assert team.players[0].status == "Training"
    
    def test_parse_injury_table_missing_table(self, temp_cache_dir):
        """Test handling of missing injury table."""
        scraper = PremierInjuries(use_cache=False)
        
        html = "<html><body></body></html>"
        
        result = scraper._parse_injury_table(html)
        
        assert isinstance(result, InjuryData)
        assert len(result.teams) == 0


class TestPremierInjuriesGetPremierLeagueInjuriesHtml:
    """Test PremierInjuries.get_premier_league_injuries_html() method."""
    
    def test_get_premier_league_injuries_html_success(self, temp_cache_dir, premierinjuries_premier_league_html):
        """Test successful fetching and parsing using real fixture."""
        scraper = PremierInjuries(use_cache=False)
        
        with patch.object(scraper, '_get_page_html', return_value=premierinjuries_premier_league_html):
            result = scraper.get_premier_league_injuries_html()
        
        assert isinstance(result, InjuryData)
        assert len(result.teams) > 0
        # Check structure
        for team in result.teams:
            assert team.team_name != ""
            assert team.team_id is not None


class TestPremierInjuriesExtractTextAfterMobTitle:
    """Test PremierInjuries._extract_text_after_mob_title() method."""
    
    def test_extract_text_after_mob_title_success(self, temp_cache_dir):
        """Test successful extraction of text after mob-title."""
        scraper = PremierInjuries(use_cache=False)
        
        html = """
        <td>
            <div class="mob-title">Player</div>
            Bukayo Saka
        </td>
        """
        
        soup = BeautifulSoup(html, 'lxml')
        cell = soup.find('td')
        
        result = scraper._extract_text_after_mob_title(cell)
        
        assert result == "Bukayo Saka"
    
    def test_extract_text_after_mob_title_no_mob_title(self, temp_cache_dir):
        """Test extraction when no mob-title exists."""
        scraper = PremierInjuries(use_cache=False)
        
        html = """
        <td>Simple text</td>
        """
        
        soup = BeautifulSoup(html, 'lxml')
        cell = soup.find('td')
        
        result = scraper._extract_text_after_mob_title(cell)
        
        assert result == "Simple text"
