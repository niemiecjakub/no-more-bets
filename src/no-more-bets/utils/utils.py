
from pathlib import Path


def get_project_root() -> str:
    """Get the project root directory (workspace root).
    
    Returns
    -------
    str
        Absolute path to the project root directory.
    """
    current_file = Path(__file__).resolve()
    project_root = current_file.parent.parent.parent.parent
    return str(project_root)
