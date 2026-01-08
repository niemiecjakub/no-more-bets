"""Constants for Premier League clubs."""


class PREMIER_LEAGUE:
    """Premier League club name constants.
    
    These constants contain the exact club names as they appear on fbref.com,
    which are used for matching when scraping club data.
    
    Usage:
        from constants import PREMIER_LEAGUE
        scraper.get_club_games(PREMIER_LEAGUE.MANCHESTER_CITY)
        scraper.get_club_players(PREMIER_LEAGUE.ARSENAL)
    """
    
    # Club names as they appear on fbref.com
    ARSENAL = "Arsenal"
    MANCHESTER_CITY = "Manchester City"
    ASTON_VILLA = "Aston Villa"
    LIVERPOOL = "Liverpool"
    BRENTFORD = "Brentford"
    NEWCASTLE_UTD = "Newcastle Utd"
    MANCHESTER_UTD = "Manchester Utd"
    CHELSEA = "Chelsea"
    FULHAM = "Fulham"
    SUNDERLAND = "Sunderland"
    BRIGHTON = "Brighton"
    EVERTON = "Everton"
    CRYSTAL_PALACE = "Crystal Palace"
    TOTTENHAM = "Tottenham"
    BOURNEMOUTH = "Bournemouth"
    LEEDS_UNITED = "Leeds United"
    NOTTINGHAM_FOREST = "Nott'ham Forest"
    WEST_HAM = "West Ham"
    BURNLEY = "Burnley"
    WOLVES = "Wolves"
    
    # List of all clubs for iteration
    ALL_CLUBS = [
        ARSENAL,
        MANCHESTER_CITY,
        ASTON_VILLA,
        LIVERPOOL,
        BRENTFORD,
        NEWCASTLE_UTD,
        MANCHESTER_UTD,
        CHELSEA,
        FULHAM,
        SUNDERLAND,
        BRIGHTON,
        EVERTON,
        CRYSTAL_PALACE,
        TOTTENHAM,
        BOURNEMOUTH,
        LEEDS_UNITED,
        NOTTINGHAM_FOREST,
        WEST_HAM,
        BURNLEY,
        WOLVES,
    ]

