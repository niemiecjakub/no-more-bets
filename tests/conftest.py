"""Shared pytest fixtures for cache utility tests."""
import os
import tempfile
import shutil
from pathlib import Path
from typing import Generator
import pytest
from unittest.mock import Mock, MagicMock


@pytest.fixture
def temp_cache_dir(tmp_path: Path) -> str:
    """Create a temporary directory for cache files.
    
    Parameters
    ----------
    tmp_path : Path
        Pytest's temporary path fixture.
        
    Yields
    ------
    str
        Path to temporary cache directory.
    """
    cache_dir = tmp_path / "cache"
    cache_dir.mkdir()
    yield str(cache_dir)
    # Cleanup is handled by pytest's tmp_path fixture


@pytest.fixture
def mock_time(monkeypatch):
    """Mock time.time() for TTL testing.
    
    Parameters
    ----------
    monkeypatch : pytest.MonkeyPatch
        Pytest's monkeypatch fixture.
        
    Yields
    ------
    Mock
        Mock object for time.time() that can be controlled.
    """
    mock_time_obj = Mock()
    mock_time_obj.return_value = 1000.0  # Default timestamp
    monkeypatch.setattr("time.time", mock_time_obj)
    yield mock_time_obj


def create_test_file(filepath: str, content: str) -> None:
    """Helper function to create a test file with content.
    
    Parameters
    ----------
    filepath : str
        Path where to create the file.
    content : str
        Content to write to the file.
    """
    os.makedirs(os.path.dirname(filepath), exist_ok=True)
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)
