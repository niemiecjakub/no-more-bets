from bs4 import BeautifulSoup
from models import Club, Player, Game
from .base_scraper import BaseScraper

class FBref(BaseScraper):
    """FBref scraper class for fetching and parsing data from fbref.com."""
    
    def __init__(
        self,
        impersonate: str = "chrome110",
        delay: float = 5.0,
        retry_count: int = 3,
        retry_delay: float = 2.0,
        timeout: float = 15.0,
        store: bool = True,
        use_cache: bool = True,
    ):
        """Initialize FBref scraper.
        
        Parameters
        ----------
        impersonate : str
            Browser impersonation string for curl_cffi. Default is "chrome110".
        delay : float
            Minimum delay in seconds between page fetches. Default is 5.0.
        retry_count : int
            Number of retry attempts if request fails. Default is 3.
        retry_delay : float
            Delay in seconds between retry attempts. Default is 2.0.
        timeout : float
            Request timeout in seconds. Default is 15.0.
        store : bool
            Whether to save fetched HTML to cache folder. Default is True.
        use_cache : bool
            Whether to use cached HTML if available. Default is True.
        """
        super().__init__(impersonate, delay, retry_count, retry_delay, timeout, store, use_cache)
        self.base_url = "https://fbref.com"
     
    def get_premier_league_stats(self) -> list[Club]:
        """Get Premier League statistics as a list of Club objects.
        
        Fetches the Premier League stats page and parses the tbody from
        selector '#results2025-202691_overall > tbody' to extract club data.
        
        Returns
        -------
        list[Club]
            List of Club objects containing statistics for each team.
        """
        selector = '#results2025-202691_overall > tbody'

        url = f"{self.base_url}/en/comps/9/Premier-League-Stats"
        html_content = self._get_page_html(url)     
        
        soup = BeautifulSoup(html_content, 'lxml')
        tbody = soup.select_one(selector)
        
        if tbody is None:
            raise ValueError(f"Selector '{selector}' not found in the page")
        
        clubs = []
        rows = tbody.find_all('tr')
        
        for row in rows:
            # Extract rank
            rank_elem = row.find('th', {'data-stat': 'rank'})
            rank = int(rank_elem.text.strip()) if rank_elem else 0
            
            # Extract team name from link
            team_elem = row.find('td', {'data-stat': 'team'})
            team_link = team_elem.find('a') if team_elem else None
            team = team_link.text.strip() if team_link else ""
            
            # Helper function to extract numeric value from td
            def get_int_value(stat_name: str) -> int:
                elem = row.find('td', {'data-stat': stat_name})
                if elem:
                    text = elem.text.strip()
                    # Remove commas and convert
                    text = text.replace(',', '')
                    try:
                        return int(text)
                    except ValueError:
                        return 0
                return 0
            
            # Helper function to extract float value from td
            def get_float_value(stat_name: str) -> float:
                elem = row.find('td', {'data-stat': stat_name})
                if elem:
                    text = elem.text.strip()
                    try:
                        return float(text)
                    except ValueError:
                        return 0.0
                return 0.0
            
            # Helper function to extract string value from td
            def get_str_value(stat_name: str) -> str:
                elem = row.find('td', {'data-stat': stat_name})
                if elem:
                    return elem.text.strip()
                return ""
            
            # Extract all statistics
            games = get_int_value('games')
            wins = get_int_value('wins')
            ties = get_int_value('ties')
            losses = get_int_value('losses')
            goals_for = get_int_value('goals_for')
            goals_against = get_int_value('goals_against')
            goal_diff = get_str_value('goal_diff')
            points = get_int_value('points')
            points_avg = get_float_value('points_avg')
            xg_for = get_float_value('xg_for')
            xg_against = get_float_value('xg_against')
            xg_diff = get_float_value('xg_diff')
            xg_diff_per90 = get_float_value('xg_diff_per90')
            
            # Extract last_5 (form) - get the text content
            last_5_elem = row.find('td', {'data-stat': 'last_5'})
            last_5 = ""
            if last_5_elem:
                # Extract the form letters (W, D, L) from the divs
                form_divs = last_5_elem.find_all('div', class_='poptip')
                last_5 = ''.join([div.find('a').text.strip() if div.find('a') else '' for div in form_divs])
            
            attendance_per_g = get_str_value('attendance_per_g')
            top_team_scorers = get_str_value('top_team_scorers')
            top_keeper = get_str_value('top_keeper')
            notes = get_str_value('notes') or None
            
            club = Club(
                rank=rank,
                team=team,
                games=games,
                wins=wins,
                ties=ties,
                losses=losses,
                goals_for=goals_for,
                goals_against=goals_against,
                goal_diff=goal_diff,
                points=points,
                points_avg=points_avg,
                xg_for=xg_for,
                xg_against=xg_against,
                xg_diff=xg_diff,
                xg_diff_per90=xg_diff_per90,
                last_5=last_5,
                attendance_per_g=attendance_per_g,
                top_team_scorers=top_team_scorers,
                top_keeper=top_keeper,
                notes=notes
            )
            
            clubs.append(club)
        
        return clubs
    
    def get_club_players(self, club_name: str) -> list[Player]:
        """Get player statistics for a specific club.
        
        Finds the club in the Premier League table, navigates to its details page,
        and scrapes player statistics from the standard stats table.
        
        Parameters
        ----------
        club_name : str
            Name of the club to get players for.
            
        Returns
        -------
        list[Player]
            List of Player objects containing statistics for each player.
        """
        # First, get the Premier League stats to find the club link
        url = f"{self.base_url}/en/comps/9/Premier-League-Stats"
        league_stats_html = self._get_page_html(url)
        soup = BeautifulSoup(league_stats_html, 'lxml')
        tbody = soup.select_one('#results2025-202691_overall > tbody')
        
        if tbody is None:
            raise ValueError("League table not found in the page")
        
        # Find the club row by matching the team name
        club_link = None
        rows = tbody.find_all('tr')
        
        for row in rows:
            team_elem = row.find('td', {'data-stat': 'team'})
            if team_elem:
                team_link = team_elem.find('a')
                if team_link and club_name.lower() in team_link.text.strip().lower():
                    club_link = team_link.get('href')
                    break
        
        if club_link is None:
            raise ValueError(f"Club '{club_name}' not found in the Premier League table")
        
        # Navigate to the club details page
        club_url = f"{self.base_url}{club_link}"
        club_response_html = self._get_page_html(club_url)

        # Parse the player statistics table
        club_soup = BeautifulSoup(club_response_html, 'lxml')
        player_tbody = club_soup.select_one('#stats_standard_9 > tbody')
        
        if player_tbody is None:
            raise ValueError("Player statistics table not found on the club page")
        
        players = []
        player_rows = player_tbody.find_all('tr')
        
        for row in player_rows:
            # Extract player name from th
            player_elem = row.find('th', {'data-stat': 'player'})
            player_link = player_elem.find('a') if player_elem else None
            player_name = player_link.text.strip() if player_link else ""
            
            if not player_name:
                continue
            
            # Helper function to extract value from td
            def get_value(stat_name: str, value_type: type = str, default=None):
                elem = row.find('td', {'data-stat': stat_name})
                if elem:
                    # Check if element has "iz" class (indicates zero/empty)
                    if 'iz' in elem.get('class', []):
                        return default if default is not None else (0 if value_type == int else 0.0 if value_type == float else "")
                    
                    text = elem.text.strip()
                    if not text or text == '':
                        return default
                    if value_type == int:
                        # Remove commas and convert
                        text = text.replace(',', '')
                        try:
                            return int(text)
                        except ValueError:
                            return default if default is not None else 0
                    elif value_type == float:
                        try:
                            return float(text)
                        except ValueError:
                            return default if default is not None else 0.0
                    return text
                return default
            
            # Extract nationality (from the link text or span)
            nationality_elem = row.find('td', {'data-stat': 'nationality'})
            nationality = ""
            if nationality_elem:
                # Try to get the country code from the span
                span = nationality_elem.find('span')
                if span:
                    # Extract country code (e.g., "NOR" from "no NOR")
                    text = span.text.strip()
                    parts = text.split()
                    if len(parts) > 1:
                        nationality = parts[-1]  # Get the last part (country code)
                    else:
                        nationality = text
            
            # Extract all statistics
            position = get_value('position', str, "")
            age = get_value('age', str, "")
            games = get_value('games', int, 0)
            games_starts = get_value('games_starts', int, 0)
            
            # Handle minutes - can be empty string or have commas
            minutes_elem = row.find('td', {'data-stat': 'minutes'})
            minutes = 0
            if minutes_elem and 'iz' not in minutes_elem.get('class', []):
                minutes_text = minutes_elem.text.strip().replace(',', '')
                if minutes_text:
                    try:
                        minutes = int(minutes_text)
                    except ValueError:
                        minutes = 0
            
            minutes_90s = get_value('minutes_90s', float, 0.0)
            goals = get_value('goals', int, 0)
            assists = get_value('assists', int, 0)
            goals_assists = get_value('goals_assists', int, 0)
            goals_pens = get_value('goals_pens', int, 0)
            pens_made = get_value('pens_made', int, 0)
            pens_att = get_value('pens_att', int, 0)
            cards_yellow = get_value('cards_yellow', int, 0)
            cards_red = get_value('cards_red', int, 0)
            xg = get_value('xg', float, 0.0)
            npxg = get_value('npxg', float, 0.0)
            xg_assist = get_value('xg_assist', float, 0.0)
            npxg_xg_assist = get_value('npxg_xg_assist', float, 0.0)
            progressive_carries = get_value('progressive_carries', int, 0)
            progressive_passes = get_value('progressive_passes', int, 0)
            progressive_passes_received = get_value('progressive_passes_received', int, 0)
            goals_per90 = get_value('goals_per90', float, 0.0)
            assists_per90 = get_value('assists_per90', float, 0.0)
            goals_assists_per90 = get_value('goals_assists_per90', float, 0.0)
            goals_pens_per90 = get_value('goals_pens_per90', float, 0.0)
            goals_assists_pens_per90 = get_value('goals_assists_pens_per90', float, 0.0)
            xg_per90 = get_value('xg_per90', float, 0.0)
            xg_assist_per90 = get_value('xg_assist_per90', float, 0.0)
            xg_xg_assist_per90 = get_value('xg_xg_assist_per90', float, 0.0)
            npxg_per90 = get_value('npxg_per90', float, 0.0)
            npxg_xg_assist_per90 = get_value('npxg_xg_assist_per90', float, 0.0)
            
            player = Player(
                player=player_name,
                nationality=nationality,
                position=position,
                age=age,
                games=games,
                games_starts=games_starts,
                minutes=minutes,
                minutes_90s=minutes_90s,
                goals=goals,
                assists=assists,
                goals_assists=goals_assists,
                goals_pens=goals_pens,
                pens_made=pens_made,
                pens_att=pens_att,
                cards_yellow=cards_yellow,
                cards_red=cards_red,
                xg=xg,
                npxg=npxg,
                xg_assist=xg_assist,
                npxg_xg_assist=npxg_xg_assist,
                progressive_carries=progressive_carries,
                progressive_passes=progressive_passes,
                progressive_passes_received=progressive_passes_received,
                goals_per90=goals_per90,
                assists_per90=assists_per90,
                goals_assists_per90=goals_assists_per90,
                goals_pens_per90=goals_pens_per90,
                goals_assists_pens_per90=goals_assists_pens_per90,
                xg_per90=xg_per90,
                xg_assist_per90=xg_assist_per90,
                xg_xg_assist_per90=xg_xg_assist_per90,
                npxg_per90=npxg_per90,
                npxg_xg_assist_per90=npxg_xg_assist_per90
            )
            
            players.append(player)
        
        return players

    def get_club_games(self, club_name: str, epl_only: bool = False) -> list[Game]:
        """Get game/match statistics for a specific club.
        
        Finds the club in the Premier League table, navigates to its details page,
        and scrapes game statistics from the match logs table.
        
        Parameters
        ----------
        club_name : str
            Name of the club to get games for.
        epl_only : bool, optional
            If True, only return Premier League games. Default is False.
            
        Returns
        -------
        list[Game]
            List of Game objects containing statistics for each match.
        """
        # First, get the Premier League stats to find the club link
        url = f"{self.base_url}/en/comps/9/Premier-League-Stats"
        league_stats_html = self._get_page_html(url)
        soup = BeautifulSoup(league_stats_html, 'lxml')
        tbody = soup.select_one('#results2025-202691_overall > tbody')
        
        if tbody is None:
            raise ValueError("League table not found in the page")
        
        # Find the club row by matching the team name
        club_link = None
        rows = tbody.find_all('tr')
        
        for row in rows:
            team_elem = row.find('td', {'data-stat': 'team'})
            if team_elem:
                team_link = team_elem.find('a')
                if team_link and club_name.lower() in team_link.text.strip().lower():
                    club_link = team_link.get('href')
                    break
        
        if club_link is None:
            raise ValueError(f"Club '{club_name}' not found in the Premier League table")
        
        # Navigate to the club details page
        club_url = f"{self.base_url}{club_link}"
        club_response_html = self._get_page_html(club_url)
   
        # Parse the game statistics table
        club_soup = BeautifulSoup(club_response_html, 'lxml')
        game_tbody = club_soup.select_one('#matchlogs_for > tbody')
        
        if game_tbody is None:
            raise ValueError("Game statistics table not found on the club page")
        
        games = []
        game_rows = game_tbody.find_all('tr')
        
        for row in game_rows:
            # Extract date from th
            date_elem = row.find('th', {'data-stat': 'date'})
            date = ""
            if date_elem:
                date_link = date_elem.find('a')
                if date_link:
                    date = date_link.text.strip()
                else:
                    date = date_elem.text.strip()
            
            if not date:
                continue
            
            # Helper function to extract value from td
            def get_value(stat_name: str, value_type: type = str, default=None):
                elem = row.find('td', {'data-stat': stat_name})
                if elem:
                    # Check if element has "iz" class (indicates zero/empty)
                    if 'iz' in elem.get('class', []):
                        return default
                    
                    text = elem.text.strip()
                    if not text or text == '':
                        return default
                    
                    if value_type == int:
                        # Remove commas and convert
                        text = text.replace(',', '')
                        try:
                            return int(text)
                        except ValueError:
                            return default
                    elif value_type == float:
                        try:
                            return float(text)
                        except ValueError:
                            return default
                    return text
                return default
            
            # Extract start_time - get the venue time
            start_time_elem = row.find('td', {'data-stat': 'start_time'})
            start_time = ""
            if start_time_elem:
                venue_time_span = start_time_elem.find('span', class_='venuetime')
                if venue_time_span:
                    start_time = venue_time_span.text.strip()
                else:
                    start_time = start_time_elem.text.strip()
            
            # Extract competition
            comp_elem = row.find('td', {'data-stat': 'comp'})
            comp = ""
            if comp_elem:
                comp_link = comp_elem.find('a')
                if comp_link:
                    comp = comp_link.text.strip()
                else:
                    comp = comp_elem.text.strip()
            
            # Filter by Premier League if epl_only is True
            if epl_only and comp != "Premier League":
                continue
            
            # Extract round
            round_elem = row.find('td', {'data-stat': 'round'})
            round_str = ""
            if round_elem:
                round_link = round_elem.find('a')
                if round_link:
                    round_str = round_link.text.strip()
                else:
                    round_str = round_elem.text.strip()
            
            # Extract opponent
            opponent_elem = row.find('td', {'data-stat': 'opponent'})
            opponent = ""
            if opponent_elem:
                opponent_link = opponent_elem.find('a')
                if opponent_link:
                    opponent = opponent_link.text.strip()
                else:
                    opponent = opponent_elem.text.strip()
            
            # Extract captain
            captain_elem = row.find('td', {'data-stat': 'captain'})
            captain = None
            if captain_elem:
                captain_link = captain_elem.find('a')
                if captain_link:
                    captain = captain_link.text.strip()
            
            # Extract all other statistics
            dayofweek = get_value('dayofweek', str, "")
            venue = get_value('venue', str, "")
            result = get_value('result', str, None)
            goals_for = get_value('goals_for', int, None)
            goals_against = get_value('goals_against', int, None)
            xg_for = get_value('xg_for', float, None)
            xg_against = get_value('xg_against', float, None)
            possession = get_value('possession', int, None)
            attendance = get_value('attendance', int, None)
            formation = get_value('formation', str, None)
            opp_formation = get_value('opp_formation', str, None)
            referee = get_value('referee', str, None)
            notes = get_value('notes', str, None)
            
            game = Game(
                date=date,
                start_time=start_time,
                comp=comp,
                round=round_str,
                dayofweek=dayofweek,
                venue=venue,
                result=result,
                goals_for=goals_for,
                goals_against=goals_against,
                opponent=opponent,
                xg_for=xg_for,
                xg_against=xg_against,
                possession=possession,
                attendance=attendance,
                captain=captain,
                formation=formation,
                opp_formation=opp_formation,
                referee=referee,
                notes=notes
            )
            
            games.append(game)
        
        return games
