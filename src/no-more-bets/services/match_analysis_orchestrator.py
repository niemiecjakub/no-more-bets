"""Orchestrator for collecting and analyzing match data from multiple sources."""
import logging
from typing import Dict, List, Optional, Tuple
from models.betclic import UpcomingGame
from models.rotowire import GameLineup
from models.soccerdata import UpcomingMatchPreview, Teams, TeamInfo, LeagueMatchPreviews
from models.fotmob import Club
from models.match_analysis import (
    MatchAnalysis,
    MatchInfo,
    LineupData,
    TeamLineupData,
    HeadToHeadData,
    MatchPreviewData,
    PredictionData,
    WeatherData,
    FBrefTeamData,
)
from services.rotowire import Rotowire
from services.soccerdata import SoccerData
from services.betclic import Betclic
from services.fotmob import FotMob
from utils.match_matcher import MatchMatcher
from output.match_analysis_output import MatchAnalysisOutput, ConsoleOutput
from output.match_analysis_persistence import MatchAnalysisPersistence

logger = logging.getLogger(__name__)


class MatchAnalysisOrchestrator:
    """Orchestrates match data collection from multiple sources."""
    
    def __init__(
        self,
        rotowire: Rotowire,
        soccerdata: SoccerData,
        bookmaker: Betclic,
        fotmob: FotMob,
        league_id: int,
        output_handler: Optional[MatchAnalysisOutput] = None,
        persistence: Optional[MatchAnalysisPersistence] = None,
    ):
        """Initialize the match analysis orchestrator.
        
        Parameters
        ----------
        rotowire : Rotowire
            Rotowire service for lineup data.
        soccerdata : SoccerData
            SoccerData service for match previews and head-to-head data.
        bookmaker : Betclic
            Betclic service for upcoming matches and betting events.
        fotmob : FotMob
            FotMob service for team statistics.
        league_id : int
            League ID for fetching upcoming matches.
        output_handler : Optional[MatchAnalysisOutput]
            Output handler for printing results. Defaults to ConsoleOutput.
        persistence : Optional[MatchAnalysisPersistence]
            Persistence handler for saving results. Defaults to file-based persistence.
        """
        self.rotowire = rotowire
        self.soccerdata = soccerdata
        self.bookmaker = bookmaker
        self.fotmob = fotmob
        self.league_id = league_id
        self.output_handler = output_handler or ConsoleOutput()
        self.persistence = persistence or MatchAnalysisPersistence()
        self.matcher = MatchMatcher()
    
    def analyze_matches(self) -> List[MatchAnalysis]:
        """Analyze all upcoming matches and return results.
        
        Returns
        -------
        List[MatchAnalysis]
            List of match analysis results.
        """
        # Fetch initial data
        lineup_index, upcoming_league_matches, fotmob_clubs = self._fetch_initial_data()
        
        # Get bookmaker matches
        bookmaker_matches = self.bookmaker.get_upcoming_games()
        
        results: List[MatchAnalysis] = []
        
        # Process each match
        for match in bookmaker_matches:
            match_analysis = self._collect_match_data(
                match=match,
                lineup_index=lineup_index,
                upcoming_league_matches=upcoming_league_matches,
                fotmob_clubs=fotmob_clubs,
            )
            results.append(match_analysis)
        
        # Save results
        self.persistence.save_results(results)
        
        return results
    
    def _fetch_initial_data(
        self
    ) -> Tuple[Dict[frozenset[str], GameLineup], List[LeagueMatchPreviews], List[Club]]:
        """Fetch initial data needed for all matches.
        
        Returns
        -------
        Tuple[Dict[frozenset[str], GameLineup], List[LeagueMatchPreviews], List[Club]]
            Tuple containing lineup index, upcoming league matches, and FotMob clubs.
        """
        # Fetch lineups
        lineups = self.rotowire.get_soccer_lineups()
        lineup_index = self.matcher.build_lineup_index(lineups)
        
        # Fetch upcoming league matches
        upcoming_league_matches = self.soccerdata.get_match_previews_upcoming(league_id=self.league_id)
        
        # Fetch FotMob clubs
        try:
            fotmob_clubs = self.fotmob.get_premier_league_table()
            logger.info(f"Loaded {len(fotmob_clubs)} FotMob clubs")
        except Exception as e:
            logger.error(f"Failed to load FotMob Premier League table: {e}")
            fotmob_clubs = []
        
        return lineup_index, upcoming_league_matches, fotmob_clubs
    
    def _collect_match_data(
        self,
        match: UpcomingGame,
        lineup_index: Dict[frozenset[str], GameLineup],
        upcoming_league_matches: List[LeagueMatchPreviews],
        fotmob_clubs: List[Club],
    ) -> MatchAnalysis:
        """Collect all data for a single match.
        
        Parameters
        ----------
        match : UpcomingGame
            Bookmaker match information.
        lineup_index : Dict[frozenset[str], GameLineup]
            Index of lineups by team keys.
        upcoming_league_matches : List[LeagueMatchPreviews]
            List of upcoming league matches.
        fotmob_clubs : List[Club]
            List of FotMob clubs.
            
        Returns
        -------
        MatchAnalysis
            Complete match analysis.
        """
        home_team_name = match.home_team
        away_team_name = match.away_team
        
        # Create match info
        match_info = MatchInfo(
            home=match.home_team,
            away=match.away_team,
            date=match.date,
            time=match.time
        )
        
        # Print match header
        self.output_handler.print_match_header(match_info)
        
        # Get lineup data
        lineup_data = self._get_lineup_data(
            home_team_name=home_team_name,
            away_team_name=away_team_name,
            match_date=match.date,
            match_time=match.time,
            lineup_index=lineup_index,
        )
        
        # Get head-to-head and match preview data
        soccerdata_match = self.matcher.find_soccerdata_match(
            home_team_name, away_team_name, upcoming_league_matches
        )
        
        head_to_head_data = None
        match_preview_data = None
        
        if soccerdata_match:
            head_to_head_data = self._get_head_to_head_data(soccerdata_match)
            match_preview_data = self._get_match_preview_data(soccerdata_match)
        
        # Get betting events
        events = self.bookmaker.get_match_events(match.url, expand=True)
        betting_events_data = events if events else None
        if betting_events_data:
            self.output_handler.print_betting_events(betting_events_data)
        
        # Get FotMob data
        fotmob_home_data, fotmob_away_data = self._get_fotmob_data(
            home_team_name=home_team_name,
            away_team_name=away_team_name,
            fotmob_clubs=fotmob_clubs,
        )
        
        self.output_handler.print_empty_line()
        
        # Build match analysis
        match_analysis = MatchAnalysis(
            match_info=match_info,
            lineup=lineup_data,
            head_to_head=head_to_head_data,
            match_preview=match_preview_data,
            betting_events=betting_events_data,
            fbref_home=fotmob_home_data,
            fbref_away=fotmob_away_data
        )
        
        return match_analysis
    
    def _get_lineup_data(
        self,
        home_team_name: str,
        away_team_name: str,
        match_date: str,
        match_time: str,
        lineup_index: Dict[frozenset[str], GameLineup],
    ) -> Optional[LineupData]:
        """Get lineup data for a match.
        
        Parameters
        ----------
        home_team_name : str
            Home team name.
        away_team_name : str
            Away team name.
        match_date : str
            Match date.
        match_time : str
            Match time.
        lineup_index : Dict[frozenset[str], GameLineup]
            Index of lineups by team keys.
            
        Returns
        -------
        Optional[LineupData]
            Lineup data if found, None otherwise.
        """
        # Create temporary match preview for matching
        temp_match = UpcomingMatchPreview(
            id=0,
            date=match_date,
            time=match_time,
            excitement_rating=0.0,
            teams=Teams(
                home=TeamInfo(id=0, name=home_team_name),
                away=TeamInfo(id=0, name=away_team_name)
            )
        )
        
        lineup = self.matcher.find_lineup(temp_match, lineup_index)
        
        if lineup:
            # Validate lineup
            home_count = len(lineup.home_team.players)
            away_count = len(lineup.away_team.players)
            
            if home_count != 11:
                logger.error(f"{home_team_name} lineup has {home_count} players (expected 11)")
            if away_count != 11:
                logger.error(f"{away_team_name} lineup has {away_count} players (expected 11)")
            
            # Print lineup
            self.output_handler.print_lineup(
                LineupData(
                    home=TeamLineupData(
                        team_name=lineup.home_team.team_name,
                        lineup_type=lineup.home_team.lineup_type,
                        players=lineup.home_team.players,
                        injuries=lineup.home_team.injuries
                    ),
                    away=TeamLineupData(
                        team_name=lineup.away_team.team_name,
                        lineup_type=lineup.away_team.lineup_type,
                        players=lineup.away_team.players,
                        injuries=lineup.away_team.injuries
                    )
                ),
                home_team_name=home_team_name,
                away_team_name=away_team_name,
            )
            
            return LineupData(
                home=TeamLineupData(
                    team_name=lineup.home_team.team_name,
                    lineup_type=lineup.home_team.lineup_type,
                    players=lineup.home_team.players,
                    injuries=lineup.home_team.injuries
                ),
                away=TeamLineupData(
                    team_name=lineup.away_team.team_name,
                    lineup_type=lineup.away_team.lineup_type,
                    players=lineup.away_team.players,
                    injuries=lineup.away_team.injuries
                )
            )
        else:
            logger.error(f"No matching lineup found for {home_team_name} vs {away_team_name}")
            return None
    
    def _get_head_to_head_data(
        self,
        soccerdata_match: UpcomingMatchPreview,
    ) -> Optional[HeadToHeadData]:
        """Get head-to-head data for a match.
        
        Parameters
        ----------
        soccerdata_match : UpcomingMatchPreview
            SoccerData match preview.
            
        Returns
        -------
        Optional[HeadToHeadData]
            Head-to-head data if found, None otherwise.
        """
        head_to_head = self.soccerdata.get_head_to_head(
            soccerdata_match.teams.home.id,
            soccerdata_match.teams.away.id
        )
        
        if head_to_head:
            self.output_handler.print_head_to_head(
                HeadToHeadData(
                    team1=head_to_head.team1,
                    team2=head_to_head.team2,
                    overall=head_to_head.stats.overall,
                    team1_at_home=head_to_head.stats.team1_at_home,
                    team2_at_home=head_to_head.stats.team2_at_home
                )
            )
            
            return HeadToHeadData(
                team1=head_to_head.team1,
                team2=head_to_head.team2,
                overall=head_to_head.stats.overall,
                team1_at_home=head_to_head.stats.team1_at_home,
                team2_at_home=head_to_head.stats.team2_at_home
            )
        
        return None
    
    def _get_match_preview_data(
        self,
        soccerdata_match: UpcomingMatchPreview,
    ) -> Optional[MatchPreviewData]:
        """Get match preview data.
        
        Parameters
        ----------
        soccerdata_match : UpcomingMatchPreview
            SoccerData match preview.
            
        Returns
        -------
        Optional[MatchPreviewData]
            Match preview data if found, None otherwise.
        """
        match_preview = self.soccerdata.get_match_preview(soccerdata_match.id)
        
        if match_preview:
            # Get team name based on prediction choice
            prediction_choice = match_preview.match_data.prediction.choice
            if prediction_choice == "home":
                team_name = match_preview.teams.home.name
            elif prediction_choice == "away":
                team_name = match_preview.teams.away.name
            else:
                team_name = prediction_choice  # For "draw" or other values
            
            match_preview_data = MatchPreviewData(
                excitement_rating=match_preview.match_data.excitement_rating,
                prediction=PredictionData(
                    type=match_preview.match_data.prediction.type,
                    choice=match_preview.match_data.prediction.choice,
                    team_name=team_name
                ),
                weather=WeatherData(
                    description=match_preview.match_data.weather.description,
                    temp_c=match_preview.match_data.weather.temp_c,
                    temp_f=match_preview.match_data.weather.temp_f
                ),
                preview_content=match_preview.preview_content
            )
            
            self.output_handler.print_match_preview(match_preview_data)
            
            return match_preview_data
        
        return None
    
    def _get_fotmob_data(
        self,
        home_team_name: str,
        away_team_name: str,
        fotmob_clubs: List[Club],
    ) -> Tuple[Optional[FBrefTeamData], Optional[FBrefTeamData]]:
        """Get FotMob data for both teams.
        
        Parameters
        ----------
        home_team_name : str
            Home team name.
        away_team_name : str
            Away team name.
        fotmob_clubs : List[Club]
            List of FotMob clubs.
            
        Returns
        -------
        Tuple[Optional[FBrefTeamData], Optional[FBrefTeamData]]
            Tuple of (home team FotMob data, away team FotMob data).
        """
        fotmob_home_data = None
        fotmob_away_data = None
        
        if not fotmob_clubs:
            return fotmob_home_data, fotmob_away_data
        
        # Find home team club stats
        home_club = self.matcher.find_fotmob_club(home_team_name, fotmob_clubs)
        if home_club:
            # FotMob does not support recent games data - set to empty list with warning
            logger.warning(f"FotMob does not support recent games data. Setting recent_games to empty list for {home_team_name}")
            fotmob_home_data = FBrefTeamData(
                club_stats=home_club,
                recent_games=[]
            )
            logger.info(f"FotMob data loaded for {home_team_name}")
        else:
            logger.warning(f"FotMob club not found for {home_team_name}")
        
        # Find away team club stats
        away_club = self.matcher.find_fotmob_club(away_team_name, fotmob_clubs)
        if away_club:
            # FotMob does not support recent games data - set to empty list with warning
            logger.warning(f"FotMob does not support recent games data. Setting recent_games to empty list for {away_team_name}")
            fotmob_away_data = FBrefTeamData(
                club_stats=away_club,
                recent_games=[]
            )
            logger.info(f"FotMob data loaded for {away_team_name}")
        else:
            logger.warning(f"FotMob club not found for {away_team_name}")
        
        return fotmob_home_data, fotmob_away_data
