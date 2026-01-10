import asyncio
from dotenv import load_dotenv
from agents.group_chat import run_betting_analysis
from services.fbref import FBref
from services.betclic import Betclic
from constants import PREMIER_LEAGUE
from pprint import pprint
from utils.utils import print_events

def main():
    betclic = Betclic()
    games = betclic.get_upcoming_games()
    pprint(games)
    for game in games[:1]:
        print(game.url)
        events = betclic.get_match_events(game.url, expand=False)
        print_events(events)
    #load_dotenv()
    #result = asyncio.run(run_betting_analysis("Analyze Leeds vs Fulham", verbose=True))
    #print(result)

if __name__ == "__main__":
    load_dotenv()
    main()
