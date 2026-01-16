"""Tests for JsonCache class."""
import os
import json
import time
import pytest
from unittest.mock import patch

# Import the cache class
import sys
from pathlib import Path
# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from utils.json_cache import JsonCache
from utils import json_cache as json_cache_module

class TestJsonCacheGetCacheKeyToFilename:
    """Test JsonCache._get_cache_key_to_filename() method."""
    
    def test_get_cache_key_to_filename_with_timestamp(self):
        """Test filename generation with provided timestamp."""
        cache = JsonCache(store_folder="/tmp")
        filename = cache._get_cache_key_to_filename("endpoint_key", timestamp=1234567890)
        assert filename == "endpoint_key_1234567890.json"
    
    def test_get_cache_key_to_filename_without_timestamp(self):
        """Test filename generation without timestamp (uses current time)."""
        cache = JsonCache(store_folder="/tmp")
        with patch('time.time', return_value=1234567890):
            filename = cache._get_cache_key_to_filename("endpoint_key")
            assert filename == "endpoint_key_1234567890.json"

class TestJsonCacheExtractTimestampFromFilename:
    """Test JsonCache._extract_timestamp_from_filename() method."""
    
    def test_extract_timestamp_valid_filename(self):
        """Test timestamp extraction from valid filename."""
        cache = JsonCache(store_folder="/tmp")
        filename = "endpoint_key_1234567890.json"
        timestamp = cache._extract_timestamp_from_filename(filename)
        assert timestamp == 1234567890
    
    def test_extract_timestamp_invalid_format(self):
        """Test timestamp extraction from invalid filename format."""
        cache = JsonCache(store_folder="/tmp")
        filename = "endpoint_key.json"
        timestamp = cache._extract_timestamp_from_filename(filename)
        assert timestamp is None
    
    def test_extract_timestamp_missing_timestamp(self):
        """Test timestamp extraction when timestamp is missing."""
        cache = JsonCache(store_folder="/tmp")
        filename = "endpoint_key_.json"
        timestamp = cache._extract_timestamp_from_filename(filename)
        assert timestamp is None
    
    def test_extract_timestamp_non_numeric(self):
        """Test timestamp extraction with non-numeric timestamp."""
        cache = JsonCache(store_folder="/tmp")
        filename = "endpoint_key_abc123.json"
        timestamp = cache._extract_timestamp_from_filename(filename)
        # Should return None for non-numeric timestamp
        assert timestamp is None

class TestJsonCacheFindCachedFiles:
    """Test JsonCache._find_cached_files() method."""
    
    def test_find_cached_files_finds_matching_files(self, temp_cache_dir):
        """Test that find_cached_files finds all files matching cache key prefix."""
        cache = JsonCache(store_folder=temp_cache_dir)
        cache_key = "endpoint_key"
        
        # Create matching files
        file1 = os.path.join(temp_cache_dir, f"{cache_key}_1000.json")
        file2 = os.path.join(temp_cache_dir, f"{cache_key}_2000.json")
        other_file = os.path.join(temp_cache_dir, "other_key_1000.json")
        
        with open(file1, 'w') as f:
            json.dump({"data": 1}, f)
        with open(file2, 'w') as f:
            json.dump({"data": 2}, f)
        with open(other_file, 'w') as f:
            json.dump({"data": "other"}, f)
        
        cached_files = cache._find_cached_files(cache_key)
        
        assert len(cached_files) == 2
        assert file1 in cached_files
        assert file2 in cached_files
        assert other_file not in cached_files
    
    def test_find_cached_files_handles_nonexistent_directory(self, tmp_path):
        """Test that find_cached_files handles non-existent directory."""
        cache_dir = str(tmp_path / "nonexistent")
        cache = JsonCache(store_folder=cache_dir)
        
        cached_files = cache._find_cached_files("endpoint_key")
        assert cached_files == []
    
    def test_find_cached_files_ignores_files_without_json_extension(self, temp_cache_dir):
        """Test that find_cached_files ignores files without .json extension."""
        cache = JsonCache(store_folder=temp_cache_dir)
        cache_key = "endpoint_key"
        
        # Create file with .json extension
        valid_file = os.path.join(temp_cache_dir, f"{cache_key}_1000.json")
        # Create file without .json extension
        invalid_file = os.path.join(temp_cache_dir, f"{cache_key}_1000.txt")
        
        with open(valid_file, 'w') as f:
            json.dump({"data": "valid"}, f)
        with open(invalid_file, 'w') as f:
            f.write("invalid")
        
        cached_files = cache._find_cached_files(cache_key)
        
        # Should only find file with .json extension
        assert len(cached_files) == 1
        assert valid_file in cached_files
        assert invalid_file not in cached_files
    
    def test_find_cached_files_requires_exact_prefix(self, temp_cache_dir):
        """Test that find_cached_files requires exact prefix match."""
        cache = JsonCache(store_folder=temp_cache_dir)
        cache_key = "endpoint"
        
        # Create file with exact prefix
        exact_file = os.path.join(temp_cache_dir, f"{cache_key}_1000.json")
        # Create file with prefix that contains the key but isn't exact
        similar_file = os.path.join(temp_cache_dir, f"{cache_key}_extended_1000.json")
        
        with open(exact_file, 'w') as f:
            json.dump({"data": "exact"}, f)
        with open(similar_file, 'w') as f:
            json.dump({"data": "similar"}, f)
        
        cached_files = cache._find_cached_files(cache_key)
        
        # Should only find file with exact prefix
        assert len(cached_files) == 1
        assert exact_file in cached_files
        assert similar_file not in cached_files


class TestJsonCacheFileOperations:
    """Test JsonCache file read/write operations."""
    
    def test_read_file_reads_json_dict(self, temp_cache_dir):
        """Test that _read_file reads and parses JSON dict correctly."""
        cache = JsonCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.json")
        json_data = {"key": "value", "number": 123, "nested": {"data": "test"}}
        
        with open(test_file, 'w', encoding='utf-8') as f:
            json.dump(json_data, f)
        
        result = cache._read_file(test_file)
        assert result == json_data
        assert isinstance(result, dict)

    def test_read_file_handles_utf8_encoding(self, temp_cache_dir):
        """Test that _read_file handles UTF-8 encoding correctly."""
        cache = JsonCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.json")
        json_data = {"text": "Test with émojis 🎉 and unicode 中文"}
        
        with open(test_file, 'w', encoding='utf-8') as f:
            json.dump(json_data, f, ensure_ascii=False)
        
        result = cache._read_file(test_file)
        assert result == json_data
    
    def test_write_file_writes_json_dict(self, temp_cache_dir):
        """Test that _write_file writes JSON dict correctly."""
        cache = JsonCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.json")
        json_data = {"key": "value", "number": 123}
        
        cache._write_file(test_file, json_data)
        
        assert os.path.exists(test_file)
        with open(test_file, 'r', encoding='utf-8') as f:
            result = json.load(f)
            assert result == json_data

    def test_write_file_pretty_printing(self, temp_cache_dir):
        """Test that _write_file uses pretty printing (indent=2)."""
        cache = JsonCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.json")
        json_data = {"key": "value", "nested": {"data": "test"}}
        
        cache._write_file(test_file, json_data)
        
        with open(test_file, 'r', encoding='utf-8') as f:
            content = f.read()
            # Should have indentation (pretty printed) - must have newlines
            assert "\n" in content, "JSON should be pretty printed with newlines"
            assert "  " in content, "JSON should be indented"
    
    def test_write_file_ensure_ascii_false(self, temp_cache_dir):
        """Test that _write_file uses ensure_ascii=False."""
        cache = JsonCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.json")
        json_data = {"text": "émojis 🎉 unicode 中文"}
        
        cache._write_file(test_file, json_data)
        
        with open(test_file, 'r', encoding='utf-8') as f:
            content = f.read()
            # Should contain unicode characters directly, not escaped (ensure_ascii=False)
            assert "émojis" in content, "Unicode characters should not be escaped"
            assert "🎉" in content, "Emoji should not be escaped"
            assert "中文" in content, "Chinese characters should not be escaped"
    
    def test_read_file_handles_corrupted_json(self, temp_cache_dir):
        """Test that _read_file raises exception for corrupted JSON."""
        cache = JsonCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.json")
        # Write invalid JSON
        with open(test_file, 'w', encoding='utf-8') as f:
            f.write("{invalid json}")
        
        # Should raise JSONDecodeError
        with pytest.raises(json.JSONDecodeError):
            cache._read_file(test_file)


class TestJsonCachePublicAPI:
    """Test JsonCache public API methods."""
    
    def test_load_returns_dict_when_valid_cache_exists(self, temp_cache_dir, mock_time):
        """Test that load returns dict when valid cache exists."""
        cache = JsonCache(store_folder=temp_cache_dir, cache_ttl=1000.0)
        
        mock_time.return_value = 2000.0
        
        cache_key = "endpoint_key"
        cached_file = os.path.join(temp_cache_dir, f"{cache_key}_1900.json")
        json_data = {"key": "value", "data": "test"}
        
        with open(cached_file, 'w', encoding='utf-8') as f:
            json.dump(json_data, f)
        
        result = cache.load(cache_key)
        assert result == json_data
        assert isinstance(result, dict)
    
    def test_load_returns_list_when_valid_cache_exists(self, temp_cache_dir, mock_time):
        """Test that load returns list when valid cache exists."""
        cache = JsonCache(store_folder=temp_cache_dir, cache_ttl=1000.0)
        
        mock_time.return_value = 2000.0
        
        cache_key = "endpoint_key"
        cached_file = os.path.join(temp_cache_dir, f"{cache_key}_1900.json")
        json_data = [{"item": 1}, {"item": 2}]
        
        with open(cached_file, 'w', encoding='utf-8') as f:
            json.dump(json_data, f)
        
        result = cache.load(cache_key)
        assert result == json_data
        assert isinstance(result, list)
    
    def test_load_returns_none_when_expired(self, temp_cache_dir, mock_time):
        """Test that load returns None when cache is expired."""
        cache = JsonCache(store_folder=temp_cache_dir, cache_ttl=100.0)
        
        mock_time.return_value = 2000.0
        
        cache_key = "endpoint_key"
        # Create expired file (timestamp 1800, age = 200 > TTL 100)
        cached_file = os.path.join(temp_cache_dir, f"{cache_key}_1800.json")
        with open(cached_file, 'w') as f:
            json.dump({"old": "data"}, f)
        
        result = cache.load(cache_key)
        assert result is None
    
    def test_load_returns_none_when_no_cache(self, temp_cache_dir):
        """Test that load returns None when no cache exists."""
        cache = JsonCache(store_folder=temp_cache_dir)
        result = cache.load("nonexistent_key")
        assert result is None
    
    def test_load_returns_none_for_invalid_data_type(self, temp_cache_dir, mock_time, monkeypatch):
        """Test that load returns None when cached data is not dict or list."""
        cache = JsonCache(store_folder=temp_cache_dir, cache_ttl=1000.0)
        
        mock_time.return_value = 2000.0
        
        cache_key = "endpoint_key"
        cached_file = os.path.join(temp_cache_dir, f"{cache_key}_1900.json")
        
        # Write a string (not dict or list) - this shouldn't happen in practice
        # but we test the type check
        with open(cached_file, 'w', encoding='utf-8') as f:
            json.dump("just a string", f)
        
        result = cache.load(cache_key)
        # Should return None because it's not a dict or list
        assert result is None
    
    def test_save_saves_json_and_removes_old_files(self, temp_cache_dir):
        """Test that save saves JSON and removes old files."""
        cache = JsonCache(store_folder=temp_cache_dir)
        
        cache_key = "endpoint_key"
        
        # Create old files
        old_file1 = os.path.join(temp_cache_dir, f"{cache_key}_1000.json")
        old_file2 = os.path.join(temp_cache_dir, f"{cache_key}_1500.json")
        
        with open(old_file1, 'w') as f:
            json.dump({"old": 1}, f)
        with open(old_file2, 'w') as f:
            json.dump({"old": 2}, f)
        
        new_data = {"new": "data", "value": 123}
        cache.save(cache_key, new_data)
        
        # Old files should be removed
        assert not os.path.exists(old_file1)
        assert not os.path.exists(old_file2)
        
        # New file should exist
        files = [f for f in os.listdir(temp_cache_dir) 
                 if f.startswith(f"{cache_key}_") and f.endswith(".json")]
        assert len(files) == 1
        
        # Verify content
        with open(os.path.join(temp_cache_dir, files[0]), 'r', encoding='utf-8') as f:
            result = json.load(f)
            assert result == new_data

    def test_clear_removes_all_cached_files_for_key(self, temp_cache_dir):
        """Test that clear removes all cached files for a cache key."""
        cache = JsonCache(store_folder=temp_cache_dir)
        
        cache_key = "endpoint_key"
        
        # Create multiple files
        file1 = os.path.join(temp_cache_dir, f"{cache_key}_1000.json")
        file2 = os.path.join(temp_cache_dir, f"{cache_key}_2000.json")
        other_file = os.path.join(temp_cache_dir, "other_key_1000.json")
        
        with open(file1, 'w') as f:
            json.dump({"data": 1}, f)
        with open(file2, 'w') as f:
            json.dump({"data": 2}, f)
        with open(other_file, 'w') as f:
            json.dump({"data": "other"}, f)
        
        result = cache.clear(cache_key)
        
        assert result == 2
        assert not os.path.exists(file1)
        assert not os.path.exists(file2)
        assert os.path.exists(other_file)  # Other file should remain
    
    def test_clear_returns_zero_when_no_files(self, temp_cache_dir):
        """Test that clear returns 0 when no files exist."""
        cache = JsonCache(store_folder=temp_cache_dir)
        result = cache.clear("nonexistent_key")
        assert result == 0
    
    def test_load_selects_most_recent_valid_file(self, temp_cache_dir, mock_time):
        """Test that load selects the most recent valid cached file."""
        cache = JsonCache(store_folder=temp_cache_dir, cache_ttl=1000.0)
        
        mock_time.return_value = 2000.0
        
        cache_key = "endpoint_key"
        
        # Create multiple files with different timestamps
        old_file = os.path.join(temp_cache_dir, f"{cache_key}_1500.json")
        recent_file = os.path.join(temp_cache_dir, f"{cache_key}_1900.json")
        newest_file = os.path.join(temp_cache_dir, f"{cache_key}_1950.json")
        
        with open(old_file, 'w') as f:
            json.dump({"data": "old"}, f)
        with open(recent_file, 'w') as f:
            json.dump({"data": "recent"}, f)
        with open(newest_file, 'w') as f:
            json.dump({"data": "newest"}, f)
        
        result = cache.load(cache_key)
        assert result == {"data": "newest"}
