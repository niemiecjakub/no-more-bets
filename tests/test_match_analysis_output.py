"""Tests for match analysis output handlers."""
import pytest
from io import StringIO
import sys
from models.match_analysis import (
    MatchInfo,
    LineupData,
    TeamLineupData,
    HeadToHeadData,
    MatchPreviewData,
    PredictionData,
    WeatherData,
)
from models.rotowire import PlayerInLineup, InjuryEntry
from models.soccerdata import TeamInfo, OverallStats, Team1AtHomeStats, Team2AtHomeStats
from models.betclic import BookmakerEvent, EventOption
from output.match_analysis_output import ConsoleOutput, SilentOutput


@pytest.fixture
def sample_match_info():
    """Create sample match info."""
    return MatchInfo(
        home_team="Arsenal",
        away_team="Chelsea",
        date="2026-01-17",
        time="15:00"
    )


@pytest.fixture
def sample_lineup_data():
    """Create sample lineup data."""
    return LineupData(
        home_team=TeamLineupData(
            team_name="Arsenal",
            lineup_type="Predicted Lineup",
            players=[PlayerInLineup(position="GK", player_name="Player 1")] * 11,
            injuries=[]
        ),
        away_team=TeamLineupData(
            team_name="Chelsea",
            lineup_type="Predicted Lineup",
            players=[PlayerInLineup(position="GK", player_name="Player 2")] * 11,
            injuries=[InjuryEntry(player="Injured Player", position="FW", status="OUT")]
        )
    )


@pytest.fixture
def sample_head_to_head():
    """Create sample head-to-head data."""
    return HeadToHeadData(
        team1=TeamInfo(id=1, name="Arsenal"),
        team2=TeamInfo(id=2, name="Chelsea"),
        overall=OverallStats(
            overall_games_played=10,
            overall_team1_wins=5,
            overall_team2_wins=3,
            overall_draws=2,
            overall_team1_scored=15,
            overall_team2_scored=10
        ),
        team1_at_home=Team1AtHomeStats(
            team1_games_played_at_home=5,
            team1_wins_at_home=3,
            team1_losses_at_home=1,
            team1_draws_at_home=1,
            team1_scored_at_home=8,
            team1_conceded_at_home=4
        ),
        team2_at_home=Team2AtHomeStats(
            team2_games_played_at_home=5,
            team2_wins_at_home=2,
            team2_losses_at_home=2,
            team2_draws_at_home=1,
            team2_scored_at_home=6,
            team2_conceded_at_home=7
        )
    )


@pytest.fixture
def sample_match_preview():
    """Create sample match preview data."""
    return MatchPreviewData(
        excitement_rating=8.5,
        prediction=PredictionData(
            type="match_winner",
            choice="home",
            team_name="Arsenal"
        ),
        weather=WeatherData(
            description="Cloudy",
            temp_c=15.0,
            temp_f=59.0
        ),
        preview_content=[]
    )


@pytest.fixture
def sample_betting_events():
    """Create sample betting events."""
    return [
        BookmakerEvent(
            title="Match Winner",
            options=[EventOption(label="Home", odds=2.0)]
        ),
        BookmakerEvent(
            title="Total Goals",
            options=[EventOption(label="Over 2.5", odds=1.5)]
        ),
    ]


class TestConsoleOutput:
    """Test cases for ConsoleOutput."""
    
    def test_print_match_header(self, sample_match_info, capsys):
        """Test printing match header."""
        output = ConsoleOutput()
        output.print_match_header(sample_match_info)
        
        captured = capsys.readouterr()
        assert "Arsenal" in captured.out
        assert "Chelsea" in captured.out
        assert "2026-01-17" in captured.out
        assert "15:00" in captured.out
    
    def test_print_lineup(self, sample_lineup_data, capsys):
        """Test printing lineup."""
        output = ConsoleOutput()
        output.print_lineup(sample_lineup_data, "Arsenal", "Chelsea")
        
        captured = capsys.readouterr()
        assert "Arsenal" in captured.out
        assert "Chelsea" in captured.out
        assert "Predicted Lineup" in captured.out
    
    def test_print_head_to_head(self, sample_head_to_head, capsys):
        """Test printing head-to-head data."""
        output = ConsoleOutput()
        output.print_head_to_head(sample_head_to_head)
        
        captured = capsys.readouterr()
        assert "Head-to-Head" in captured.out
        assert "Arsenal" in captured.out
        assert "Chelsea" in captured.out
        assert "Games Played" in captured.out
    
    def test_print_match_preview(self, sample_match_preview, capsys):
        """Test printing match preview."""
        output = ConsoleOutput()
        output.print_match_preview(sample_match_preview)
        
        captured = capsys.readouterr()
        assert "Match Preview" in captured.out
        assert "8.5" in captured.out
        assert "Arsenal" in captured.out
        assert "Cloudy" in captured.out
    
    def test_print_betting_events(self, sample_betting_events, capsys):
        """Test printing betting events."""
        output = ConsoleOutput()
        output.print_betting_events(sample_betting_events)
        
        captured = capsys.readouterr()
        assert "Betting Events" in captured.out
        assert "2 total" in captured.out


class TestSilentOutput:
    """Test cases for SilentOutput."""
    
    def test_print_match_header_does_nothing(self, sample_match_info, capsys):
        """Test that print_match_header does nothing."""
        output = SilentOutput()
        output.print_match_header(sample_match_info)
        
        captured = capsys.readouterr()
        assert captured.out == ""
    
    def test_print_lineup_does_nothing(self, sample_lineup_data, capsys):
        """Test that print_lineup does nothing."""
        output = SilentOutput()
        output.print_lineup(sample_lineup_data, "Arsenal", "Chelsea")
        
        captured = capsys.readouterr()
        assert captured.out == ""
    
    def test_print_head_to_head_does_nothing(self, sample_head_to_head, capsys):
        """Test that print_head_to_head does nothing."""
        output = SilentOutput()
        output.print_head_to_head(sample_head_to_head)
        
        captured = capsys.readouterr()
        assert captured.out == ""
    
    def test_print_match_preview_does_nothing(self, sample_match_preview, capsys):
        """Test that print_match_preview does nothing."""
        output = SilentOutput()
        output.print_match_preview(sample_match_preview)
        
        captured = capsys.readouterr()
        assert captured.out == ""
    
    def test_print_betting_events_does_nothing(self, sample_betting_events, capsys):
        """Test that print_betting_events does nothing."""
        output = SilentOutput()
        output.print_betting_events(sample_betting_events)
        
        captured = capsys.readouterr()
        assert captured.out == ""
