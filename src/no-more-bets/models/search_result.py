from typing import Optional
from pydantic import BaseModel, Field


class BaseSearchResult(BaseModel):
    """Base class for search results with common fields."""
    
    title: str = Field(..., description="Title of the search result")
    body: str = Field(..., description="Body/description of the search result")
    
    class Config:
        frozen = True


class TextSearchResult(BaseSearchResult):
    """Represents a search result from text/web search (DDGS().text())."""
    
    href: str = Field(..., description="URL/href of the search result")
    
    class Config:
        frozen = True


class NewsSearchResult(BaseSearchResult):
    """Represents a search result from news search (DDGS().news())."""
    
    url: str = Field(..., description="URL of the news article")
    image: Optional[str] = Field(None, description="Image URL if available")
    source: Optional[str] = Field(None, description="News source if available")
    date: Optional[str] = Field(None, description="Publication date if available")

    class Config:
        frozen = True
