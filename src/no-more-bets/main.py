import logging
from typing import List
from dotenv import load_dotenv
from constants import PREMIER_LEAGUE
from models.match_analysis import MatchAnalysis
from services.betclic import Betclic
from services.fbref import FBref
from services.rotowire import Rotowire
from services.soccerdata import SoccerData
from services.match_analysis_orchestrator import MatchAnalysisOrchestrator
from output.match_analysis_persistence import MatchAnalysisPersistence
from output.match_analysis_output import ConsoleOutput, SilentOutput


logging.basicConfig(
    level=logging.INFO, 
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)


def main() -> List[MatchAnalysis]:
    rotowire = Rotowire()
    soccerdata = SoccerData()
    bookmaker = Betclic(cache_ttl=9999999999999999999999999999999999999999999, delay=10, retry_delay=20, n_retries=10, timeout=60)
    fbref = FBref(cache_ttl=36000000000)
    persistence = MatchAnalysisPersistence()
    output = ConsoleOutput()

    orchestrator = MatchAnalysisOrchestrator(
        rotowire=rotowire,
        soccerdata=soccerdata,
        bookmaker=bookmaker,
        fbref=fbref,
        output_handler=output,
        persistence=persistence,
        league_id=PREMIER_LEAGUE.SOCCERDATA_PREMIER_LEAGUE_ID,
    )
    
    return orchestrator.analyze_matches()


if __name__ == "__main__":
    load_dotenv()
    main()