from typing import Annotated, Optional
from pydantic import Field
from ..base_model import FrozenBaseModel


class BaseSearchResult(FrozenBaseModel):
    """Base class for search results with common fields."""
    
    title: Annotated[str, Field(..., description="Title of the search result")]
    body: Annotated[str, Field(..., description="Body/description of the search result")]


class TextSearchResult(BaseSearchResult):
    """Represents a search result from text/web search (DDGS().text())."""
    
    href: Annotated[str, Field(..., description="URL/href of the search result")]
    date: Annotated[Optional[str], Field(None, description="Publication date if available")]


class NewsSearchResult(BaseSearchResult):
    """Represents a search result from news search (DDGS().news())."""
    
    url: Annotated[str, Field(..., description="URL of the news article")]
    image: Annotated[Optional[str], Field(None, description="Image URL if available")]
    source: Annotated[Optional[str], Field(None, description="News source if available")]
    date: Annotated[Optional[str], Field(None, description="Publication date if available")]
