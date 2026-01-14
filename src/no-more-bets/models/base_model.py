from pydantic import BaseModel, ConfigDict


class FrozenBaseModel(BaseModel):
    """Base model class with frozen configuration.
    
    All models in this package should inherit from this class
    to ensure consistent immutability across the codebase.
    """
    
    model_config = ConfigDict(
        frozen=True,
        extra='ignore' 
    )
