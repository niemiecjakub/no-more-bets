import time
import logging
import hashlib
from bs4 import BeautifulSoup
from curl_cffi import requests
from .base_scraper import BaseScraper
from models.premierinjuries import InjuryData, PlayerInjury, TeamInjury

logger = logging.getLogger(__name__)


class PremierInjuries(BaseScraper):
    """Premier Injuries scraper using curl_cffi browser impersonation."""

    def __init__(
        self,
        impersonate: str = "chrome120",
        delay: float = 3.0,
        retry_count: int = 3,
        retry_delay: float = 2.0,
        timeout: float = 15.0,
        store: bool = True,
        use_cache: bool = True,
        cache_ttl: float = 3600.0,
    ):
        super().__init__(
            impersonate,
            delay,
            retry_count,
            retry_delay,
            timeout,
            store,
            use_cache,
            cache_ttl,
        )
        self.base_url = "https://www.premierinjuries.com"

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def get_premier_league_injuries_html(self) -> InjuryData:
        """Fetch and parse Premier League injuries page.
        
        Returns
        -------
        InjuryData
            Parsed injury data from Premier League website.
        """
        url = "https://www.premierleague.com/en/latest-player-injuries"
        html = self._get_page_html(url)
        
        # Ensure HTML is saved to cache
        if self.cache.store:
            self.cache.save(url, html)
        
        return self._parse_premier_league_injuries(html)

    def get_injury_table(self) -> InjuryData:
        """Fetch and parse Premier League injury table."""
        url = f"{self.base_url}/injury-table.php"

        #if self.cache.use_cache:
        #    cached_html = self.cache.load(url)
        #    if cached_html:
        #        return self._parse_injury_table(cached_html)

        html = self._fetch_html(url)

        if self.cache.store:
            self.cache.save(url, html)

        return self._parse_injury_table(html)

    # ------------------------------------------------------------------
    # Fetching
    # ------------------------------------------------------------------

    def _fetch_html(self, url: str) -> str:
        last_exception = None

        for attempt in range(1, self.retry_count + 1):
            self._rate_limit()

            try:
                with requests.Session() as session:
                    # 1️⃣ Warm up Cloudflare session (very important)
                    session.get(
                        self.base_url,
                        impersonate=self.impersonate,
                        timeout=self.timeout,
                        headers={
                            "accept-language": "en-US,en;q=0.9",
                            "referer": "https://www.google.com/",
                        },
                    )

                    time.sleep(1.5)

                    # 2️⃣ Fetch the real page using same cookies
                    response = session.get(
                        url,
                        impersonate=self.impersonate,
                        timeout=self.timeout,
                        headers={
                            "accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
                            "accept-language": "en-US,en;q=0.9",
                            "referer": self.base_url + "/",
                        },
                    )

                    response.raise_for_status()
                    self.last_fetch_time = time.time()
                    return response.text

            except Exception as e:
                last_exception = e
                logger.warning(
                    f"Fetch attempt {attempt}/{self.retry_count} failed: {e}"
                )

                if attempt < self.retry_count:
                    time.sleep(self.retry_delay * attempt)

        raise RuntimeError(
            f"Failed to fetch {url} after {self.retry_count} attempts"
        ) from last_exception


    # ------------------------------------------------------------------
    # Parsing
    # ------------------------------------------------------------------

    def _parse_premier_league_injuries(self, html: str) -> InjuryData:
        """Parse Premier League injuries HTML structure.
        
        The HTML structure:
        - Team names are in <h2> tags
        - Each team has a following table with class "article__table article__table-scrollable"
        - Tables have columns: Player, Injury, Latest
        """
        soup = BeautifulSoup(html, "lxml")
        teams = []
        
        # Find the main content container
        content = soup.find("div", class_="article__content")
        if not content:
            logger.warning("Article content not found")
            return InjuryData(teams=[])
        
        # Find all team headings (h2 tags)
        team_headings = content.find_all("h2")
        
        for i, heading in enumerate(team_headings):
            team_name = heading.get_text(strip=True)
            if not team_name:
                continue
            
            # Generate team_id from team name hash (consistent across runs)
            # Use SHA256 hash for deterministic team_id
            team_id = int(hashlib.sha256(team_name.encode()).hexdigest()[:8], 16) % (10 ** 8)
            
            # Find the next table after this heading
            # The table can be either:
            # 1. Directly after h2 (in actual HTML)
            # 2. Inside a div with class "article__table" (in some HTML versions)
            next_heading = None
            if i + 1 < len(team_headings):
                next_heading = team_headings[i + 1]
            
            # First, try to find table directly (most common case)
            table = None
            if next_heading:
                # Find all tables between current heading and next heading
                all_next_tables = heading.find_all_next("table")
                for tbl in all_next_tables:
                    # Check if this table comes before the next heading
                    if tbl in next_heading.find_all_previous():
                        table = tbl
                        break
            else:
                # Last team, just find next table
                table = heading.find_next("table")
            
            # If no direct table found, try finding it in a div wrapper
            if not table:
                table_div = None
                if next_heading:
                    all_next_divs = heading.find_all_next("div")
                    for div in all_next_divs:
                        if div in next_heading.find_all_previous():
                            classes = div.get("class", [])
                            if isinstance(classes, list) and "article__table" in classes:
                                table_div = div
                                break
                else:
                    table_div = heading.find_next("div", class_=lambda x: x and isinstance(x, list) and "article__table" in x)
                
                if table_div:
                    table = table_div.find("table")
            
            players = []
            
            if not table:
                logger.warning(f"No table found for team: {team_name}")
            else:
                tbody = table.find("tbody")
                if not tbody:
                    logger.warning(f"No tbody found for team: {team_name}")
                else:
                    rows = tbody.find_all("tr")
                    logger.debug(f"Found {len(rows)} rows for team: {team_name}")
                    for row in rows:
                        # Extract player name from <th> (first cell)
                        player_cell = row.find("th")
                        if not player_cell:
                            continue
                        
                        player_name = player_cell.get_text(strip=True)
                        if not player_name:
                            continue
                        
                        # Extract all <td> cells (injury type and details)
                        cells = row.find_all("td")
                        
                        # First <td> contains injury type
                        injury_type = "-"
                        if len(cells) >= 1:
                            injury_type = cells[0].get_text(strip=True)
                        
                        # Second <td> contains details link
                        details_link = None
                        if len(cells) >= 2:
                            link_elem = cells[1].find("a")
                            if link_elem:
                                details_link = link_elem.get("href", "")
                            # If no link, check if it's just text "-"
                            elif cells[1].get_text(strip=True) != "-":
                                details_link = cells[1].get_text(strip=True)
                        
                        # Clean up injury type
                        if injury_type == "-" or not injury_type:
                            injury_type = "Unknown"
                        
                        players.append(
                            PlayerInjury(
                                player=player_name,
                                reason=injury_type,
                                further_detail=details_link if details_link and details_link != "-" else None,
                                potential_return=None,
                                condition=None,
                                status=None,
                                team_id=team_id,
                            )
                        )
            
            teams.append(
                TeamInjury(
                    team_name=team_name,
                    team_id=team_id,
                    injury_count=len(players) if players else None,
                    players=players,
                )
            )
        
        return InjuryData(teams=teams)

    def _parse_injury_table(self, html: str) -> InjuryData:
        soup = BeautifulSoup(html, "lxml")
        teams = []

        injury_table = soup.find("table", class_="injury-table")
        if not injury_table:
            logger.warning("Injury table not found")
            return InjuryData(teams=[])

        tbody = injury_table.find("tbody")
        if not tbody:
            logger.warning("Injury table body not found")
            return InjuryData(teams=[])

        heading_rows = tbody.find_all("tr", class_="heading")

        for heading_row in heading_rows:
            team_id_attr = heading_row.get("data-team-id")
            if not team_id_attr:
                continue

            try:
                team_id = int(team_id_attr)
            except ValueError:
                continue

            team_name_elem = heading_row.find("div", class_="injury-team")
            team_name = team_name_elem.get_text(strip=True) if team_name_elem else ""

            injury_count = None
            count_elem = heading_row.find("span", class_="injury-count2-num")
            if count_elem:
                try:
                    injury_count = int(count_elem.get_text(strip=True))
                except ValueError:
                    pass

            player_rows = tbody.find_all(
                "tr",
                class_=lambda c: c
                and f"team_{team_id}" in c
                and "player-row" in c,
            )

            players = []

            for row in player_rows:
                if "team-ad-slot" in row.get("class", []):
                    continue

                cells = row.find_all("td")
                if len(cells) < 6:
                    continue

                player = self._extract_text_after_mob_title(cells[0])
                reason = self._extract_text_after_mob_title(cells[1])
                detail = self._extract_text_after_mob_title(cells[2])
                return_date = self._extract_text_after_mob_title(cells[3])
                condition = self._extract_text_after_mob_title(cells[4])
                status = self._extract_text_after_mob_title(cells[5])

                players.append(
                    PlayerInjury(
                        player=player,
                        reason=reason,
                        further_detail=detail or None,
                        potential_return=return_date or None,
                        condition=condition or None,
                        status=status or None,
                        team_id=team_id,
                    )
                )

            teams.append(
                TeamInjury(
                    team_name=team_name,
                    team_id=team_id,
                    injury_count=injury_count,
                    players=players,
                )
            )

        return InjuryData(teams=teams)

    # ------------------------------------------------------------------
    # Helpers
    # ------------------------------------------------------------------

    def _extract_text_after_mob_title(self, cell) -> str:
        if not cell:
            return ""

        clone = BeautifulSoup(str(cell), "lxml").find("td")
        if not clone:
            return ""

        for mob in clone.find_all("div", class_="mob-title"):
            mob.decompose()

        return clone.get_text(separator=" ", strip=True)
