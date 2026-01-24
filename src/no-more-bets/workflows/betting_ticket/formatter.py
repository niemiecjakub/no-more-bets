"""Formatters for betting ticket output."""
import json
from .models import BettingTicket, BetSelection


class ConsoleTicketFormatter:
    """Formatter for console output of betting tickets."""
    
    @staticmethod
    def format(ticket: BettingTicket) -> str:
        """Format betting ticket for console output.
        
        Parameters
        ----------
        ticket : BettingTicket
            The ticket to format.
            
        Returns
        -------
        str
            Formatted string for console output.
        """
        lines = []
        lines.append("=" * 80)
        lines.append(f"BETTING TICKET")
        lines.append("=" * 80)
        lines.append(f"Ticket ID: {ticket.ticket_id}")
        lines.append(f"Created: {ticket.created_at}")
        lines.append("")
        
        if not ticket.selections:
            lines.append("NO SELECTIONS - No value bets identified for this match.")
            lines.append("")
        else:
            lines.append(f"SELECTIONS ({len(ticket.selections)}):")
            lines.append("")
            
            for i, selection in enumerate(ticket.selections, 1):
                lines.append(f"BET {i}: {selection.selection}")
                lines.append(f"  Match: {selection.match.home} vs {selection.match.away}")
                lines.append(f"  Type: {selection.bet_type}")
                lines.append(f"  Selection: {selection.selection}")
                lines.append(f"  Odds: {selection.odds:.2f}")
                lines.append(f"  Confidence: {selection.confidence}")
                lines.append(f"  Stake: {selection.stake_units} units")
                lines.append(f"  Value Score: {selection.value_score:.2f}")
                lines.append(f"  Implied Probability: {selection.implied_probability:.1%}")
                lines.append(f"  Calculated Probability: {selection.calculated_probability:.1%}")
                lines.append(f"  Reasoning: {selection.reasoning}")
                lines.append("")
        
        lines.append("TICKET SUMMARY:")
        lines.append(f"  Total Stake: {ticket.total_stake} units")
        lines.append(f"  Expected Return: {ticket.expected_return:.2f} units")
        lines.append(f"  Risk Assessment: {ticket.risk_assessment}")
        lines.append(f"  Overall Confidence: {ticket.overall_confidence}")
        lines.append("")
        lines.append("=" * 80)
        
        return "\n".join(lines)


class JsonTicketFormatter:
    """Formatter for JSON output of betting tickets."""
    
    @staticmethod
    def format(ticket: BettingTicket, indent: int = 2) -> str:
        """Format betting ticket as JSON.
        
        Parameters
        ----------
        ticket : BettingTicket
            The ticket to format.
        indent : int
            JSON indentation level.
            
        Returns
        -------
        str
            JSON string representation.
        """
        return ticket.model_dump_json(indent=indent)
    
    @staticmethod
    def save(ticket: BettingTicket, filepath: str, indent: int = 2) -> None:
        """Save betting ticket to JSON file.
        
        Parameters
        ----------
        ticket : BettingTicket
            The ticket to save.
        filepath : str
            Path to save the JSON file.
        indent : int
            JSON indentation level.
        """
        json_str = JsonTicketFormatter.format(ticket, indent=indent)
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(json_str)
