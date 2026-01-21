import re
from bs4 import BeautifulSoup
from typing import Optional, List
from .base_scraper import BaseScraper
from models.rotowire import (
    GameLineup,
    TeamLineup,
    PlayerInLineup,
    InjuryEntry,
    GameOdds,
    WeatherInfo,
)


class Rotowire(BaseScraper):
    """RotoWire scraper class for fetching and parsing soccer lineup data from rotowire.com."""
    
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
        """Initialize RotoWire scraper.
        
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
        self.base_url = "https://www.rotowire.com"
    
    def get_soccer_lineups(self,) -> List[GameLineup]:
        """Get soccer lineups from RotoWire.
        
        Returns
        -------
        List[GameLineup]
            List of games with team lineups, injuries, odds, and weather for all leagues.
        """
        url = f"{self.base_url}/soccer/lineups.php"
        html_content = self._get_page_html(url)
        return self._parse_lineups(html_content)
    
    def _parse_lineups(self, html: str) -> List[GameLineup]:
        """Parse the lineups HTML page.
        
        Parameters
        ----------
        html : str
            HTML content of the lineups page.
            
        Returns
        -------
        List[GameLineup]
            List of parsed game lineups.
        """
        soup = BeautifulSoup(html, 'lxml')
        games = []
        
        # Find all lineup divs - each div with class "lineup is-soccer" represents one game
        lineup_divs = soup.select('div.lineup.is-soccer')
        
        # Parse each lineup div
        for lineup_div in lineup_divs:
            try:
                game = self._parse_game_section(lineup_div, soup)
                if game:
                    games.append(game)
            except Exception:
                # Log error but continue with other games
                continue
        
        return games
    
    def _parse_game_section(self, section, full_soup: BeautifulSoup) -> Optional[GameLineup]:
        """Parse a single game section (div with class 'lineup is-soccer') into a GameLineup object."""
        # Extract date and time from lineup__time div
        date = None
        time = None
        time_elem = section.find('div', class_='lineup__time')
        if time_elem:
            time_text = time_elem.get_text(strip=True)
            # Format is like "January 17  10:00 AM ET" or "<b>January 17</b>&nbsp; 10:00 AM ET"
            # Extract date (month and day)
            date_match = re.search(r'(\w+\s+\d+)', time_text)
            if date_match:
                date = date_match.group(1)
            # Extract time (e.g., "10:00 AM ET")
            time_match = re.search(r'(\d+:\d+\s+(?:AM|PM)\s+ET)', time_text)
            if time_match:
                time = time_match.group(1)
        
        # Extract team codes from lineup__abbr divs
        home_code = None
        away_code = None
        team_abbrs = section.find_all('div', class_='lineup__abbr')
        if len(team_abbrs) >= 2:
            home_code = team_abbrs[0].get_text(strip=True)
            away_code = team_abbrs[1].get_text(strip=True)
        
        # Extract team names from lineup__mteam divs
        home_team_name = None
        away_team_name = None
        home_team_elem = section.find('div', class_='lineup__mteam is-home')
        away_team_elem = section.find('div', class_='lineup__mteam is-visit')
        
        if home_team_elem:
            home_team_name = home_team_elem.get_text(strip=True)
        if away_team_elem:
            away_team_name = away_team_elem.get_text(strip=True)
        
        if not home_code or not away_code:
            return None
        
        # Parse lineups for both teams
        home_lineup = self._parse_team_lineup(section, home_code, home_team_name or f"Team {home_code}")
        away_lineup = self._parse_team_lineup(section, away_code, away_team_name or f"Team {away_code}")
        
        # Parse odds
        odds = self._parse_odds(section)
        
        # Parse weather
        weather = self._parse_weather(section)
        
        if not date:
            date = "Unknown"
        
        return GameLineup(
            date=date,
            time=time,
            home_team=home_lineup,
            away_team=away_lineup,
            odds=odds,
            weather=weather
        )
    
    def _parse_team_lineup(self, section, team_code: str, team_name: str) -> TeamLineup:
        """Parse lineup information for a single team."""
        players = []
        injuries: list[InjuryEntry] = []
        lineup_type = "Predicted Lineup"  # Default
        
        # Determine which team we're parsing (home or away) by checking team codes
        home_team_elem = section.find('div', class_='lineup__team is-home')
        
        is_home = False
        if home_team_elem:
            home_abbr = home_team_elem.find('div', class_='lineup__abbr')
            if home_abbr and home_abbr.get_text(strip=True) == team_code:
                is_home = True
        
        # Find the lineup list for this team
        lineup_list_class = 'lineup__list is-home' if is_home else 'lineup__list is-visit'
        lineup_list = section.find('ul', class_=lineup_list_class)
        
        if not lineup_list:
            # Fallback: try to find by team code in text
            for ul in section.find_all('ul', class_='lineup__list'):
                if team_code in ul.get_text():
                    lineup_list = ul
                    break
        
        if not lineup_list:
            return TeamLineup(
                team_name=team_name,
                team_code=team_code,
                lineup_type=lineup_type,
                players=players,
                injuries=injuries
            )
        
        # Determine lineup type from status element
        status_elem = lineup_list.find('li', class_='lineup__status')
        if status_elem:
            status_text = status_elem.get_text(strip=True)
            if 'Confirmed Lineup' in status_text:
                lineup_type = "Confirmed Lineup"
            elif 'Predicted Lineup' in status_text:
                lineup_type = "Predicted Lineup"
        
        # Track if we've reached the injuries section
        # All players BEFORE the "Injuries" separator are in the lineup
        # All players AFTER the "Injuries" separator are injuries only
        in_injuries_section = False
        
        # Parse all list items
        for li in lineup_list.find_all('li'):
            # Check if this is the injuries header separator
            if 'lineup__title' in li.get('class', []) and 'Injuries' in li.get_text():
                in_injuries_section = True
                continue
            
            # Skip status element
            if 'lineup__status' in li.get('class', []):
                continue
            
            # Parse player or injury
            if 'lineup__player' in li.get('class', []):
                pos_elem = li.find('div', class_='lineup__pos')
                name_elem = li.find('a')
                injury_elem = li.find('span', class_='lineup__inj')
                
                if pos_elem and name_elem:
                    position = pos_elem.get_text(strip=True)
                    player_name = name_elem.get_text(strip=True)
                    
                    if in_injuries_section:
                        # After the separator: these are injury entries only
                        if injury_elem:
                            status = injury_elem.get_text(strip=True)
                            injuries.append(InjuryEntry(
                                player=player_name,
                                position=position,
                                status=status
                            ))
                    else:
                        # Before the separator: these are all lineup players
                        # (even if they have an injury status, they're still in the lineup)
                        players.append(PlayerInLineup(
                            position=position,
                            player=player_name
                        ))
        
        return TeamLineup(
            team_name=team_name,
            team_code=team_code,
            lineup_type=lineup_type,
            players=players,
            injuries=injuries
        )
    
    def _parse_odds(self, section) -> Optional[GameOdds]:
        """Parse betting odds from the game section."""
        odds_div = section.find('div', class_='lineup__odds')
        if not odds_div:
            return None
        
        # Get team codes to match odds
        home_code = None
        away_code = None
        home_team_elem = section.find('div', class_='lineup__team is-home')
        away_team_elem = section.find('div', class_='lineup__team is-visit')
        if home_team_elem:
            home_abbr = home_team_elem.find('div', class_='lineup__abbr')
            if home_abbr:
                home_code = home_abbr.get_text(strip=True)
        if away_team_elem:
            away_abbr = away_team_elem.find('div', class_='lineup__abbr')
            if away_abbr:
                away_code = away_abbr.get_text(strip=True)
        
        home_odds = None
        draw_odds = None
        away_odds = None
        
        # Find all odds items
        odds_items = odds_div.find_all('div', class_='lineup__odds-item')
        
        for item in odds_items:
            item_text = item.get_text()
            # Find the selected odds span (has class "is-selected")
            selected_span = item.find('span', class_=lambda x: x and 'is-selected' in x)
            if selected_span:
                odds_value = selected_span.get_text(strip=True)
                # Skip if it's a dash
                if odds_value == '–' or odds_value == '-':
                    continue
                
                # Determine which odds this is based on the label
                if 'Draw:' in item_text:
                    draw_odds = odds_value
                elif home_code and home_code in item_text:
                    home_odds = odds_value
                elif away_code and away_code in item_text:
                    away_odds = odds_value
        
        # Fallback: if we didn't match by code, use order (home, draw, away)
        if not (home_odds and draw_odds and away_odds):
            all_selected = odds_div.find_all('span', class_=lambda x: x and 'is-selected' in x)
            if len(all_selected) >= 3:
                if not home_odds:
                    home_odds = all_selected[0].get_text(strip=True) if all_selected[0].get_text(strip=True) not in ['–', '-'] else None
                if not draw_odds:
                    draw_odds = all_selected[1].get_text(strip=True) if all_selected[1].get_text(strip=True) not in ['–', '-'] else None
                if not away_odds:
                    away_odds = all_selected[2].get_text(strip=True) if all_selected[2].get_text(strip=True) not in ['–', '-'] else None
        
        # Return GameOdds object when odds_div exists, even if all odds are None (e.g., dashes)
        return GameOdds(
            home_odds=home_odds,
            draw_odds=draw_odds,
            away_odds=away_odds
        )
    
    def _parse_weather(self, section) -> Optional[WeatherInfo]:
        """Parse weather information from the game section."""
        weather_div = section.find('div', class_='lineup__weather')
        if not weather_div:
            return None
        
        condition = None
        precipitation = None
        temperature = None
        wind = None
        
        # Extract condition from weather icon alt text
        weather_icon = weather_div.find('img', class_='lineup__weather-icon')
        if weather_icon and weather_icon.get('alt'):
            condition = weather_icon.get('alt')
        
        # Extract weather text
        weather_text_elem = weather_div.find('div', class_='lineup__weather-text')
        if weather_text_elem:
            weather_text = weather_text_elem.get_text()
            
            # Extract precipitation
            precip_match = re.search(r'(\d+)%\s*Precipitation', weather_text)
            if precip_match:
                precipitation = f"{precip_match.group(1)}%"
            
            # Extract temperature (format: "49°")
            temp_match = re.search(r'(\d+)°', weather_text)
            if temp_match:
                temperature = f"{temp_match.group(1)}°"
            
            # Extract wind (format: "Wind 5 mph 153.3" or "Wind 8 mph 138.4")
            wind_match = re.search(r'Wind\s+(\d+\s*mph)', weather_text)
            if wind_match:
                wind = wind_match.group(1)
        
        if any([condition, precipitation, temperature, wind]):
            return WeatherInfo(
                condition=condition,
                precipitation=precipitation,
                temperature=temperature,
                wind=wind
            )
        
        return None
