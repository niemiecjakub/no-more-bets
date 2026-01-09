from fbref import FBref
from betclic import Betclic
from constants import PREMIER_LEAGUE
from pprint import pprint

def main():
    #scraper = FBref()
    
    #club_name = PREMIER_LEAGUE.ARSENAL
    # league_stats = scraper.get_premier_league_stats()
    #players = scraper.get_club_players(club_name)
    #games = scraper.get_club_games(club_name)
    #pprint(players)

    betclic = Betclic()
    html = betclic.get_upcoming_games()
    pprint(html)

if __name__ == "__main__":
    main()
