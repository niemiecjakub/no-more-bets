import asyncio
import logging
from typing import List
from constants import PREMIER_LEAGUE
from config import Config
from models.match_analysis import MatchAnalysis
from services.betclic import Betclic
from services.fotmob import FotMob
from services.rotowire import Rotowire
from services.soccerdata import SoccerData
from services.match_analysis_orchestrator import MatchAnalysisOrchestrator
from output.match_analysis_persistence import MatchAnalysisPersistence
from output.match_analysis_output import ConsoleOutput, SilentOutput
from agents.group_chat import create_group_chat
from services.fotmob import FotMob

logging.basicConfig(
    level=logging.INFO, 
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)


def main() -> List[MatchAnalysis]:
    rotowire = Rotowire()
    soccerdata = SoccerData()
    bookmaker = Betclic(cache_ttl=9999999999999999999999999999999999999999999, n_retries=1, )
    fotmob = FotMob(cache_ttl=9999999999999999999999999999999999999999999)
    persistence = MatchAnalysisPersistence()
    output = SilentOutput()

    orchestrator = MatchAnalysisOrchestrator(
        rotowire=rotowire,
        soccerdata=soccerdata,
        bookmaker=bookmaker,
        fotmob=fotmob,
        output_handler=output,
        persistence=persistence,
        league_id=PREMIER_LEAGUE.SOCCERDATA_PREMIER_LEAGUE_ID,
    )
    
    return orchestrator.analyze_matches()

async def run_group_chat() -> None:
    chat = create_group_chat()
    query = "Analyze Arsenal vs Liverpool match"
    
    await chat.add_chat_message(message=query)

    async for response in chat.invoke():
        if response is None or not response.name:
            continue
        
        print(f"\n[{response.name.upper()}]")
        print(response.content)
        print()
 
if __name__ == "__main__":
    # Config is imported above and will load .env automatically
    #asyncio.run(run_group_chat())
    main()