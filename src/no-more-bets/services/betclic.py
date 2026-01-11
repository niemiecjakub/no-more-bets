from bs4 import BeautifulSoup
from typing import List, Optional
import time
import random
from models.upcoming_game import UpcomingGame
from models.bookmaker_event import BookmakerEvent, EventOption
from .base_scraper import BaseScraper


class Betclic(BaseScraper):
    """Betclic scraper class for fetching data from betclic.pl."""
    
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
        n_retries: int = 5,
    ):
        """Initialize Betclic scraper.
        
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
        n_retries : int
            Number of retry attempts when empty results are returned. Default is 5.
        """
        super().__init__(impersonate, delay, retry_count, retry_delay, timeout, store, use_cache, cache_ttl)
        self.base_url = "https://www.betclic.pl"
        self.n_retries = n_retries
    
    def get_premier_league_html(self) -> str:
        """Get HTML content from the Premier League page on Betclic.
        
        Returns
        -------
        str
            HTML content of the page (uses caching if enabled).
        """
        url = f"{self.base_url}/football-sfootball/premier-league-c3"
        return self._get_page_html(url)
    
    def get_upcoming_games(self) -> List[UpcomingGame]:
        """Get list of upcoming games from the Premier League page.
        
        Extracts the first div with class 'groupEvents' and parses all games
        from it. If the result is empty, clears cache and retries up to n_retries times.
        
        Returns
        -------
        List[UpcomingGame]
            List of upcoming games with team names, time, odds, and URL.
        """
        url = f"{self.base_url}/football-sfootball/premier-league-c3"
        
        for attempt in range(self.n_retries):
            html = self._get_page_html(url)
            soup = BeautifulSoup(html, 'lxml')
            
            # Find the first div with class 'groupEvents'
            group_events = soup.find('div', class_='groupEvents')
            if not group_events:
                games = []
            else:
                # Extract date from the header
                date_header = group_events.find('h2', class_='groupEvents_headTitle')
                date = date_header.get_text(strip=True) if date_header else ""
                
                # Find all game cards
                game_cards = group_events.find_all('sports-events-event-card', class_='groupEvents_card')
                games = []
                
                for card in game_cards:
                    # Extract URL
                    link = card.find('a', class_='cardEvent')
                    card_url = ""
                    if link and link.get('href'):
                        card_url = f"{self.base_url}{link['href']}"
                    
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
                        url=card_url
                    ))
            
            # If games list is not empty, return it
            if games:
                return games
            
            # If empty and retries remaining, clear cache and retry
            if attempt < self.n_retries - 1:
                self.clear_cache(url)
                wait_time = random.uniform(3, 10)
                time.sleep(wait_time)
        
        # Return empty list if all retries exhausted
        return games
    
    def get_match_events(self, game_link: str, expand: bool = False) -> List[BookmakerEvent]:
        """Get all bookmaker events for a specific match.
        
        Fetches the match page and extracts all events from the verticalScroller_list container.
        If the result is empty, clears cache and retries up to n_retries times.
        
        Parameters
        ----------
        game_link : str
            URL to the match page (can be relative or absolute).
        expand : bool, optional
            If True, clicks all "see more" buttons to expand hidden content before extraction.
            Default is False.
            
        Returns
        -------
        List[BookmakerEvent]
            List of extracted bookmaker events.
            
        Raises
        ------
        Exception
            If the page cannot be fetched or the verticalScroller_list container is not found.
        ImportError
            If expand=True but selenium is not installed.
        """
        events = []
        
        for attempt in range(self.n_retries):
            # If expand is True, use browser automation to click "see more" buttons
            if expand:
                html = self._fetch_and_expand_page(game_link)
            else:
                # Fetch the page normally (uses caching if enabled)
                html = self._get_page_html_selenium(game_link)
            
            events = self._extract_events(str(html))
            events = self._aggregate_events(events)
            
            # If events list is not empty, return it
            if events:
                return events
            
            # If empty and retries remaining, clear cache and retry
            if attempt < self.n_retries - 1:
                self.clear_cache(game_link)
                wait_time = random.uniform(8, 20)
                time.sleep(wait_time)
        
        # Return empty list if all retries exhausted
        return events
    
    def _extract_events(self, html: str) -> List[BookmakerEvent]:
        """Extract all bookmaker events from HTML content.
        
        Parameters
        ----------
        html : str
            HTML content containing bookmaker events.
            
        Returns
        -------
        List[BookmakerEvent]
            List of extracted bookmaker events.
        """
        soup = BeautifulSoup(html, 'lxml')
        events = []
        
        # Find all marketBox elements directly
        market_boxes = soup.find_all('div', class_='marketBox')
        
        for market_box in market_boxes:
            # Extract title
            title_elem = market_box.find('h2', class_='marketBox_headTitle')
            if not title_elem:
                continue
            
            title = title_elem.get_text(strip=True)
            
            # Determine market type and parse accordingly
            # Check for split-card markets (goalscorer markets)
            split_cards = market_box.find_all('sports-split-card')
            if split_cards:
                # Goalscorer market - split by teams, create separate events per team
                for split_card in split_cards:
                    team_title_elem = split_card.find('div', class_='marketBox_bodyTitle')
                    if not team_title_elem:
                        continue
                    
                    team_name = team_title_elem.get_text(strip=True)
                    card_options = self._parse_matrix_options(split_card)
                    
                    if card_options:
                        events.append(BookmakerEvent(
                            title=f"{title} - {team_name}",
                            options=card_options
                        ))
                continue
            
            # Check for grouped markets
            if 'is-groupedMarket' in market_box.get('class', []) or market_box.find('div', class_='marketBox_list'):
                # Parse as grouped market
                grouped_events = self._parse_grouped_market_from_box(market_box, title)
                events.extend(grouped_events)
                continue
            
            # Check for simple/matrix markets
            # Look for containers: sports-matrix-markets, sports-spaced-blocks, or sports-simple-markets
            matrix_markets = market_box.find('sports-matrix-markets')
            spaced_blocks = market_box.find('sports-spaced-blocks')
            simple_markets = market_box.find('sports-simple-markets')
            
            container = None
            if matrix_markets:
                container = matrix_markets
            elif spaced_blocks:
                container = spaced_blocks
            elif simple_markets:
                # For sports-simple-markets, look for marketBox_body inside
                container = simple_markets.find('div', class_='marketBox_body')
                if not container:
                    container = simple_markets
            
            if container:
                options = self._parse_matrix_options(container)
                if options:
                    events.append(BookmakerEvent(
                        title=title,
                        options=options
                    ))
            else:
                # Fallback: try to parse directly from marketBox_body
                market_box_body = market_box.find('div', class_='marketBox_body')
                if market_box_body:
                    options = self._parse_matrix_options(market_box_body)
                    if options:
                        events.append(BookmakerEvent(
                            title=title,
                            options=options
                        ))
        
        return events

    def _fetch_and_expand_page(self, url: str) -> str:
        """Fetch page using browser automation and click all "see more" buttons.
        
        Parameters
        ----------
        url : str
            URL to fetch.
            
        Returns
        -------
        str
            HTML content after expanding all "see more" buttons.
            
        Raises
        ------
        ImportError
            If selenium is not installed.
        """
        from selenium import webdriver
        from selenium.webdriver.common.by import By
        from selenium.webdriver.support.ui import WebDriverWait
        from selenium.webdriver.support import expected_conditions as EC
        from selenium.webdriver.chrome.options import Options
        from selenium.common.exceptions import TimeoutException, NoSuchElementException

        
        # Setup Chrome options for headless browsing
        chrome_options = Options()
        chrome_options.add_argument('--headless')
        chrome_options.add_argument('--no-sandbox')
        chrome_options.add_argument('--disable-dev-shm-usage')
        chrome_options.add_argument('--disable-blink-features=AutomationControlled')
        chrome_options.add_argument('user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36')
        
        driver = None
        try:
            driver = webdriver.Chrome(options=chrome_options)
            driver.get(url)
            
            # Wait for page to load
            WebDriverWait(driver, 5).until(
                EC.presence_of_element_located((By.TAG_NAME, "body"))
            )
            
            # Wait 5 seconds for page to fully initialize
            time.sleep(5)
            
            # Handle privacy consent popup if present
            consent_clicked = False
            try:
                privacy_container = driver.find_element(By.ID, 'popin_tc_privacy_container_button')
                if privacy_container:
                    # Find all buttons inside the privacy container
                    buttons = privacy_container.find_elements(By.TAG_NAME, 'button')
                    if len(buttons) >= 2:
                        # Click the 2nd button (index 1)
                        driver.execute_script("arguments[0].scrollIntoView(true);", buttons[1])
                        time.sleep(0.2)
                        buttons[1].click()
                        consent_clicked = True
                        print("Privacy consent clicked")
                        time.sleep(2)
            except (NoSuchElementException, IndexError):
                # Privacy container not found or doesn't have enough buttons, continue
                pass
            
            if not consent_clicked:
                print("Privacy consent not found or not clicked")
            
            # Handle modal if present
            modal_clicked = False
            try:
                modal = driver.find_element(By.CSS_SELECTOR, 'div.modal')
                if modal:
                    # Find the first button inside the modal
                    modal_buttons = modal.find_elements(By.TAG_NAME, 'button')
                    if len(modal_buttons) >= 1:
                        driver.execute_script("arguments[0].scrollIntoView(true);", modal_buttons[0])
                        time.sleep(0.2)
                        modal_buttons[0].click()
                        modal_clicked = True
                        print("Modal clicked")
                        time.sleep(2)
            except (NoSuchElementException, IndexError):
                # Modal not found or doesn't have buttons, continue
                pass
            
            if not modal_clicked:
                print("Modal not found or not clicked")
            
            # Find all "see more" buttons (buttons with class containing 'seeMore' or 'is-seeMore')
            see_more_buttons = driver.find_elements(
                By.CSS_SELECTOR, 
                "button.is-seeMore, button[class*='seeMore'], button[class*='see-more']"
            )
            
            print(f"Found {len(see_more_buttons)} 'see more' button(s)")
            
            # Click all "see more" buttons
            for button in see_more_buttons:
                try:
                    # Wait for button to be clickable
                    WebDriverWait(driver, 1).until(
                        EC.element_to_be_clickable(button)
                    )
                    
                    # Scroll to button to ensure it's visible
                    driver.execute_script("arguments[0].scrollIntoView({block: 'center'});", button)
                    time.sleep(0.3)  # Small delay for scroll
                    
                    # Try to click using JavaScript (more reliable for dynamic content)
                    driver.execute_script("arguments[0].click();", button)
                    time.sleep(0.5)  # Wait for content to expand
                except (TimeoutException, Exception):
                    # Continue if button click fails (might be already clicked or not clickable)
                    continue
            
            # Wait a bit more for any dynamic content to load
            time.sleep(1)
            
            # Get the final HTML
            html = driver.page_source
            
            return html
            
        finally:
            if driver:
                driver.quit()
    
    def _parse_single_market(self, market_element) -> Optional[BookmakerEvent | List[BookmakerEvent]]:
        """Parse a single market element.
        
        Parameters
        ----------
        market_element : Tag
            BeautifulSoup tag for a single market.
            
        Returns
        -------
        Optional[BookmakerEvent | List[BookmakerEvent]]
            Parsed event(s) or None if parsing fails. Returns list for goalscorer markets.
        """
        # Extract title
        title_elem = market_element.find('h2', class_='marketBox_headTitle')
        if not title_elem:
            return None
        
        title = title_elem.get_text(strip=True)

        # Check for different market structures
        matrix_markets = market_element.find('sports-matrix-markets')
        split_cards = market_element.find_all('sports-split-card')
        spaced_blocks = market_element.find('sports-spaced-blocks')
        
        options = []
        
        if split_cards:
            # Goalscorer market - split by teams, create separate events per team
            events = []
            for split_card in split_cards:
                team_title_elem = split_card.find('div', class_='marketBox_bodyTitle')
                if not team_title_elem:
                    continue
                
                team_name = team_title_elem.get_text(strip=True)
                card_options = self._parse_matrix_options(split_card)
                
                if card_options:
                    events.append(BookmakerEvent(
                        title=f"{title} - {team_name}",
                        options=card_options
                    ))
            
            return events if events else None
        elif spaced_blocks:
            # Handicap market
            options = self._parse_matrix_options(spaced_blocks)
        elif matrix_markets:
            # Standard matrix market
            options = self._parse_matrix_options(matrix_markets)
        else:
            return None
        
        if not options:
            return None

        return BookmakerEvent(
            title=title,
            options=options,
        )
    
    def _parse_grouped_market(self, market_element) -> List[BookmakerEvent]:
        """Parse a grouped market element (e.g., first/last goal).
        
        Parameters
        ----------
        market_element : Tag
            BeautifulSoup tag for a grouped market.
            
        Returns
        -------
        List[BookmakerEvent]
            List of parsed events from the grouped market.
        """
        events = []
        
        # Extract main title
        title_elem = market_element.find('h2', class_='marketBox_headTitle')
        if not title_elem:
            return events
        
        main_title = title_elem.get_text(strip=True)
        
        # Extract sub-market types
        sub_market_items = market_element.find_all('span', class_='marketBox_itemValue')
        sub_markets = [item.get_text(strip=True) for item in sub_market_items]
        
        # Extract options by row
        line_selections = market_element.find_all('div', class_='marketBox_lineSelection')
        
        for line_selection in line_selections:
            label_elem = line_selection.find('p', class_='marketBox_label')
            if not label_elem:
                continue
            
            label = label_elem.get_text(strip=True)
            
            # Get odds for each sub-market
            market_items = line_selection.find_all('div', class_='marketBox_item')
            if len(market_items) != len(sub_markets):
                continue
            
            for i, market_item in enumerate(market_items):
                if i >= len(sub_markets):
                    break
                
                odds_elem = market_item.find('span', class_='btn_label')
                if not odds_elem:
                    continue
                
                odds_str = odds_elem.get_text(strip=True)
                odds = self._parse_odds(odds_str)
                if odds is None:
                    continue
                
                events.append(BookmakerEvent(
                    title=f"{main_title}",
                    options=[EventOption(label=label, odds=odds)],
                ))
        
        return events
    
    def _parse_grouped_market_from_box(self, market_box, title: str) -> List[BookmakerEvent]:
        """Parse a grouped market from a marketBox element.
        
        Parameters
        ----------
        market_box : Tag
            BeautifulSoup tag for a marketBox element with grouped market structure.
        title : str
            The market title (already extracted).
            
        Returns
        -------
        List[BookmakerEvent]
            List of parsed events from the grouped market.
        """
        events = []
        
        # Extract sub-market types
        sub_market_items = market_box.find_all('span', class_='marketBox_itemValue')
        sub_markets = [item.get_text(strip=True) for item in sub_market_items]
        
        # Extract options by row
        line_selections = market_box.find_all('div', class_='marketBox_lineSelection')
        
        for line_selection in line_selections:
            label_elem = line_selection.find('p', class_='marketBox_label')
            if not label_elem:
                continue
            
            label = label_elem.get_text(strip=True)
            
            # Get odds for each sub-market
            market_items = line_selection.find_all('div', class_='marketBox_item')
            if len(market_items) != len(sub_markets):
                continue
            
            for i, market_item in enumerate(market_items):
                if i >= len(sub_markets):
                    break
                
                odds_elem = market_item.find('span', class_='btn_label')
                if not odds_elem:
                    continue
                
                odds_str = odds_elem.get_text(strip=True)
                odds = self._parse_odds(odds_str)
                if odds is None:
                    continue
                
                events.append(BookmakerEvent(
                    title=title,
                    options=[EventOption(label=label, odds=odds)],
                ))
        
        return events
    
    def _parse_matrix_options(self, container) -> List[EventOption]:
        """Parse options from a matrix markets container.
        
        Parameters
        ----------
        container : Tag
            BeautifulSoup tag containing market options.
            
        Returns
        -------
        List[EventOption]
            List of parsed options.
        """
        options = []
        line_selections = container.find_all('div', class_='marketBox_lineSelection')
        
        for line_selection in line_selections:
            label_elem = line_selection.find('p', class_='marketBox_label')
            if not label_elem:
                # For some markets, label might be in btn_label with is-top class
                # Try to find it from button structure
                button = line_selection.find('button', class_='btn')
                if button:
                    top_label_spans = button.find_all('span', class_='btn_label')
                    for span in top_label_spans:
                        if 'is-top' in span.get('class', []):
                            label_elem = span
                            break
            
            if not label_elem:
                continue
            
            label = label_elem.get_text(strip=True)
            
            # Find odds button - exclude spans with is-top class
            odds_elem = None
            label_spans = line_selection.find_all('span', class_='btn_label')
            for span in label_spans:
                if 'is-top' not in span.get('class', []):
                    odds_elem = span
                    break
            
            if not odds_elem:
                continue
            
            odds_str = odds_elem.get_text(strip=True)
            odds = self._parse_odds(odds_str)
            if odds is not None:
                options.append(EventOption(label=label, odds=odds))
        
        return options
      
    def _parse_odds(self, odds_str: str) -> Optional[float]:
        """Parse odds string to float.
        
        Converts comma-separated decimal to float (e.g., "1,48" -> 1.48).
        
        Parameters
        ----------
        odds_str : str
            Odds as string (may contain comma as decimal separator).
            
        Returns
        -------
        Optional[float]
            Parsed odds as float, or None if parsing fails.
        """
        if not odds_str:
            return None
        
        try:
            # Replace comma with dot for European format
            normalized = odds_str.replace(',', '.').strip()
            return float(normalized)
        except (ValueError, AttributeError):
            return None
    
    def _aggregate_events(self, events: List[BookmakerEvent]) -> List[BookmakerEvent]:
        """Aggregate events of the same type into single events.
        
        Groups events by title, then merges their options.
     
        Parameters
        ----------
        events : List[BookmakerEvent]
            List of events to aggregate.
            
        Returns
        -------
        List[BookmakerEvent]
            Aggregated list of events.
        """
        if not events:
            return []
        
        grouped = {}
        
        for event in events:
            # Create a grouping key from title
            group_key = (event.title)
            
            if group_key not in grouped:
                grouped[group_key] = []
            grouped[group_key].append(event)
        
        # Merge events in each group
        aggregated = []
        for group_key, group_events in grouped.items():
            if len(group_events) == 1:
                # No need to aggregate if only one event
                aggregated.append(group_events[0])
            else:
                # Merge multiple events
                merged_event = self._merge_events(group_events)
                aggregated.append(merged_event)
        
        return aggregated
       
    def _merge_events(self, events: List[BookmakerEvent]) -> BookmakerEvent:
        """Merge multiple events of the same type into a single event.
        
        Parameters
        ----------
        events : List[BookmakerEvent]
            List of events to merge
            
        Returns
        -------
        BookmakerEvent
            Merged event with combined options.
        """
        if not events:
            raise ValueError("Cannot merge empty event list")
        
        if len(events) == 1:
            return events[0]
        
        # Use the first event as base
        base_event = events[0]
        
        # Collect all options, using (label, odds) as key to handle same label with different odds
        options_dict = {}
        for event in events:
            for option in event.options:
                # Use (label, odds) tuple as key to preserve options with same label but different odds
                key = (option.label, option.odds)
                if key not in options_dict:
                    options_dict[key] = option
        
        # Keep the original title without modification
        original_title = base_event.title
        
        # Sort options by label for consistent output
        sorted_options = sorted(options_dict.values(), key=lambda x: x.label)
        
        # Create merged event
        return BookmakerEvent(
            title=original_title,
            options=sorted_options,
        )
    


