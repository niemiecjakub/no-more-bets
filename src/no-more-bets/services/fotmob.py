from bs4 import BeautifulSoup
import re
from models.fotmob import Club, XgStats
from .base_scraper import BaseScraper


class FotMob(BaseScraper):
    """FotMob scraper class for fetching and parsing data from fotmob.com."""
    
    def __init__(
        self,
        impersonate: str = "chrome110",
        delay: float = 5.0,
        retry_count: int = 3,
        retry_delay: float = 2.0,
        timeout: float = 15.0,
        store: bool = True,
        use_cache: bool = True,
        cache_ttl: float = 3600.0,
    ):
        """Initialize FotMob scraper.
        
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
        cache_ttl : float
            Cache time-to-live in seconds. Default is 3600.0 (1 hour).
        """
        super().__init__(impersonate, delay, retry_count, retry_delay, timeout, store, use_cache, cache_ttl)
        self.base_url = "https://www.fotmob.com/en"
    
    def get_premier_league_table(self) -> list[Club]:
        """Get Premier League table as a list of Club objects.
        
        Fetches the Premier League table page and parses the table to extract
        club data including position, team info, statistics, form, and next opponent.
        
        Returns
        -------
        list[Club]
            List of Club objects containing table data for each team.
        """
        url = f"{self.base_url}/leagues/47/table/premier-league"
        html_content = self._get_page_html_selenium(url)
        
        soup = BeautifulSoup(html_content, 'lxml')
        
        # Find the table container
        table_container = soup.find('article', class_='TableContainer')
        if table_container is None:
            raise ValueError("Table container not found in the page")
        
        # Find all table rows - they are in divs with class containing "TableRowCSS"
        # The rows are inside divs with class "flipmove"
        rows = table_container.find_all('div', class_=lambda x: x and 'TableRowCSS' in x)
        
        clubs = []
        
        for row in rows:
            # Extract position
            position_cell = row.find('div', class_=lambda x: x and 'TablePositionCell' in x)
            if not position_cell:
                continue
            
            try:
                position = int(position_cell.get_text(strip=True))
            except ValueError:
                continue
            
            # Extract team info
            team_cell = row.find('div', class_=lambda x: x and 'TableTeamCell' in x)
            if not team_cell:
                continue
            
            team_link = team_cell.find('a', class_=lambda x: x and 'TeamLink' in x)
            if not team_link:
                continue
            
            # Extract team ID from href (e.g., "/pl/teams/9825/overview/arsenal" -> 9825)
            href = team_link.get('href', '')
            team_id_match = re.search(r'/teams/(\d+)/', href)
            team_id = int(team_id_match.group(1)) if team_id_match else 0
            
            # Extract team logo URL
            team_img = team_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
            team_logo_url = team_img.get('src', '') if team_img else ''
            
            # Extract team name and shortname
            team_name_elem = team_link.find('span', class_=lambda x: x and 'TeamName' in x)
            team_name = team_name_elem.get_text(strip=True) if team_name_elem else ''
            
            team_shortname_elem = team_link.find('span', class_=lambda x: x and 'TeamShortname' in x)
            team_shortname = team_shortname_elem.get_text(strip=True) if team_shortname_elem else ''
            
            # Extract statistics - find all direct child divs of the row
            # They appear in order: Position, Team, M, Z, R, P, +/-, =, PKT., Form, Next Opponent
            all_cells = row.find_all('div', recursive=False)
            
            if len(all_cells) < 11:
                continue
            
            # Extract stats in order (skip first 2: Position and Team)
            # Index 2: M (matches played)
            # Index 3: Z (wins)
            # Index 4: R (draws)
            # Index 5: P (losses)
            # Index 6: +/- (goals for/against)
            # Index 7: = (goal difference)
            # Index 8: PKT. (points)
            # Index 9: Form
            # Index 10: Next opponent
            
            matches_played = self._extract_int(all_cells[2])
            wins = self._extract_int(all_cells[3])
            draws = self._extract_int(all_cells[4])
            losses = self._extract_int(all_cells[5])
            
            # Goals for/against from +/- column (format: "40-14")
            goals_cell = all_cells[6]
            goals_text = goals_cell.get_text(strip=True)
            goals_match = re.search(r'(\d+)\s*-\s*(\d+)', goals_text)
            if goals_match:
                goals_for = int(goals_match.group(1))
                goals_against = int(goals_match.group(2))
            else:
                goals_for = 0
                goals_against = 0
            
            # Goal difference from = column (format: "+26" or "-5")
            goal_diff_cell = all_cells[7]
            goal_difference = goal_diff_cell.get_text(strip=True)
            
            # Points
            points = self._extract_int(all_cells[8])
            
            # Extract form from cell at index 9
            form_cell = all_cells[9]
            form_section = form_cell.find('section', class_=lambda x: x and 'SingleTeamForm' in x)
            form = ''
            if form_section:
                form_items = form_section.find_all('a', class_=lambda x: x and 'ResultBox' in x)
                form_chars = []
                for item in form_items:
                    classes = item.get('class', [])
                    if 'team-form__win' in str(classes):
                        form_chars.append('W')
                    elif 'team-form__draw' in str(classes):
                        form_chars.append('D')
                    elif 'team-form__loss' in str(classes):
                        form_chars.append('L')
                form = ''.join(form_chars)
            
            # Extract next opponent from cell at index 10
            next_opponent_cell = all_cells[10]
            next_opponent_link = next_opponent_cell.find('a', class_=lambda x: x and 'NextOpponentCSS' in x)
            next_opponent_id = None
            next_opponent_name = None
            next_opponent_logo_url = None
            
            if next_opponent_link:
                # Extract opponent logo and ID from image src
                opp_img = next_opponent_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
                if opp_img:
                    next_opponent_logo_url = opp_img.get('src', '')
                    # Extract team ID from logo URL (e.g., "teamlogo/10260.png" -> 10260)
                    logo_id_match = re.search(r'teamlogo/(\d+)', next_opponent_logo_url)
                    if logo_id_match:
                        next_opponent_id = int(logo_id_match.group(1))
                
                # Try to extract opponent name from match URL
                # Format: "/pl/matches/arsenal-vs-manchester-united/3c3mu0#4813596"
                opp_href = next_opponent_link.get('href', '')
                if opp_href:
                    # Extract the second team name from the match URL
                    match_url_match = re.search(r'/matches/([^/]+)-vs-([^/]+)/', opp_href)
                    if match_url_match:
                        # The second team is the opponent (assuming current team is first)
                        opponent_slug = match_url_match.group(2)
                        # Convert slug to readable name (e.g., "manchester-united" -> "Manchester United")
                        next_opponent_name = opponent_slug.replace('-', ' ').title()
                
                # Fallback: try to get from image alt if available
                if not next_opponent_name and opp_img:
                    alt_text = opp_img.get('alt', '')
                    if alt_text and alt_text.strip():
                        next_opponent_name = alt_text.strip()
            
            club = Club(
                position=position,
                team_name=team_name,
                team_shortname=team_shortname,
                team_id=team_id,
                team_logo_url=team_logo_url,
                matches_played=matches_played,
                wins=wins,
                draws=draws,
                losses=losses,
                goals_for=goals_for,
                goals_against=goals_against,
                goal_difference=goal_difference,
                points=points,
                form=form,
                next_opponent_id=next_opponent_id,
                next_opponent_name=next_opponent_name,
                next_opponent_logo_url=next_opponent_logo_url
            )
            
            clubs.append(club)
        
        return clubs
    
    def get_home_stats(self) -> list[Club]:
        """Get Premier League home table as a list of Club objects.
        
        Fetches the Premier League home table page (filtered for home matches only)
        and parses the table to extract club data including position, team info,
        statistics, form, and next opponent.
        
        Returns
        -------
        list[Club]
            List of Club objects containing home table data for each team.
        """
        url = f"{self.base_url}/leagues/47/table/premier-league?filter=home"
        html_content = self._get_page_html_selenium(url)
        
        soup = BeautifulSoup(html_content, 'lxml')
        
        # Find the table container
        table_container = soup.find('article', class_='TableContainer')
        if table_container is None:
            raise ValueError("Table container not found in the page")
        
        # Find all table rows - they are in divs with class containing "TableRowCSS"
        # The rows are inside divs with class "flipmove"
        rows = table_container.find_all('div', class_=lambda x: x and 'TableRowCSS' in x)
        
        clubs = []
        
        for row in rows:
            # Extract position
            position_cell = row.find('div', class_=lambda x: x and 'TablePositionCell' in x)
            if not position_cell:
                continue
            
            try:
                position = int(position_cell.get_text(strip=True))
            except ValueError:
                continue
            
            # Extract team info
            team_cell = row.find('div', class_=lambda x: x and 'TableTeamCell' in x)
            if not team_cell:
                continue
            
            team_link = team_cell.find('a', class_=lambda x: x and 'TeamLink' in x)
            if not team_link:
                continue
            
            # Extract team ID from href (e.g., "/pl/teams/9825/overview/arsenal" -> 9825)
            href = team_link.get('href', '')
            team_id_match = re.search(r'/teams/(\d+)/', href)
            team_id = int(team_id_match.group(1)) if team_id_match else 0
            
            # Extract team logo URL
            team_img = team_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
            team_logo_url = team_img.get('src', '') if team_img else ''
            
            # Extract team name and shortname
            team_name_elem = team_link.find('span', class_=lambda x: x and 'TeamName' in x)
            team_name = team_name_elem.get_text(strip=True) if team_name_elem else ''
            
            team_shortname_elem = team_link.find('span', class_=lambda x: x and 'TeamShortname' in x)
            team_shortname = team_shortname_elem.get_text(strip=True) if team_shortname_elem else ''
            
            # Extract statistics - find all direct child divs of the row
            # They appear in order: Position, Team, M, Z, R, P, +/-, =, PKT., Form, Next Opponent
            all_cells = row.find_all('div', recursive=False)
            
            if len(all_cells) < 11:
                continue
            
            # Extract stats in order (skip first 2: Position and Team)
            # Index 2: M (matches played)
            # Index 3: Z (wins)
            # Index 4: R (draws)
            # Index 5: P (losses)
            # Index 6: +/- (goals for/against)
            # Index 7: = (goal difference)
            # Index 8: PKT. (points)
            # Index 9: Form
            # Index 10: Next opponent
            
            matches_played = self._extract_int(all_cells[2])
            wins = self._extract_int(all_cells[3])
            draws = self._extract_int(all_cells[4])
            losses = self._extract_int(all_cells[5])
            
            # Goals for/against from +/- column (format: "40-14")
            goals_cell = all_cells[6]
            goals_text = goals_cell.get_text(strip=True)
            goals_match = re.search(r'(\d+)\s*-\s*(\d+)', goals_text)
            if goals_match:
                goals_for = int(goals_match.group(1))
                goals_against = int(goals_match.group(2))
            else:
                goals_for = 0
                goals_against = 0
            
            # Goal difference from = column (format: "+26" or "-5")
            goal_diff_cell = all_cells[7]
            goal_difference = goal_diff_cell.get_text(strip=True)
            
            # Points
            points = self._extract_int(all_cells[8])
            
            # Extract form from cell at index 9
            form_cell = all_cells[9]
            form_section = form_cell.find('section', class_=lambda x: x and 'SingleTeamForm' in x)
            form = ''
            if form_section:
                form_items = form_section.find_all('a', class_=lambda x: x and 'ResultBox' in x)
                form_chars = []
                for item in form_items:
                    classes = item.get('class', [])
                    if 'team-form__win' in str(classes):
                        form_chars.append('W')
                    elif 'team-form__draw' in str(classes):
                        form_chars.append('D')
                    elif 'team-form__loss' in str(classes):
                        form_chars.append('L')
                form = ''.join(form_chars)
            
            # Extract next opponent from cell at index 10
            next_opponent_cell = all_cells[10]
            next_opponent_link = next_opponent_cell.find('a', class_=lambda x: x and 'NextOpponentCSS' in x)
            next_opponent_id = None
            next_opponent_name = None
            next_opponent_logo_url = None
            
            if next_opponent_link:
                # Extract opponent logo and ID from image src
                opp_img = next_opponent_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
                if opp_img:
                    next_opponent_logo_url = opp_img.get('src', '')
                    # Extract team ID from logo URL (e.g., "teamlogo/10260.png" -> 10260)
                    logo_id_match = re.search(r'teamlogo/(\d+)', next_opponent_logo_url)
                    if logo_id_match:
                        next_opponent_id = int(logo_id_match.group(1))
                
                # Try to extract opponent name from match URL
                # Format: "/pl/matches/arsenal-vs-manchester-united/3c3mu0#4813596"
                opp_href = next_opponent_link.get('href', '')
                if opp_href:
                    # Extract the second team name from the match URL
                    match_url_match = re.search(r'/matches/([^/]+)-vs-([^/]+)/', opp_href)
                    if match_url_match:
                        # The second team is the opponent (assuming current team is first)
                        opponent_slug = match_url_match.group(2)
                        # Convert slug to readable name (e.g., "manchester-united" -> "Manchester United")
                        next_opponent_name = opponent_slug.replace('-', ' ').title()
                
                # Fallback: try to get from image alt if available
                if not next_opponent_name and opp_img:
                    alt_text = opp_img.get('alt', '')
                    if alt_text and alt_text.strip():
                        next_opponent_name = alt_text.strip()
            
            club = Club(
                position=position,
                team_name=team_name,
                team_shortname=team_shortname,
                team_id=team_id,
                team_logo_url=team_logo_url,
                matches_played=matches_played,
                wins=wins,
                draws=draws,
                losses=losses,
                goals_for=goals_for,
                goals_against=goals_against,
                goal_difference=goal_difference,
                points=points,
                form=form,
                next_opponent_id=next_opponent_id,
                next_opponent_name=next_opponent_name,
                next_opponent_logo_url=next_opponent_logo_url
            )
            
            clubs.append(club)
        
        return clubs
    
    def get_away_stats(self) -> list[Club]:
        """Get Premier League away table as a list of Club objects.
        
        Fetches the Premier League away table page (filtered for away matches only)
        and parses the table to extract club data including position, team info,
        statistics, form, and next opponent.
        
        Returns
        -------
        list[Club]
            List of Club objects containing away table data for each team.
        """
        url = f"{self.base_url}/leagues/47/table/premier-league?filter=away"
        html_content = self._get_page_html_selenium(url)
        
        soup = BeautifulSoup(html_content, 'lxml')
        
        # Find the table container
        table_container = soup.find('article', class_='TableContainer')
        if table_container is None:
            raise ValueError("Table container not found in the page")
        
        # Find all table rows - they are in divs with class containing "TableRowCSS"
        # The rows are inside divs with class "flipmove"
        rows = table_container.find_all('div', class_=lambda x: x and 'TableRowCSS' in x)
        
        clubs = []
        
        for row in rows:
            # Extract position
            position_cell = row.find('div', class_=lambda x: x and 'TablePositionCell' in x)
            if not position_cell:
                continue
            
            try:
                position = int(position_cell.get_text(strip=True))
            except ValueError:
                continue
            
            # Extract team info
            team_cell = row.find('div', class_=lambda x: x and 'TableTeamCell' in x)
            if not team_cell:
                continue
            
            team_link = team_cell.find('a', class_=lambda x: x and 'TeamLink' in x)
            if not team_link:
                continue
            
            # Extract team ID from href (e.g., "/pl/teams/9825/overview/arsenal" -> 9825)
            href = team_link.get('href', '')
            team_id_match = re.search(r'/teams/(\d+)/', href)
            team_id = int(team_id_match.group(1)) if team_id_match else 0
            
            # Extract team logo URL
            team_img = team_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
            team_logo_url = team_img.get('src', '') if team_img else ''
            
            # Extract team name and shortname
            team_name_elem = team_link.find('span', class_=lambda x: x and 'TeamName' in x)
            team_name = team_name_elem.get_text(strip=True) if team_name_elem else ''
            
            team_shortname_elem = team_link.find('span', class_=lambda x: x and 'TeamShortname' in x)
            team_shortname = team_shortname_elem.get_text(strip=True) if team_shortname_elem else ''
            
            # Extract statistics - find all direct child divs of the row
            # They appear in order: Position, Team, M, Z, R, P, +/-, =, PKT., Form, Next Opponent
            all_cells = row.find_all('div', recursive=False)
            
            if len(all_cells) < 11:
                continue
            
            # Extract stats in order (skip first 2: Position and Team)
            # Index 2: M (matches played)
            # Index 3: Z (wins)
            # Index 4: R (draws)
            # Index 5: P (losses)
            # Index 6: +/- (goals for/against)
            # Index 7: = (goal difference)
            # Index 8: PKT. (points)
            # Index 9: Form
            # Index 10: Next opponent
            
            matches_played = self._extract_int(all_cells[2])
            wins = self._extract_int(all_cells[3])
            draws = self._extract_int(all_cells[4])
            losses = self._extract_int(all_cells[5])
            
            # Goals for/against from +/- column (format: "40-14")
            goals_cell = all_cells[6]
            goals_text = goals_cell.get_text(strip=True)
            goals_match = re.search(r'(\d+)\s*-\s*(\d+)', goals_text)
            if goals_match:
                goals_for = int(goals_match.group(1))
                goals_against = int(goals_match.group(2))
            else:
                goals_for = 0
                goals_against = 0
            
            # Goal difference from = column (format: "+26" or "-5")
            goal_diff_cell = all_cells[7]
            goal_difference = goal_diff_cell.get_text(strip=True)
            
            # Points
            points = self._extract_int(all_cells[8])
            
            # Extract form from cell at index 9
            form_cell = all_cells[9]
            form_section = form_cell.find('section', class_=lambda x: x and 'SingleTeamForm' in x)
            form = ''
            if form_section:
                form_items = form_section.find_all('a', class_=lambda x: x and 'ResultBox' in x)
                form_chars = []
                for item in form_items:
                    classes = item.get('class', [])
                    if 'team-form__win' in str(classes):
                        form_chars.append('W')
                    elif 'team-form__draw' in str(classes):
                        form_chars.append('D')
                    elif 'team-form__loss' in str(classes):
                        form_chars.append('L')
                form = ''.join(form_chars)
            
            # Extract next opponent from cell at index 10
            next_opponent_cell = all_cells[10]
            next_opponent_link = next_opponent_cell.find('a', class_=lambda x: x and 'NextOpponentCSS' in x)
            next_opponent_id = None
            next_opponent_name = None
            next_opponent_logo_url = None
            
            if next_opponent_link:
                # Extract opponent logo and ID from image src
                opp_img = next_opponent_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
                if opp_img:
                    next_opponent_logo_url = opp_img.get('src', '')
                    # Extract team ID from logo URL (e.g., "teamlogo/10260.png" -> 10260)
                    logo_id_match = re.search(r'teamlogo/(\d+)', next_opponent_logo_url)
                    if logo_id_match:
                        next_opponent_id = int(logo_id_match.group(1))
                
                # Try to extract opponent name from match URL
                # Format: "/pl/matches/arsenal-vs-manchester-united/3c3mu0#4813596"
                opp_href = next_opponent_link.get('href', '')
                if opp_href:
                    # Extract the second team name from the match URL
                    match_url_match = re.search(r'/matches/([^/]+)-vs-([^/]+)/', opp_href)
                    if match_url_match:
                        # The second team is the opponent (assuming current team is first)
                        opponent_slug = match_url_match.group(2)
                        # Convert slug to readable name (e.g., "manchester-united" -> "Manchester United")
                        next_opponent_name = opponent_slug.replace('-', ' ').title()
                
                # Fallback: try to get from image alt if available
                if not next_opponent_name and opp_img:
                    alt_text = opp_img.get('alt', '')
                    if alt_text and alt_text.strip():
                        next_opponent_name = alt_text.strip()
            
            club = Club(
                position=position,
                team_name=team_name,
                team_shortname=team_shortname,
                team_id=team_id,
                team_logo_url=team_logo_url,
                matches_played=matches_played,
                wins=wins,
                draws=draws,
                losses=losses,
                goals_for=goals_for,
                goals_against=goals_against,
                goal_difference=goal_difference,
                points=points,
                form=form,
                next_opponent_id=next_opponent_id,
                next_opponent_name=next_opponent_name,
                next_opponent_logo_url=next_opponent_logo_url
            )
            
            clubs.append(club)
        
        return clubs
    
    def get_lat_5_games_stats(self) -> list[Club]:
        """Get Premier League table filtered by form (last 5 games) as a list of Club objects.
        
        Fetches the Premier League table page filtered by form and parses the table to extract
        club data including position, team info, statistics, form, and next opponent.
        
        Returns
        -------
        list[Club]
            List of Club objects containing form table data for each team.
        """
        url = f"{self.base_url}/leagues/47/table/premier-league?filter=form"
        html_content = self._get_page_html_selenium(url)
        
        soup = BeautifulSoup(html_content, 'lxml')
        
        # Find the table container
        table_container = soup.find('article', class_='TableContainer')
        if table_container is None:
            raise ValueError("Table container not found in the page")
        
        # Find all table rows - they are in divs with class containing "TableRowCSS"
        # The rows are inside divs with class "flipmove"
        rows = table_container.find_all('div', class_=lambda x: x and 'TableRowCSS' in x)
        
        clubs = []
        
        for row in rows:
            # Extract position
            position_cell = row.find('div', class_=lambda x: x and 'TablePositionCell' in x)
            if not position_cell:
                continue
            
            try:
                position = int(position_cell.get_text(strip=True))
            except ValueError:
                continue
            
            # Extract team info
            team_cell = row.find('div', class_=lambda x: x and 'TableTeamCell' in x)
            if not team_cell:
                continue
            
            team_link = team_cell.find('a', class_=lambda x: x and 'TeamLink' in x)
            if not team_link:
                continue
            
            # Extract team ID from href (e.g., "/pl/teams/9825/overview/arsenal" -> 9825)
            href = team_link.get('href', '')
            team_id_match = re.search(r'/teams/(\d+)/', href)
            team_id = int(team_id_match.group(1)) if team_id_match else 0
            
            # Extract team logo URL
            team_img = team_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
            team_logo_url = team_img.get('src', '') if team_img else ''
            
            # Extract team name and shortname
            team_name_elem = team_link.find('span', class_=lambda x: x and 'TeamName' in x)
            team_name = team_name_elem.get_text(strip=True) if team_name_elem else ''
            
            team_shortname_elem = team_link.find('span', class_=lambda x: x and 'TeamShortname' in x)
            team_shortname = team_shortname_elem.get_text(strip=True) if team_shortname_elem else ''
            
            # Extract statistics - find all direct child divs of the row
            # They appear in order: Position, Team, M, Z, R, P, +/-, =, PKT., Form, Next Opponent
            all_cells = row.find_all('div', recursive=False)
            
            if len(all_cells) < 11:
                continue
            
            # Extract stats in order (skip first 2: Position and Team)
            # Index 2: M (matches played)
            # Index 3: Z (wins)
            # Index 4: R (draws)
            # Index 5: P (losses)
            # Index 6: +/- (goals for/against)
            # Index 7: = (goal difference)
            # Index 8: PKT. (points)
            # Index 9: Form
            # Index 10: Next opponent
            
            matches_played = self._extract_int(all_cells[2])
            wins = self._extract_int(all_cells[3])
            draws = self._extract_int(all_cells[4])
            losses = self._extract_int(all_cells[5])
            
            # Goals for/against from +/- column (format: "40-14")
            goals_cell = all_cells[6]
            goals_text = goals_cell.get_text(strip=True)
            goals_match = re.search(r'(\d+)\s*-\s*(\d+)', goals_text)
            if goals_match:
                goals_for = int(goals_match.group(1))
                goals_against = int(goals_match.group(2))
            else:
                goals_for = 0
                goals_against = 0
            
            # Goal difference from = column (format: "+26" or "-5")
            goal_diff_cell = all_cells[7]
            goal_difference = goal_diff_cell.get_text(strip=True)
            
            # Points
            points = self._extract_int(all_cells[8])
            
            # Extract form from cell at index 9
            form_cell = all_cells[9]
            form_section = form_cell.find('section', class_=lambda x: x and 'SingleTeamForm' in x)
            form = ''
            if form_section:
                form_items = form_section.find_all('a', class_=lambda x: x and 'ResultBox' in x)
                form_chars = []
                for item in form_items:
                    classes = item.get('class', [])
                    if 'team-form__win' in str(classes):
                        form_chars.append('W')
                    elif 'team-form__draw' in str(classes):
                        form_chars.append('D')
                    elif 'team-form__loss' in str(classes):
                        form_chars.append('L')
                form = ''.join(form_chars)
            
            # Extract next opponent from cell at index 10
            next_opponent_cell = all_cells[10]
            next_opponent_link = next_opponent_cell.find('a', class_=lambda x: x and 'NextOpponentCSS' in x)
            next_opponent_id = None
            next_opponent_name = None
            next_opponent_logo_url = None
            
            if next_opponent_link:
                # Extract opponent logo and ID from image src
                opp_img = next_opponent_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
                if opp_img:
                    next_opponent_logo_url = opp_img.get('src', '')
                    # Extract team ID from logo URL (e.g., "teamlogo/10260.png" -> 10260)
                    logo_id_match = re.search(r'teamlogo/(\d+)', next_opponent_logo_url)
                    if logo_id_match:
                        next_opponent_id = int(logo_id_match.group(1))
                
                # Try to extract opponent name from match URL
                # Format: "/pl/matches/arsenal-vs-manchester-united/3c3mu0#4813596"
                opp_href = next_opponent_link.get('href', '')
                if opp_href:
                    # Extract the second team name from the match URL
                    match_url_match = re.search(r'/matches/([^/]+)-vs-([^/]+)/', opp_href)
                    if match_url_match:
                        # The second team is the opponent (assuming current team is first)
                        opponent_slug = match_url_match.group(2)
                        # Convert slug to readable name (e.g., "manchester-united" -> "Manchester United")
                        next_opponent_name = opponent_slug.replace('-', ' ').title()
                
                # Fallback: try to get from image alt if available
                if not next_opponent_name and opp_img:
                    alt_text = opp_img.get('alt', '')
                    if alt_text and alt_text.strip():
                        next_opponent_name = alt_text.strip()
            
            club = Club(
                position=position,
                team_name=team_name,
                team_shortname=team_shortname,
                team_id=team_id,
                team_logo_url=team_logo_url,
                matches_played=matches_played,
                wins=wins,
                draws=draws,
                losses=losses,
                goals_for=goals_for,
                goals_against=goals_against,
                goal_difference=goal_difference,
                points=points,
                form=form,
                next_opponent_id=next_opponent_id,
                next_opponent_name=next_opponent_name,
                next_opponent_logo_url=next_opponent_logo_url
            )
            
            clubs.append(club)
        
        return clubs
    
    def get_xg_stats(self) -> list[XgStats]:
        """Get Premier League xG statistics table as a list of XgStats objects.
        
        Fetches the Premier League xG table page and parses the table to extract
        xG statistics including position, position change, team info, xG, xGA, and xPTS.
        
        Returns
        -------
        list[XgStats]
            List of XgStats objects containing xG statistics for each team.
        """
        url = f"{self.base_url}/leagues/47/table/premier-league?filter=xg"
        html_content = self._get_page_html_selenium(url)
        
        soup = BeautifulSoup(html_content, 'lxml')
        
        # Find the table container
        table_container = soup.find('article', class_='TableContainer')
        if table_container is None:
            raise ValueError("Table container not found in the page")
        
        # Find all table rows - they are in divs with class containing "TableRowCSS"
        rows = table_container.find_all('div', class_=lambda x: x and 'TableRowCSS' in x)
        
        xg_stats_list = []
        
        for row in rows:
            # Find all direct child td elements (cells in the row)
            cells = row.find_all('td', recursive=False)
            
            if len(cells) < 7:
                continue
            
            # Extract position from first cell (XgTablePositionCell)
            position_cell = cells[0]
            try:
                position = int(position_cell.get_text(strip=True))
            except ValueError:
                continue
            
            # Extract position change from second cell (chevron wrapper)
            position_change_cell = cells[1]
            position_change = None
            chevron_wrapper = position_change_cell.find('div', class_=lambda x: x and 'ChevronWrapper' in x)
            if chevron_wrapper:
                span = chevron_wrapper.find('span')
                if span:
                    change_text = span.get_text(strip=True)
                    # Parse position change (e.g., "0", "+2", "-1")
                    if change_text and change_text != "0":
                        try:
                            position_change = int(change_text)
                        except ValueError:
                            pass
            
            # Extract team info from third cell (XgTableTeamCell)
            team_cell = cells[2]
            team_link = team_cell.find('a', class_=lambda x: x and 'TeamLink' in x)
            if not team_link:
                continue
            
            # Extract team ID from href (e.g., "/teams/9825/overview/arsenal" -> 9825)
            href = team_link.get('href', '')
            team_id_match = re.search(r'/teams/(\d+)/', href)
            team_id = int(team_id_match.group(1)) if team_id_match else 0
            
            # Extract team logo URL
            team_img = team_link.find('img', class_=lambda x: x and 'TeamIcon' in x)
            team_logo_url = team_img.get('src', '') if team_img else ''
            
            # Extract team name and shortname
            team_name_elem = team_link.find('span', class_=lambda x: x and 'TeamName' in x)
            team_name = team_name_elem.get_text(strip=True) if team_name_elem else ''
            
            team_shortname_elem = team_link.find('span', class_=lambda x: x and 'TeamShortname' in x)
            team_shortname = team_shortname_elem.get_text(strip=True) if team_shortname_elem else ''
            
            # Extract xG from cells (after position, position change, team, and matches played)
            # The structure is: Position, Position Change, Team, Matches Played, xG, xGA, xPTS
            # So xG is at index 4, xGA at index 5, xPTS at index 6
            xg_cell = cells[4]
            xga_cell = cells[5]
            xpts_cell = cells[6]
            
            # Helper function to extract value and diff from xG cell
            def extract_xg_value(cell):
                """Extract main value and diff from xG/xGA/xPTS cell."""
                if not cell:
                    return None, None
                
                xg_cell_div = cell.find('div', class_=lambda x: x and 'XgCellCSS' in x)
                if not xg_cell_div:
                    return None, None
                
                # Extract main value from span with MainNumber class
                # Prefer title attribute for precision, otherwise use text
                main_number_span = xg_cell_div.find('span', class_=lambda x: x and 'MainNumber' in x)
                if not main_number_span:
                    return None, None
                
                main_value = None
                title = main_number_span.get('title')
                if title:
                    try:
                        main_value = float(title)
                    except ValueError:
                        pass
                
                if main_value is None:
                    text = main_number_span.get_text(strip=True)
                    try:
                        main_value = float(text)
                    except ValueError:
                        return None, None
                
                # Extract diff from sup with DiffText class
                # Keep as string to preserve the + sign for positive values
                diff_sup = xg_cell_div.find('sup', class_=lambda x: x and 'DiffText' in x)
                diff_value = None
                if diff_sup:
                    diff_text = diff_sup.get_text(strip=True)
                    if diff_text:
                        # Keep the diff as a string to preserve the + sign
                        diff_value = diff_text
                
                return main_value, diff_value
            
            xg, xg_diff = extract_xg_value(xg_cell)
            xga, xga_diff = extract_xg_value(xga_cell)
            xpts, xpts_diff = extract_xg_value(xpts_cell)
            
            # Skip if we couldn't extract required values
            if xg is None or xga is None or xpts is None:
                continue
            
            xg_stat = XgStats(
                position=position,
                position_change=position_change,
                team_id=team_id,
                team_name=team_name,
                team_shortname=team_shortname,
                team_logo_url=team_logo_url,
                xg=xg,
                xg_diff=xg_diff,
                xga=xga,
                xga_diff=xga_diff,
                xpts=xpts,
                xpts_diff=xpts_diff
            )
            
            xg_stats_list.append(xg_stat)
        
        return xg_stats_list
    
    def _extract_int(self, element) -> int:
        """Extract integer value from an element.
        
        Parameters
        ----------
        element : bs4.element.Tag
            BeautifulSoup element to extract integer from.
            
        Returns
        -------
        int
            Extracted integer value, or 0 if extraction fails.
        """
        if not element:
            return 0
        
        text = element.get_text(strip=True)
        # Remove any non-digit characters except minus sign
        text = re.sub(r'[^\d-]', '', text)
        try:
            return int(text)
        except ValueError:
            return 0
