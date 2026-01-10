"""Semantic Kernel plugins for betting analysis agents."""

from .web_search_plugin import WebSearchPlugin
from .fbref_plugin import FBrefPlugin
from .betclic_plugin import BetclicPlugin

__all__ = ["WebSearchPlugin", "FBrefPlugin", "BetclicPlugin"]
