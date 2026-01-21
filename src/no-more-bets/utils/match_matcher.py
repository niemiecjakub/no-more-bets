"""Utility class for matching teams and data across different sources."""
import logging
from typing import Dict, List, Optional, Mapping
from rapidfuzz import process
from models.rotowire import GameLineup
from models.soccerdata import UpcomingMatchPreview, LeagueMatchPreviews
from models.fotmob import Club

logger = logging.getLogger(__name__)


class MatchMatcher:
    """Utility class for matching teams and data across different sources."""
    
    @staticmethod
    def teams_key(home: str, away: str) -> frozenset[str]:
        """Create a normalized key for team matching.
        
        Parameters
        ----------
        home : str
            Home team name.
        away : str
            Away team name.
            
        Returns
        -------
        frozenset[str]
            Normalized key for team matching.
        """
        return frozenset({home.lower().strip(), away.lower().strip()})
    
    @staticmethod
    def build_lineup_index(lineups: List[GameLineup]) -> Dict[frozenset[str], GameLineup]:
        """Build an index of lineups by team names.
        
        Parameters
        ----------
        lineups : List[GameLineup]
            List of game lineups to index.
            
        Returns
        -------
        Dict[frozenset[str], GameLineup]
            Dictionary mapping team keys to lineups.
        """
        return {
            MatchMatcher.teams_key(lineup.home_team.team_name, lineup.away_team.team_name): lineup
            for lineup in lineups
        }
    
    @staticmethod
    def find_lineup(
        match_preview: UpcomingMatchPreview,
        lineup_index: Mapping[frozenset[str], GameLineup]
    ) -> Optional[GameLineup]:
        """Find matching lineup for a match preview.
        
        Parameters
        ----------
        match_preview : UpcomingMatchPreview
            Match preview to find lineup for.
        lineup_index : Mapping[frozenset[str], GameLineup]
            Index of lineups by team keys.
            
        Returns
        -------
        Optional[GameLineup]
            Matching lineup if found, None otherwise.
        """
        key = MatchMatcher.teams_key(
            match_preview.teams.home.name,
            match_preview.teams.away.name
        )
        
        if key in lineup_index:
            return lineup_index[key]
        
        # Fuzzy matching
        search_str = " vs ".join(sorted(key))
        candidates = {" vs ".join(sorted(k)): lineup_index[k] for k in lineup_index.keys()}
        
        result = process.extractOne(search_str, candidates.keys(), score_cutoff=85)
        if result:
            return candidates[result[0]]
        
        return None
    
    @staticmethod
    def find_soccerdata_match(
        home_team_name: str,
        away_team_name: str,
        upcoming_league_matches: List[LeagueMatchPreviews]
    ) -> Optional[UpcomingMatchPreview]:
        """Find matching match from upcoming league matches by team names.
        
        Parameters
        ----------
        home_team_name : str
            Home team name.
        away_team_name : str
            Away team name.
        upcoming_league_matches : List[LeagueMatchPreviews]
            List of league match previews to search.
            
        Returns
        -------
        Optional[UpcomingMatchPreview]
            Matching match preview if found, None otherwise.
        """
        key = MatchMatcher.teams_key(home_team_name, away_team_name)
        
        # Search through all leagues and matches
        for league in upcoming_league_matches:
            for match in league.match_previews:
                match_key = MatchMatcher.teams_key(match.teams.home.name, match.teams.away.name)
                if match_key == key:
                    return match
        
        # Fuzzy matching
        search_str = " vs ".join(sorted(key))
        candidates = {}
        for league in upcoming_league_matches:
            for match in league.match_previews:
                match_key = MatchMatcher.teams_key(match.teams.home.name, match.teams.away.name)
                candidate_str = " vs ".join(sorted(match_key))
                candidates[candidate_str] = match
        
        result = process.extractOne(search_str, candidates.keys(), score_cutoff=85)
        if result:
            return candidates[result[0]]
        
        return None
    
    @staticmethod
    def find_fotmob_club(team_name: str, fotmob_clubs: List[Club]) -> Optional[Club]:
        """Find matching club from FotMob clubs by team name using fuzzy matching.
        
        Parameters
        ----------
        team_name : str
            Team name to search for.
        fotmob_clubs : List[Club]
            List of FotMob clubs to search.
            
        Returns
        -------
        Optional[Club]
            Matching club if found, None otherwise.
        """
        # Normalize team name for better matching
        normalized_search = team_name.lower().strip()
        
        # Exact match first (case-insensitive)
        for club in fotmob_clubs:
            if club.team_name.lower().strip() == normalized_search:
                return club
        
        # Try partial match (if search name is contained in club name or vice versa)
        for club in fotmob_clubs:
            normalized_club = club.team_name.lower().strip()
            if normalized_search in normalized_club or normalized_club in normalized_search:
                return club
        
        # Fuzzy matching with lower threshold for better matching
        candidates = {club.team_name: club for club in fotmob_clubs}
        result = process.extractOne(team_name, candidates.keys(), score_cutoff=70)
        if result:
            logger.debug(f"Fuzzy matched '{team_name}' to '{result[0]}' (score: {result[1]:.1f})")
            return candidates[result[0]]
        
        return None
