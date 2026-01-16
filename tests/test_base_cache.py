"""Tests for BaseCache abstract class."""
import os
import time
import pytest
from unittest.mock import Mock, patch, MagicMock
from pathlib import Path

# Import the abstract base class
import sys
from pathlib import Path
# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from utils.base_cache import BaseCache


class ConcreteCache(BaseCache):
    """Concrete implementation of BaseCache for testing."""
    
    def _get_cache_key_to_filename(self, key: str, timestamp=None):
        if timestamp is None:
            timestamp = int(time.time())
        return f"{key}_{timestamp}.txt"
    
    def _extract_timestamp_from_filename(self, filename: str):
        # Extract timestamp from format: {key}_{timestamp}.txt
        import re
        match = re.search(r'_(\d+)\.txt$', filename)
        if match:
            try:
                return int(match.group(1))
            except ValueError:
                return None
        return None
    
    def _find_cached_files(self, key: str):
        cached_files = []
        if not os.path.exists(self.store_folder):
            return cached_files
        
        for filename in os.listdir(self.store_folder):
            if filename.startswith(f"{key}_") and filename.endswith(".txt"):
                filepath = os.path.join(self.store_folder, filename)
                cached_files.append(filepath)
        
        return cached_files
    
    def _read_file(self, filepath: str):
        with open(filepath, 'r', encoding='utf-8') as f:
            return f.read()
    
    def _write_file(self, filepath: str, data):
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(str(data))


class TestBaseCacheLoad:
    """Test BaseCache.load() method."""
    
    def test_load_returns_none_when_cache_disabled(self, temp_cache_dir):
        """Test that load returns None when use_cache is False."""
        cache = ConcreteCache(temp_cache_dir, use_cache=False)
        result = cache.load("test_key")
        assert result is None
    
    def test_load_returns_none_when_no_files_exist(self, temp_cache_dir):
        """Test that load returns None when no cached files exist."""
        cache = ConcreteCache(temp_cache_dir)
        result = cache.load("nonexistent_key")
        assert result is None
    
    def test_load_returns_none_when_all_files_expired(self, temp_cache_dir, mock_time):
        """Test that load returns None when all cached files are expired."""
        cache = ConcreteCache(temp_cache_dir, cache_ttl=100.0)
        
        # Set current time to 2000
        mock_time.return_value = 2000.0
        
        # Create an expired file (timestamp 1800, age = 200 > TTL 100)
        old_file = os.path.join(temp_cache_dir, "test_key_1800.txt")
        with open(old_file, 'w') as f:
            f.write("old content")
        
        result = cache.load("test_key")
        assert result is None
    
    def test_load_returns_most_recent_valid_file(self, temp_cache_dir, mock_time):
        """Test that load returns the most recent valid cached file."""
        cache = ConcreteCache(temp_cache_dir, cache_ttl=1000.0)
        
        # Set current time to 2000
        mock_time.return_value = 2000.0
        
        # Create multiple files with different timestamps
        old_file = os.path.join(temp_cache_dir, "test_key_1500.txt")
        recent_file = os.path.join(temp_cache_dir, "test_key_1900.txt")
        newest_file = os.path.join(temp_cache_dir, "test_key_1950.txt")
        
        with open(old_file, 'w') as f:
            f.write("old content")
        with open(recent_file, 'w') as f:
            f.write("recent content")
        with open(newest_file, 'w') as f:
            f.write("newest content")
        
        result = cache.load("test_key")
        assert result == "newest content"
    
    def test_load_handles_file_reading_errors(self, temp_cache_dir, mock_time, monkeypatch):
        """Test that load handles file reading errors gracefully."""
        cache = ConcreteCache(temp_cache_dir, cache_ttl=1000.0)
        
        mock_time.return_value = 2000.0
        
        # Create a valid file
        valid_file = os.path.join(temp_cache_dir, "test_key_1900.txt")
        with open(valid_file, 'w') as f:
            f.write("test content")
        
        # Mock _read_file to raise an exception
        def mock_read_file(filepath):
            raise IOError("Cannot read file")
        
        monkeypatch.setattr(cache, "_read_file", mock_read_file)
        
        result = cache.load("test_key")
        assert result is None
    
    def test_load_skips_files_without_valid_timestamps(self, temp_cache_dir, mock_time):
        """Test that load skips files without valid timestamps."""
        cache = ConcreteCache(temp_cache_dir, cache_ttl=1000.0)
        
        mock_time.return_value = 2000.0
        
        # Create a file with valid timestamp
        valid_file = os.path.join(temp_cache_dir, "test_key_1900.txt")
        with open(valid_file, 'w') as f:
            f.write("valid content")
        
        # Create a file without valid timestamp (invalid format)
        invalid_file = os.path.join(temp_cache_dir, "test_key_invalid.txt")
        with open(invalid_file, 'w') as f:
            f.write("invalid content")
        
        result = cache.load("test_key")
        assert result == "valid content"


class TestBaseCacheSave:
    """Test BaseCache.save() method."""
    
    def test_save_does_nothing_when_store_disabled(self, temp_cache_dir):
        """Test that save does nothing when store is False."""
        cache = ConcreteCache(temp_cache_dir, store=False)
        
        # Create an old file
        old_file = os.path.join(temp_cache_dir, "test_key_1000.txt")
        with open(old_file, 'w') as f:
            f.write("old content")
        
        cache.save("test_key", "new content")
        
        # Old file should still exist
        assert os.path.exists(old_file)
        # No new file should be created
        files = os.listdir(temp_cache_dir)
        assert len(files) == 1
    
    def test_save_removes_old_files_before_saving(self, temp_cache_dir):
        """Test that save removes old cached files before saving new one."""
        cache = ConcreteCache(temp_cache_dir)
        
        # Create old files
        old_file1 = os.path.join(temp_cache_dir, "test_key_1000.txt")
        old_file2 = os.path.join(temp_cache_dir, "test_key_1500.txt")
        with open(old_file1, 'w') as f:
            f.write("old content 1")
        with open(old_file2, 'w') as f:
            f.write("old content 2")
        
        cache.save("test_key", "new content")
        
        # Old files should be removed
        assert not os.path.exists(old_file1)
        assert not os.path.exists(old_file2)
        
        # New file should exist (with current timestamp)
        files = [f for f in os.listdir(temp_cache_dir) if f.startswith("test_key_")]
        assert len(files) == 1
        # Verify content
        with open(os.path.join(temp_cache_dir, files[0]), 'r') as f:
            assert f.read() == "new content"
    
    def test_save_handles_file_removal_errors(self, temp_cache_dir, monkeypatch):
        """Test that save handles file removal errors gracefully."""
        cache = ConcreteCache(temp_cache_dir)
        
        # Create an old file
        old_file = os.path.join(temp_cache_dir, "test_key_1000.txt")
        with open(old_file, 'w') as f:
            f.write("old content")
        
        # Mock os.remove to raise an exception
        original_remove = os.remove
        def mock_remove(filepath):
            if "test_key_1000" in filepath:
                raise PermissionError("Cannot remove file")
            return original_remove(filepath)
        
        monkeypatch.setattr("os.remove", mock_remove)
        
        # Should not raise exception, should continue and save new file
        cache.save("test_key", "new content")
        
        # New file should still be created
        files = [f for f in os.listdir(temp_cache_dir) if f.startswith("test_key_")]
        assert len(files) >= 1
    
    def test_save_creates_new_cache_file(self, temp_cache_dir):
        """Test that save creates a new cache file successfully."""
        cache = ConcreteCache(temp_cache_dir)
        
        cache.save("test_key", "test content")
        
        # File should exist
        files = [f for f in os.listdir(temp_cache_dir) if f.startswith("test_key_")]
        assert len(files) == 1
        
        # Verify content
        with open(os.path.join(temp_cache_dir, files[0]), 'r') as f:
            assert f.read() == "test content"
    
    def test_save_handles_file_writing_errors(self, temp_cache_dir, monkeypatch):
        """Test that save handles file writing errors gracefully."""
        cache = ConcreteCache(temp_cache_dir)
        
        # Mock _write_file to raise an exception
        def mock_write_file(filepath, data):
            raise IOError("Cannot write file")
        
        monkeypatch.setattr(cache, "_write_file", mock_write_file)
        
        # Should not raise exception
        cache.save("test_key", "test content")
        
        # No file should be created
        files = [f for f in os.listdir(temp_cache_dir) if f.startswith("test_key_")]
        assert len(files) == 0


class TestBaseCacheClear:
    """Test BaseCache.clear() method."""
    
    def test_clear_returns_zero_when_no_files_exist(self, temp_cache_dir):
        """Test that clear returns 0 when no files exist."""
        cache = ConcreteCache(temp_cache_dir)
        result = cache.clear("nonexistent_key")
        assert result == 0
    
    def test_clear_removes_all_files_for_key(self, temp_cache_dir):
        """Test that clear removes all cached files for a specific key."""
        cache = ConcreteCache(temp_cache_dir)
        
        # Create multiple files for the same key
        file1 = os.path.join(temp_cache_dir, "test_key_1000.txt")
        file2 = os.path.join(temp_cache_dir, "test_key_1500.txt")
        file3 = os.path.join(temp_cache_dir, "other_key_1000.txt")
        
        with open(file1, 'w') as f:
            f.write("content 1")
        with open(file2, 'w') as f:
            f.write("content 2")
        with open(file3, 'w') as f:
            f.write("other content")
        
        result = cache.clear("test_key")
        
        assert result == 2
        assert not os.path.exists(file1)
        assert not os.path.exists(file2)
        assert os.path.exists(file3)  # Other key's file should remain
    
    def test_clear_returns_correct_count(self, temp_cache_dir):
        """Test that clear returns the correct count of removed files."""
        cache = ConcreteCache(temp_cache_dir)
        
        # Create 3 files
        for i in range(3):
            filepath = os.path.join(temp_cache_dir, f"test_key_{i}.txt")
            with open(filepath, 'w') as f:
                f.write(f"content {i}")
        
        result = cache.clear("test_key")
        assert result == 3
    
    def test_clear_handles_file_removal_errors(self, temp_cache_dir, monkeypatch):
        """Test that clear handles file removal errors gracefully."""
        cache = ConcreteCache(temp_cache_dir)
        
        # Create files
        file1 = os.path.join(temp_cache_dir, "test_key_1000.txt")
        file2 = os.path.join(temp_cache_dir, "test_key_1500.txt")
        
        with open(file1, 'w') as f:
            f.write("content 1")
        with open(file2, 'w') as f:
            f.write("content 2")
        
        # Mock os.remove to raise exception for first file
        original_remove = os.remove
        call_count = [0]
        def mock_remove(filepath):
            call_count[0] += 1
            if call_count[0] == 1:
                raise PermissionError("Cannot remove file")
            return original_remove(filepath)
        
        monkeypatch.setattr("os.remove", mock_remove)
        
        result = cache.clear("test_key")
        
        # Should return count of successfully removed files
        assert result == 1
        # First file should still exist
        assert os.path.exists(file1)
        # Second file should be removed
        assert not os.path.exists(file2)
