"""Tests for MatchMatcher utility class."""
import pytest
from models.rotowire import GameLineup, TeamLineup, PlayerInLineup
from models.soccerdata import UpcomingMatchPreview, Teams, TeamInfo, LeagueMatchPreviews
from models.fbref import Club
from utils.match_matcher import MatchMatcher


@pytest.fixture
def sample_lineups():
    """Create sample lineups for testing."""
    return [
        GameLineup(
            date="2026-01-17",
            time="15:00",
            home_team=TeamLineup(
                team_name="Arsenal",
                lineup_type="Predicted Lineup",
                players=[PlayerInLineup(position="GK", player_name="Player 1")] * 11,
                injuries=[]
            ),
            away_team=TeamLineup(
                team_name="Chelsea",
                lineup_type="Predicted Lineup",
                players=[PlayerInLineup(position="GK", player_name="Player 2")] * 11,
                injuries=[]
            )
        ),
        GameLineup(
            date="2026-01-17",
            time="17:30",
            home_team=TeamLineup(
                team_name="Liverpool",
                lineup_type="Confirmed Lineup",
                players=[PlayerInLineup(position="GK", player_name="Player 3")] * 11,
                injuries=[]
            ),
            away_team=TeamLineup(
                team_name="Manchester United",
                lineup_type="Confirmed Lineup",
                players=[PlayerInLineup(position="GK", player_name="Player 4")] * 11,
                injuries=[]
            )
        ),
    ]


@pytest.fixture
def sample_match_preview():
    """Create sample match preview for testing."""
    return UpcomingMatchPreview(
        id=1,
        date="2026-01-17",
        time="15:00",
        excitement_rating=8.5,
        teams=Teams(
            home=TeamInfo(id=1, name="Arsenal"),
            away=TeamInfo(id=2, name="Chelsea")
        )
    )


@pytest.fixture
def sample_fbref_clubs():
    """Create sample FBref clubs for testing."""
    return [
        Club(team="Arsenal", url="https://fbref.com/arsenal"),
        Club(team="Chelsea", url="https://fbref.com/chelsea"),
        Club(team="Liverpool", url="https://fbref.com/liverpool"),
    ]


class TestMatchMatcher:
    """Test cases for MatchMatcher class."""
    
    def test_teams_key_normalizes_names(self):
        """Test that teams_key normalizes team names."""
        key1 = MatchMatcher.teams_key("Arsenal", "Chelsea")
        key2 = MatchMatcher.teams_key("  ARSENAL  ", "  CHELSEA  ")
        key3 = MatchMatcher.teams_key("Chelsea", "Arsenal")
        
        assert key1 == key2 == key3
    
    def test_build_lineup_index(self, sample_lineups):
        """Test building lineup index."""
        index = MatchMatcher.build_lineup_index(sample_lineups)
        
        assert len(index) == 2
        assert MatchMatcher.teams_key("Arsenal", "Chelsea") in index
        assert MatchMatcher.teams_key("Liverpool", "Manchester United") in index
    
    def test_find_lineup_exact_match(self, sample_lineups, sample_match_preview):
        """Test finding lineup with exact match."""
        index = MatchMatcher.build_lineup_index(sample_lineups)
        lineup = MatchMatcher.find_lineup(sample_match_preview, index)
        
        assert lineup is not None
        assert lineup.home_team.team_name == "Arsenal"
        assert lineup.away_team.team_name == "Chelsea"
    
    def test_find_lineup_no_match(self, sample_lineups):
        """Test finding lineup when no match exists."""
        index = MatchMatcher.build_lineup_index(sample_lineups)
        no_match_preview = UpcomingMatchPreview(
            id=999,
            date="2026-01-17",
            time="20:00",
            excitement_rating=0.0,
            teams=Teams(
                home=TeamInfo(id=999, name="Team A"),
                away=TeamInfo(id=1000, name="Team B")
            )
        )
        
        lineup = MatchMatcher.find_lineup(no_match_preview, index)
        assert lineup is None
    
    def test_find_soccerdata_match_exact(self):
        """Test finding soccerdata match with exact match."""
        league_matches = [
            LeagueMatchPreviews(
                league_id=39,
                league_name="Premier League",
                match_previews=[
                    UpcomingMatchPreview(
                        id=1,
                        date="2026-01-17",
                        time="15:00",
                        excitement_rating=8.5,
                        teams=Teams(
                            home=TeamInfo(id=1, name="Arsenal"),
                            away=TeamInfo(id=2, name="Chelsea")
                        )
                    )
                ]
            )
        ]
        
        match = MatchMatcher.find_soccerdata_match("Arsenal", "Chelsea", league_matches)
        assert match is not None
        assert match.id == 1
    
    def test_find_soccerdata_match_no_match(self):
        """Test finding soccerdata match when no match exists."""
        league_matches = [
            LeagueMatchPreviews(
                league_id=39,
                league_name="Premier League",
                match_previews=[]
            )
        ]
        
        match = MatchMatcher.find_soccerdata_match("Team A", "Team B", league_matches)
        assert match is None
    
    def test_find_fbref_club_exact_match(self, sample_fbref_clubs):
        """Test finding FBref club with exact match."""
        club = MatchMatcher.find_fbref_club("Arsenal", sample_fbref_clubs)
        assert club is not None
        assert club.team == "Arsenal"
    
    def test_find_fbref_club_case_insensitive(self, sample_fbref_clubs):
        """Test finding FBref club is case-insensitive."""
        club = MatchMatcher.find_fbref_club("arsenal", sample_fbref_clubs)
        assert club is not None
        assert club.team == "Arsenal"
    
    def test_find_fbref_club_no_match(self, sample_fbref_clubs):
        """Test finding FBref club when no match exists."""
        club = MatchMatcher.find_fbref_club("NonExistent Team", sample_fbref_clubs)
        assert club is None
