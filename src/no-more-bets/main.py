import asyncio
import logging
from dotenv import load_dotenv
from agents.group_chat import run_betting_analysis
from services.fbref import FBref
from services.betclic import Betclic
from constants import PREMIER_LEAGUE
from pprint import pprint
from utils.utils import print_events
from services.soccerdata import SoccerData
import os

# Configure logging to display logs in console
logging.basicConfig(
    level=logging.DEBUG,  # Set to logging.DEBUG for more verbose output
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)

def soccer():
    soccerdata = SoccerData()
    soccerdata.get_matches(league_id=PREMIER_LEAGUE.SOCCERDATA_PREMIER_LEAGUE_ID, season=PREMIER_LEAGUE.SOCCERDATA_CURRENT_SEASON)

def main():
    betclic = Betclic(cache_ttl=9999999999999999999999999999999999999999999, delay=10, retry_delay=20, n_retries=10, timeout=60)
    games = betclic.get_upcoming_games()
    pprint("Upcoming games: " + str(len(games)))
    all_events = []
    for game in games:
        events = betclic.get_match_events(game.url, expand=False)
        all_events.extend(events)
        print("Game: " + game.url + " - Events: " + str(len(events)))

    print_events(all_events[:3])
    # Now all_events contains all events from all games
        #print_events(events)
    #load_dotenv()
    #result = asyncio.run(run_betting_analysis("Analyze Leeds vs Fulham", verbose=True))
    #print(result)

if __name__ == "__main__":
    load_dotenv()
    #main()
    soccer()
