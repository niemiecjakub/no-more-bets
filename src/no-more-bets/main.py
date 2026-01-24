import asyncio
import logging
import os
from datetime import datetime
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

from workflows import MatchAnalysisWorkflow
from workflows.match_insights.formatter import ConsoleReportFormatter, JsonReportFormatter
from workflows.betting_ticket.formatter import ConsoleTicketFormatter, JsonTicketFormatter

logging.basicConfig(
    level=logging.INFO, 
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
)


def collect_match_data() -> List[MatchAnalysis]:
    """Helper function to collect match data from orchestrator.
    
    Returns
    -------
    List[MatchAnalysis]
        List of match analysis data.
    """
    rotowire = Rotowire()
    soccerdata = SoccerData()
    bookmaker = Betclic(cache_ttl=9999999999999999999999999999999999999999999, n_retries=1)
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
    
    matches: List[MatchAnalysis] = orchestrator.analyze_matches()
    return matches


def sanitize_filename(text: str) -> str:
    """Sanitize text for use in filenames.
    
    Parameters
    ----------
    text : str
        Text to sanitize.
        
    Returns
    -------
    str
        Sanitized text safe for filesystem.
    """
    # Replace spaces and special characters with underscores
    sanitized = text.replace(" ", "_").replace("/", "_").replace("\\", "_")
    # Remove other problematic characters
    sanitized = "".join(c if c.isalnum() or c in ("_", "-") else "_" for c in sanitized)
    return sanitized


def main() -> None:
    """Collect match data and process through agentic workflow."""
    # Step 1: Collect match data
    rotowire = Rotowire()
    soccerdata = SoccerData()
    bookmaker = Betclic(cache_ttl=9999999999999999999999999999999999999999999, n_retries=1)
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
    
    matches: List[MatchAnalysis] = orchestrator.analyze_matches()
    print(f"Collected data for {len(matches)} matches")

async def run_workflow_single() -> None:
    """Process matches one by one with immediate console output and JSON saving.
    
    Processes each match individually, displays results immediately, and saves
    each report and ticket to separate JSON files.
    """
    # Collect data
    matches = collect_match_data()
    print(f"Collected data for {len(matches)} matches\n")
    
    # Setup output directories
    package_dir = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
    output_dir = os.path.join(package_dir, "no-more-bets", "cache", "output")
    reports_dir = os.path.join(output_dir, "workflow_reports")
    tickets_dir = os.path.join(output_dir, "workflow_tickets")
    os.makedirs(reports_dir, exist_ok=True)
    os.makedirs(tickets_dir, exist_ok=True)
    
    # Process each match individually
    workflow = MatchAnalysisWorkflow()
    
    for i, match in enumerate(matches, 1):
        print(f"\n{'=' * 80}")
        print(f"MATCH {i}/{len(matches)}: {match.match_info.home} vs {match.match_info.away}")
        print(f"{'=' * 80}\n")
        
        try:
            # Generate insights and ticket
            report, ticket = await workflow.process_match(match)
            
            # Display results
            print(ConsoleReportFormatter.format(report))
            print("\n" + ConsoleTicketFormatter.format(ticket))
            
            # Save to JSON files
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            home_safe = sanitize_filename(match.match_info.home)
            away_safe = sanitize_filename(match.match_info.away)
            
            report_filename = f"match_report_{home_safe}_vs_{away_safe}_{timestamp}.json"
            ticket_filename = f"betting_ticket_{home_safe}_vs_{away_safe}_{timestamp}.json"
            
            report_path = os.path.join(reports_dir, report_filename)
            ticket_path = os.path.join(tickets_dir, ticket_filename)
            
            JsonReportFormatter.save(report, report_path)
            JsonTicketFormatter.save(ticket, ticket_path)
            
            print(f"\nSaved report to: {report_path}")
            print(f"Saved ticket to: {ticket_path}")
            
        except Exception as e:
            print(f"Error processing match: {e}")
            logging.error(f"Failed to process match {match.match_info.home} vs {match.match_info.away}: {e}")
            continue
    
    print("\n" + "=" * 80)
    print("Processing complete!")
    print("=" * 80)


async def run_workflow_batch() -> None:
    """Process all matches at once, then display and save all results.
    
    Collects all match data first, processes all matches through workflow
    in batch, then displays and saves all results.
    """
    # Collect all data
    matches = collect_match_data()
    print(f"Collected data for {len(matches)} matches\n")
    
    # Setup output directories
    package_dir = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
    output_dir = os.path.join(package_dir, "no-more-bets", "cache", "output")
    reports_dir = os.path.join(output_dir, "workflow_reports")
    tickets_dir = os.path.join(output_dir, "workflow_tickets")
    os.makedirs(reports_dir, exist_ok=True)
    os.makedirs(tickets_dir, exist_ok=True)
    
    # Process all matches at once
    workflow = MatchAnalysisWorkflow()
    results = await workflow.process_matches(matches)
    
    print(f"Successfully processed {len(results)}/{len(matches)} matches\n")
    
    # Display and save all results
    for i, (report, ticket) in enumerate(results, 1):
        match = matches[i - 1]  # Get original match for filename
        print(f"\n{'=' * 80}")
        print(f"MATCH {i}/{len(results)}: {match.match_info.home} vs {match.match_info.away}")
        print(f"{'=' * 80}\n")
        
        try:
            # Display results
            print(ConsoleReportFormatter.format(report))
            print("\n" + ConsoleTicketFormatter.format(ticket))
            
            # Save to JSON files
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            home_safe = sanitize_filename(match.match_info.home)
            away_safe = sanitize_filename(match.match_info.away)
            
            report_filename = f"match_report_{home_safe}_vs_{away_safe}_{timestamp}.json"
            ticket_filename = f"betting_ticket_{home_safe}_vs_{away_safe}_{timestamp}.json"
            
            report_path = os.path.join(reports_dir, report_filename)
            ticket_path = os.path.join(tickets_dir, ticket_filename)
            
            JsonReportFormatter.save(report, report_path)
            JsonTicketFormatter.save(ticket, ticket_path)
            
            print(f"\nSaved report to: {report_path}")
            print(f"Saved ticket to: {ticket_path}")
            
        except Exception as e:
            print(f"Error saving results for match: {e}")
            logging.error(f"Failed to save results for match {match.match_info.home} vs {match.match_info.away}: {e}")
            continue
    
    print("\n" + "=" * 80)
    print("Processing complete!")
    print("=" * 80)


async def run_workflow_with_data_collection() -> None:
    """Complete workflow: collect data and process with full output and JSON saving.
    
    Most comprehensive option that combines data collection and workflow processing,
    processes matches one by one, and saves all results to JSON files.
    """
    # Step 1: Collect match data
    print("=" * 80)
    print("STEP 1: Collecting match data...")
    print("=" * 80)
    
    rotowire = Rotowire()
    soccerdata = SoccerData()
    bookmaker = Betclic(cache_ttl=9999999999999999999999999999999999999999999, n_retries=1)
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
    
    matches: List[MatchAnalysis] = orchestrator.analyze_matches()
    print(f"Collected data for {len(matches)} matches\n")
    
    # Step 2: Setup output directories
    package_dir = os.path.dirname(os.path.dirname(os.path.dirname(__file__)))
    output_dir = os.path.join(package_dir, "no-more-bets", "cache", "output")
    reports_dir = os.path.join(output_dir, "workflow_reports")
    tickets_dir = os.path.join(output_dir, "workflow_tickets")
    os.makedirs(reports_dir, exist_ok=True)
    os.makedirs(tickets_dir, exist_ok=True)
    
    # Step 3: Process through agentic workflow
    print("=" * 80)
    print("STEP 2: Processing through agentic workflow...")
    print("=" * 80)
    
    workflow = MatchAnalysisWorkflow()
    
    # Process each match
    for i, match in enumerate(matches, 1):
        print(f"\n{'=' * 80}")
        print(f"MATCH {i}/{len(matches)}: {match.match_info.home} vs {match.match_info.away}")
        print(f"{'=' * 80}\n")
        
        try:
            # Generate insights and ticket
            report, ticket = await workflow.process_match(match)
            
            # Display results
            print(ConsoleReportFormatter.format(report))
            print("\n" + ConsoleTicketFormatter.format(ticket))
            
            # Save to JSON files
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            home_safe = sanitize_filename(match.match_info.home)
            away_safe = sanitize_filename(match.match_info.away)
            
            report_filename = f"match_report_{home_safe}_vs_{away_safe}_{timestamp}.json"
            ticket_filename = f"betting_ticket_{home_safe}_vs_{away_safe}_{timestamp}.json"
            
            report_path = os.path.join(reports_dir, report_filename)
            ticket_path = os.path.join(tickets_dir, ticket_filename)
            
            JsonReportFormatter.save(report, report_path)
            JsonTicketFormatter.save(ticket, ticket_path)
            
            print(f"\nSaved report to: {report_path}")
            print(f"Saved ticket to: {ticket_path}")
            
        except Exception as e:
            print(f"Error processing match: {e}")
            logging.error(f"Failed to process match {match.match_info.home} vs {match.match_info.away}: {e}")
            continue
    
    print("\n" + "=" * 80)
    print("Processing complete!")
    print("=" * 80)


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
    # Option 1: Original main (unchanged)
    # main()
    
    # Option 2: Process one by one
    asyncio.run(run_workflow_single())
    
    # Option 3: Process all at once
    # asyncio.run(run_workflow_batch())
    
    # Option 4: Complete workflow
    # asyncio.run(run_workflow_with_data_collection())
    
    # Option 5: Group chat
    # asyncio.run(run_group_chat())