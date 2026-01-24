"""Betting Ticket vertical slice for generating structured betting recommendations."""

from .models import BettingTicket, BetSelection
from .processor import BettingTicketProcessor

__all__ = [
    "BettingTicket",
    "BetSelection",
    "BettingTicketProcessor",
]
