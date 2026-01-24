import logging

logger = logging.getLogger(__name__)

class PREMIER_LEAGUE:
    """Premier League club name constants."""

    # Club names as they appear in SoccerData API
    ARSENAL = "Arsenal"
    MANCHESTER_CITY = "Manchester City"
    ASTON_VILLA = "Aston Villa"
    LIVERPOOL = "Liverpool"
    BRENTFORD = "Brentford"
    NEWCASTLE_UTD = "Newcastle United"
    MANCHESTER_UTD = "Manchester United"
    CHELSEA = "Chelsea"
    FULHAM = "Fulham"
    SUNDERLAND = "Sunderland"
    BRIGHTON = "Brighton & Hove Albion"
    EVERTON = "Everton"
    CRYSTAL_PALACE = "Crystal Palace"
    TOTTENHAM = "Tottenham Hotspur"
    BOURNEMOUTH = "AFC Bournemouth"
    LEEDS_UNITED = "Leeds United"
    NOTTINGHAM_FOREST = "Nottingham Forest"
    WEST_HAM = "West Ham United"
    BURNLEY = "Burnley"
    WOLVES = "Wolverhampton Wanderers"
    
    SOCCERDATA_CURRENT_SEASON = "2025-2026"
    SOCCERDATA_ENGLAND_ID = 8
    SOCCERDATA_PREMIER_LEAGUE_ID = 228

    ALL_CLUBS_FBREF = [
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

class FOOTBALL:
    """
    Provides utilities for football position code mapping and lookup.
    """

    POSITIONS_MAP = {
        "GK": "Goalkeeper",
        "DL": "Left Back / Left Defender",
        "DR": "Right Back / Right Defender",
        "DC": "Center Back / Central Defender",
        "DMC": "Defensive Midfielder / Central Defensive Midfielder",
        "DM": "Defensive Midfielder",
        "ML": "Left Midfielder / Left Wing Midfielder",
        "MR": "Right Midfielder / Right Wing Midfielder",
        "MC": "Central Midfielder",
        "AMC": "Attacking Midfielder / Central Attacking Midfielder",
        "AML": "Attacking Midfielder / Left Attacking Midfielder",
        "AMR": "Attacking Midfielder / Right Attacking Midfielder",
        "LW": "Left Winger / Left Forward",
        "RW": "Right Winger / Right Forward",
        "FW": "Forward / Striker",
        "ST": "Striker / Center Forward",
        "M": "Midfielder",
        "D": "Defender",
        "F": "Forward",
        "F/M": "Forward / Midfielder",
        "G": "Goalkeeper"
    }
    
    @classmethod
    def get(cls, acronym: str) -> str:
        """
        Return the full position name for a given football position acronym.

        Parameters
        ----------
        acronym : str
            The position acronym (e.g., "GK", "DL", "ST").

        Returns
        -------
        str
            The full descriptive name of the position if found, otherwise returns the acronym.
        """
        if acronym not in cls.POSITIONS_MAP:
            logger.error(f"No matching position found for acronym: {acronym}")
            return acronym
        return cls.POSITIONS_MAP[acronym]
