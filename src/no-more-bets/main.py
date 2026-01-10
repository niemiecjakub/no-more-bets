"""Main entry point for the No More Bets betting analysis system."""

import asyncio
from dotenv import load_dotenv

from agents.group_chat import run_betting_analysis
from models import ApprovedCoupon


def run_analysis(query: str):
    """Run a single betting analysis query.
    
    Parameters
    ----------
    query : str
        The match query to analyze (e.g., "Analyze Arsenal vs Liverpool").
    """
    result = asyncio.run(run_betting_analysis(query, verbose=True))
    
    # If result is an ApprovedCoupon, display it nicely
    if isinstance(result, ApprovedCoupon):
        print("\n" + "="*70)
        print("APPROVED COUPON SUMMARY")
        print("="*70)
        print(f"\nQuery: {result.query}")
        print(f"Analysis Date: {result.analysis_date}")
        print(f"Total Bets: {result.total_bets}")
        print(f"Total Stake: {result.total_stake} units")
        if result.expected_return:
            print(f"Expected Return: {result.expected_return:.2f} units")
        if result.potential_profit:
            print(f"Potential Profit: {result.potential_profit:.2f} units")
        print(f"Risk Level: {result.overall_risk}")
        
        print("\n" + "-"*70)
        print("BETTING SELECTIONS:")
        print("-"*70)
        for i, bet in enumerate(result.bets, 1):
            print(f"\nBet {i}:")
            print(f"  Match: {bet.match}")
            print(f"  Type: {bet.bet_type}")
            print(f"  Selection: {bet.selection}")
            print(f"  Odds: {bet.odds:.2f}")
            if bet.implied_probability:
                print(f"  Implied Probability: {bet.implied_probability:.1f}%")
            print(f"  Confidence: {bet.confidence}")
            print(f"  Stake: {bet.stake} units")
            print(f"  Reasoning: {bet.reasoning[:200]}..." if len(bet.reasoning) > 200 else f"  Reasoning: {bet.reasoning}")
        
        print("\n" + "-"*70)
        print("CLOSING THOUGHTS:")
        print("-"*70)
        print(result.closing_thoughts)
        
        if result.research_summary or result.analytics_summary:
            print("\n" + "-"*70)
            print("ANALYSIS SUMMARY:")
            print("-"*70)
            if result.research_summary:
                print(f"\nResearch: {result.research_summary[:300]}...")
            if result.analytics_summary:
                print(f"\nAnalytics: {result.analytics_summary[:300]}...")
        
        print("\n" + "="*70)


def main():
    """Main entry point with CLI argument support."""
    load_dotenv()
    run_analysis("Analyze Leeds vs Fulham")


if __name__ == "__main__":
    main()
