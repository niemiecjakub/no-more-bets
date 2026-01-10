"""Base model class with common configuration for all models."""

from pydantic import BaseModel


class FrozenBaseModel(BaseModel):
    """Base model class with frozen configuration.
    
    All models in this package should inherit from this class
    to ensure consistent immutability across the codebase.
    """
    
    class Config:
        frozen = True
