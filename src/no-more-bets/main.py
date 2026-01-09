from services.fbref import FBref
from services.betclic import Betclic
from utils.utils import print_events, print_clubs, print_games, print_players
from constants import PREMIER_LEAGUE 
from pprint import pprint
from dotenv import load_dotenv
from agents.sample_agent import Agent

def main(): 
    # agent = Agent()
    # agent.run_conversation_loop()
    scraper = FBref()
    
    club_name = PREMIER_LEAGUE.ARSENAL
    #league_stats = scraper.get_premier_league_stats()
    #print_clubs(league_stats)
    #print(f"--------------------------------")

    players = scraper.get_club_players(club_name)
    print_players(players)
    print(f"--------------------------------")
    #games = scraper.get_club_games(club_name)
    #print_games(games)
    #print(f"--------------------------------")

    # betclic = Betclic()
    # upcomming_games = betclic.get_upcoming_games()
    # for game in upcomming_games:
    #     print(f'{game.away_team} vs {game.home_team} {game.time}')
    #     events = betclic.get_match_events(game.url, expand=False)
    #     print_events(events)
    
    # pprint(html)

if __name__ == "__main__":
    load_dotenv()
    main()
