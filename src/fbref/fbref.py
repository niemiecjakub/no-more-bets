from bs4 import BeautifulSoup
from curl_cffi import requests
from models import Club

class FBref:
    """FBref scraper class for fetching and parsing data from fbref.com."""
    
    def __init__(self, impersonate: str = "chrome110"):
        """Initialize FBref scraper.
        
        Parameters
        ----------
        impersonate : str
            Browser impersonation string for curl_cffi. Default is "chrome110".
        """
        self.impersonate = impersonate
        self.base_url = "https://fbref.com"
    
    def fetch_page(self, url: str) -> requests.Response:
        """Fetch a page from fbref.com.
        
        Parameters
        ----------
        url : str
            URL to fetch.
            
        Returns
        -------
        requests.Response
            Response object from the request.
        """
        return requests.get(url, impersonate=self.impersonate)    
    
    
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
        response = self.fetch_page(url)
        
        if response.status_code == 200:
            html_content = response.text
        else:
            raise Exception(f"Error fetching page: Status code {response.status_code}")
        
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

