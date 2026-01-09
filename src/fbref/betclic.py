from curl_cffi import requests
from bs4 import BeautifulSoup
from typing import List
from models.upcoming_game import UpcomingGame


class Betclic:
    """Betclic scraper class for fetching HTML from betclic.pl."""
    
    def __init__(self, impersonate: str = "chrome110"):
        """Initialize Betclic scraper.
        
        Parameters
        ----------
        impersonate : str
            Browser impersonation string for curl_cffi. Default is "chrome110".
        """
        self.impersonate = impersonate
        self.base_url = "https://www.betclic.pl"
    
    def get_premier_league_html(self) -> str:
        """Get HTML content from the Premier League page on Betclic.
        
        Returns
        -------
        str
            HTML content of the page.
        """
        url = f"{self.base_url}/football-sfootball/premier-league-c3"
        response = self._fetch_page(url)
        
        if response.status_code == 200:
            return response.text
        else:
            raise Exception(f"Error fetching page: Status code {response.status_code}")
    
    def get_upcoming_games(self) -> List[UpcomingGame]:
        """Get list of upcoming games from the Premier League page.
        
        Extracts the first div with class 'groupEvents' and parses all games
        from it.
        
        Returns
        -------
        List[UpcomingGame]
            List of upcoming games with team names, time, odds, and URL.
        """
        html = self.get_premier_league_html()
        soup = BeautifulSoup(html, 'lxml')
        
        # Find the first div with class 'groupEvents'
        group_events = soup.find('div', class_='groupEvents')
        if not group_events:
            return []
        
        # Extract date from the header
        date_header = group_events.find('h2', class_='groupEvents_headTitle')
        date = date_header.get_text(strip=True) if date_header else ""
        
        # Find all game cards
        game_cards = group_events.find_all('sports-events-event-card', class_='groupEvents_card')
        games = []
        
        for card in game_cards:
            # Extract URL
            link = card.find('a', class_='cardEvent')
            url = ""
            if link and link.get('href'):
                url = f"{self.base_url}{link['href']}"
            
            # Extract team names
            home_team_elem = card.find('div', {'data-qa': 'contestant-1-label'})
            away_team_elem = card.find('div', {'data-qa': 'contestant-2-label'})
            home_team = home_team_elem.get_text(strip=True) if home_team_elem else ""
            away_team = away_team_elem.get_text(strip=True) if away_team_elem else ""
            
            # Extract time
            time_elem = card.find('div', class_='scoreboard_hour')
            time = time_elem.get_text(strip=True) if time_elem else ""
            
            # Extract odds from .market_odds div (three buttons: home, draw, away)
            market_odds_div = card.find('div', class_='market_odds')
            home_odds = None
            draw_odds = None
            away_odds = None
            
            if market_odds_div:
                # Find all buttons within market_odds div
                odds_buttons = market_odds_div.find_all('button', class_='btn')

                if len(odds_buttons) >= 3:
                    # 1st button is home, 2nd is draw, 3rd is away
                    # Each button has two spans with btn_label: one with is-top (team name) and one without (odds)
                    for i, button in enumerate(odds_buttons[:3]):
                        # Find all spans with btn_label and get the one without is-top class (the odds value)
                        label_spans = button.find_all('span', class_='btn_label')
                        odds_value = None
                        for span in label_spans:
                            if 'is-top' not in span.get('class', []):
                                odds_value = span.get_text(strip=True)
                                break

                        if odds_value:
                            try:
                                odds_float = float(odds_value.replace(',', '.'))
                                if i == 0:
                                    home_odds = odds_float
                                elif i == 1:
                                    draw_odds = odds_float
                                elif i == 2:
                                    away_odds = odds_float
                            except (ValueError, AttributeError):
                                pass
            
            games.append(UpcomingGame(
                date=date,
                home_team=home_team,
                away_team=away_team,
                time=time,
                home_odds=home_odds,
                draw_odds=draw_odds,
                away_odds=away_odds,
                url=url
            ))
        
        return games
    
    def _fetch_page(self, url: str) -> requests.Response:
        """Fetch a page from betclic.pl.
        
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

