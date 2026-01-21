"""Script to populate test fixtures with real data from services.

This script makes real API calls and scrapes real HTML to populate
the fixtures directory with actual data for use in tests.

Usage:
    python tests/fixtures/populate_fixtures.py

Note: This requires valid API keys and network access.
"""
import sys
import logging
from pathlib import Path

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)

logger = logging.getLogger(__name__)

# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent.parent / 'src' / 'no-more-bets'))

from services.betclic import Betclic
from services.rotowire import Rotowire
from services.soccerdata import SoccerData
from services.web_search import WebSearch
from config import Config


def populate_betclic_fixtures(fixtures_dir: Path):
    """Populate Betclic fixtures."""
    logger.info("Populating Betclic fixtures...")
    betclic_dir = fixtures_dir / "betclic"
    betclic_dir.mkdir(exist_ok=True)
    
    scraper = Betclic(use_cache=False, store=False)
    
    try:
        # Get Premier League page
        html = scraper.get_premier_league_html()
        (betclic_dir / "premier_league_page.html").write_text(html, encoding='utf-8')
        logger.info("  [OK] Saved premier_league_page.html")
        
        # Get upcoming games to find a match URL
        games = scraper.get_upcoming_games()
        if games and games[0].url:
            match_html = scraper._get_page_html_selenium(games[0].url)
            (betclic_dir / "match_page.html").write_text(match_html, encoding='utf-8')
            logger.info(f"  [OK] Saved match_page.html (from {games[0].url})")
    except Exception as e:
        logger.error(f"  [ERROR] Error populating Betclic fixtures: {e}")


def populate_rotowire_fixtures(fixtures_dir: Path):
    """Populate Rotowire fixtures."""
    logger.info("Populating Rotowire fixtures...")
    rotowire_dir = fixtures_dir / "rotowire"
    rotowire_dir.mkdir(exist_ok=True)
    
    scraper = Rotowire(use_cache=False, store=False)
    
    try:
        url = f"{scraper.base_url}/soccer/lineups.php"
        html = scraper._get_page_html(url)
        (rotowire_dir / "lineups_page.html").write_text(html, encoding='utf-8')
        logger.info("  [OK] Saved lineups_page.html")
    except Exception as e:
        logger.error(f"  [ERROR] Error populating Rotowire fixtures: {e}")


def populate_soccerdata_fixtures(fixtures_dir: Path):
    """Populate SoccerData fixtures."""
    logger.info("Populating SoccerData fixtures...")
    soccerdata_dir = fixtures_dir / "soccerdata"
    soccerdata_dir.mkdir(exist_ok=True)
    
    import json
    
    # Check for API key from Config
    api_key = Config.SOCCERDATA_API_KEY
    if not api_key:
        logger.warning("  [WARN] SOCCERDATA_API_KEY not set, skipping SoccerData fixtures")
        return
    
    try:
        service = SoccerData(use_cache=False, store_cache=False)
        
        # Get match previews upcoming
        previews = service.get_match_previews_upcoming()
        if previews:
            previews_data = [p.model_dump() for p in previews]
            (soccerdata_dir / "match_previews_upcoming.json").write_text(
                json.dumps(previews_data, indent=2, default=str),
                encoding='utf-8'
            )
            logger.info("  [OK] Saved match_previews_upcoming.json")
            
            # Get a specific match preview if available
            if previews and previews[0].match_previews and previews[0].match_previews[0].id:
                match_id = previews[0].match_previews[0].id
                try:
                    match_preview = service.get_match_preview(match_id)
                    (soccerdata_dir / "match_preview.json").write_text(
                        json.dumps(match_preview.model_dump(), indent=2, default=str),
                        encoding='utf-8'
                    )
                    logger.info("  [OK] Saved match_preview.json")
                except Exception as e:
                    logger.warning(f"  [WARN] Could not fetch match preview {match_id}: {e}")
        
        # Get head-to-head if we have team IDs
        # Using example team IDs (Arsenal=2916, Chelsea=4148)
        try:
            h2h = service.get_head_to_head(2916, 4148)
            (soccerdata_dir / "head_to_head.json").write_text(
                json.dumps(h2h.model_dump(), indent=2, default=str),
                encoding='utf-8'
            )
            logger.info("  [OK] Saved head_to_head.json")
        except Exception as e:
            logger.warning(f"  [WARN] Could not fetch head-to-head: {e}")
        
        # Get matches for Premier League (league_id=39)
        try:
            matches = service.get_matches(league_id=39)
            matches_data = [m.model_dump() for m in matches]
            (soccerdata_dir / "matches_league.json").write_text(
                json.dumps(matches_data, indent=2, default=str),
                encoding='utf-8'
            )
            logger.info("  [OK] Saved matches_league.json")
        except Exception as e:
            logger.warning(f"  [WARN] Could not fetch matches: {e}")
    except Exception as e:
        logger.error(f"  [ERROR] Error populating SoccerData fixtures: {e}")


def populate_web_search_fixtures(fixtures_dir: Path):
    """Populate WebSearch fixtures (using sample data)."""
    logger.info("Populating WebSearch fixtures...")
    web_search_dir = fixtures_dir / "web_search"
    web_search_dir.mkdir(exist_ok=True)
    
    import json
    
    # Create sample search results (since DDGS results are non-deterministic)
    sample_text_results = [
        {
            "title": "Arsenal vs Chelsea Preview",
            "href": "https://www.bbc.com/sport/football/12345",
            "body": "Arsenal host Chelsea in a crucial Premier League match...",
            "date": "2025-01-15"
        },
        {
            "title": "Premier League Match Report",
            "href": "https://www.skysports.com/football/news/12345",
            "body": "Arsenal secured a vital victory over Chelsea...",
            "date": "2025-01-16"
        }
    ]
    
    sample_news_results = [
        {
            "title": "Arsenal Transfer News",
            "url": "https://www.theguardian.com/football/12345",
            "body": "Arsenal are reportedly interested in signing...",
            "date": "2025-01-15",
            "image": "https://example.com/image.jpg",
            "source": "The Guardian"
        }
    ]
    
    (web_search_dir / "search_results_sample.json").write_text(
        json.dumps(sample_text_results, indent=2),
        encoding='utf-8'
    )
    logger.info("  [OK] Saved search_results_sample.json")
    
    (web_search_dir / "news_results_sample.json").write_text(
        json.dumps(sample_news_results, indent=2),
        encoding='utf-8'
    )
    logger.info("  [OK] Saved news_results_sample.json")


def main():
    """Main function to populate all fixtures."""
    fixtures_dir = Path(__file__).parent
    
    logger.info("=" * 60)
    logger.info("Populating Test Fixtures")
    logger.info("=" * 60)
    logger.info("")
    
    populate_betclic_fixtures(fixtures_dir)
    logger.info("")
    
    populate_rotowire_fixtures(fixtures_dir)
    logger.info("")
    
    populate_soccerdata_fixtures(fixtures_dir)
    logger.info("")
    
    populate_web_search_fixtures(fixtures_dir)
    logger.info("")
    
    logger.info("=" * 60)
    logger.info("Fixture population complete!")
    logger.info("=" * 60)


if __name__ == "__main__":
    main()
