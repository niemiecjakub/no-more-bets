from typing import List
from models.bookmaker_event import BookmakerEvent
from models.club import Club
from models.player import Player
from models.game import Game


def print_green(message: str) -> None:
    """Print a message in green color to the console.
    
    Parameters
    ----------
    message : str
        The message to print in green.
    """
    print(f"\033[92m{message}\033[0m")


def print_events(events: List[BookmakerEvent]) -> None:
    """Print a list of bookmaker events in a nice, readable format.
    
    Parameters
    ----------
    events : List[BookmakerEvent]
        List of bookmaker events to print.
    """
    if not events:
        print("No events to display.")
        return
    
    print(f"\n{'=' * 80}")
    print(f"BOOKMAKER EVENTS ({len(events)} total)")
    print(f"{'=' * 80}\n")
    
    for idx, event in enumerate(events, 1):
        print(f"Event #{idx}")
        print(f"  Title: {event.title}")
        
        print(f"  Options ({len(event.options)}):")
        for option in event.options:
            print(f"    • {option.label:<30} Odds: {option.odds:.2f}")
        
        # Add separator between events (except for the last one)
        if idx < len(events):
            print()
    
    print(f"\n{'=' * 80}\n")


def print_clubs(clubs: List[Club]) -> None:
    """Print a list of clubs sorted by rank in a nice, readable format.
    
    Parameters
    ----------
    clubs : List[Club]
        List of clubs to print.
    """
    if not clubs:
        print("No clubs to display.")
        return
    
    # Sort by rank
    sorted_clubs = sorted(clubs, key=lambda c: c.rank)
    
    print(f"\n{'=' * 120}")
    print(f"CLUBS ({len(sorted_clubs)} total)")
    print(f"{'=' * 120}\n")
    
    # Header
    print(f"{'Rank':<6} {'Team':<25} {'GP':<5} {'W':<4} {'D':<4} {'L':<4} {'GF':<5} {'GA':<5} {'GD':<6} {'Pts':<5} {'Pts/G':<7} {'xG':<6} {'xGA':<6} {'xGD':<7} {'Last 5':<8}")
    print("-" * 120)
    
    for club in sorted_clubs:
        print(f"{club.rank:<6} {club.team:<25} {club.games:<5} {club.wins:<4} {club.ties:<4} {club.losses:<4} "
              f"{club.goals_for:<5} {club.goals_against:<5} {club.goal_diff:<6} {club.points:<5} "
              f"{club.points_avg:<7.2f} {club.xg_for:<6.2f} {club.xg_against:<6.2f} {club.xg_diff:<7.2f} "
              f"{club.last_5:<8}")
    
    print(f"\n{'=' * 120}\n")


def print_players(players: List[Player]) -> None:
    """Print a list of players in a nice, readable format.
    
    Parameters
    ----------
    players : List[Player]
        List of players to print.
    """
    if not players:
        print("No players to display.")
        return
    
    print(f"\n{'=' * 140}")
    print(f"PLAYERS ({len(players)} total)")
    print(f"{'=' * 140}\n")
    
    # Header
    print(f"{'Player':<25} {'Pos':<5} {'Age':<6} {'GP':<5} {'GS':<5} {'Min':<7} {'G':<4} {'A':<4} {'G+A':<5} "
          f"{'xG':<6} {'xA':<6} {'xG+xA':<8} {'G/90':<6} {'A/90':<6} {'G+A/90':<8}")
    print("-" * 140)
    
    for player in players:
        print(f"{player.player:<25} {player.position:<5} {player.age:<6} {player.games:<5} {player.games_starts:<5} "
              f"{player.minutes:<7} {player.goals:<4} {player.assists:<4} {player.goals_assists:<5} "
              f"{player.xg:<6.2f} {player.xg_assist:<6.2f} {player.npxg_xg_assist:<8.2f} "
              f"{player.goals_per90:<6.2f} {player.assists_per90:<6.2f} {player.goals_assists_per90:<8.2f}")
    
    print(f"\n{'=' * 140}\n")


def print_games(games: List[Game]) -> None:
    """Print a list of games in a nice, readable format.
    
    Parameters
    ----------
    games : List[Game]
        List of games to print.
    """
    if not games:
        print("No games to display.")
        return
    
    print(f"\n{'=' * 120}")
    print(f"GAMES ({len(games)} total)")
    print(f"{'=' * 120}\n")
    
    for idx, game in enumerate(games, 1):
        print(f"Game #{idx}")
        print(f"  Date: {game.date} {game.start_time} ({game.dayofweek})")
        print(f"  Competition: {game.comp} - {game.round}")
        print(f"  Venue: {game.venue}")
        print(f"  Opponent: {game.opponent}")
        
        if game.result:
            result_str = f"{game.result}"
            if game.goals_for is not None and game.goals_against is not None:
                result_str += f" ({game.goals_for}-{game.goals_against})"
            print(f"  Result: {result_str}")
        
        if game.xg_for is not None or game.xg_against is not None:
            xg_str = ""
            if game.xg_for is not None:
                xg_str += f"xG: {game.xg_for:.2f}"
            if game.xg_against is not None:
                if xg_str:
                    xg_str += " | "
                xg_str += f"xGA: {game.xg_against:.2f}"
            print(f"  {xg_str}")
        
        if game.possession is not None:
            print(f"  Possession: {game.possession}%")
        
        if game.formation:
            formation_str = f"Formation: {game.formation}"
            if game.opp_formation:
                formation_str += f" | Opponent: {game.opp_formation}"
            print(f"  {formation_str}")
        
        if game.captain:
            print(f"  Captain: {game.captain}")
        
        if game.referee:
            print(f"  Referee: {game.referee}")
        
        if game.attendance:
            print(f"  Attendance: {game.attendance:,}")
        
        if game.notes:
            print(f"  Notes: {game.notes}")
        
        # Add separator between games (except for the last one)
        if idx < len(games):
            print()
    
    print(f"\n{'=' * 120}\n")
