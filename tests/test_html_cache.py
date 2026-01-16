"""Tests for HtmlCache class."""
import os
import time
import pytest
import sys
from unittest.mock import patch, Mock
from urllib.parse import urlparse
from pathlib import Path
# Add src/no-more-bets to path
sys.path.insert(0, str(Path(__file__).parent.parent / 'src' / 'no-more-bets'))
from utils.html_cache import HtmlCache
from utils import html_cache as html_cache_module


class TestHtmlCacheInitialization:
    """Test HtmlCache initialization."""
    
    def test_init_default_store_folder(self, tmp_path, monkeypatch):
        """Test initialization with default store_folder."""
        # Mock get_project_root to return tmp_path
        # Patch it where it's used (in html_cache module), not where it's defined
        monkeypatch.setattr(html_cache_module, "get_project_root", lambda: str(tmp_path))
        
        cache = HtmlCache()
        expected_path = os.path.join(str(tmp_path), "src", "no-more-bets", "cache", "html")
        assert cache.store_folder == expected_path
        assert os.path.exists(expected_path)
    
    def test_init_custom_store_folder(self, temp_cache_dir):
        """Test initialization with custom store_folder."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        assert cache.store_folder == temp_cache_dir
        assert os.path.exists(temp_cache_dir)
    
    def test_init_all_parameters(self, temp_cache_dir):
        """Test initialization with all parameters."""
        cache = HtmlCache(
            store_folder=temp_cache_dir,
            store=False,
            use_cache=False,
            cache_ttl=7200.0
        )
        assert cache.store_folder == temp_cache_dir
        assert cache.store is False
        assert cache.use_cache is False
        assert cache.cache_ttl == 7200.0


class TestHtmlCacheUrlToFilename:
    """Test HtmlCache._url_to_filename() method."""
    
    def test_url_to_filename_simple_url(self):
        """Test URL to filename conversion for simple URL."""
        cache = HtmlCache(store_folder="/tmp")
        filename = cache._url_to_filename("https://example.com/page")
        assert filename.endswith(".html")
        assert "example.com" in filename
        assert "page" in filename
    
    def test_url_to_filename_with_query_parameters(self):
        """Test URL to filename conversion with query parameters."""
        cache = HtmlCache(store_folder="/tmp")
        filename = cache._url_to_filename("https://example.com/page?param=value&other=123")
        assert filename.endswith(".html")
        assert "example.com" in filename
        assert "page" in filename
    
    def test_url_to_filename_with_special_characters(self):
        """Test URL to filename conversion with special characters."""
        cache = HtmlCache(store_folder="/tmp")
        url = "https://example.com/page with spaces"
        filename = cache._url_to_filename(url)
        assert filename.endswith(".html")
        # Special characters should be handled
        assert "/" not in filename or filename.count("/") == 0
    
    def test_url_to_filename_invalid_filesystem_characters(self):
        """Test that invalid filesystem characters are replaced."""
        cache = HtmlCache(store_folder="/tmp")
        # URL with various invalid characters
        url = "https://example.com/page?query=value*test<file>|pipe"
        filename = cache._url_to_filename(url)
        assert "/" not in filename
        assert "\\" not in filename
        assert ":" not in filename or filename.count(":") == 0  # May be in protocol
        assert "*" not in filename
        assert "<" not in filename
        assert ">" not in filename
        assert "|" not in filename
    
    def test_url_to_filename_url_encoding(self):
        """Test that special characters are URL encoded."""
        cache = HtmlCache(store_folder="/tmp")
        url = "https://example.com/page with spaces & symbols"
        filename = cache._url_to_filename(url)
        assert filename.endswith(".html")
        # Should be URL encoded
        assert "%20" in filename or " " not in filename
    
    def test_url_to_filename_with_timestamp(self):
        """Test URL to filename conversion with timestamp."""
        cache = HtmlCache(store_folder="/tmp")
        with patch('time.time', return_value=1234567890):
            filename = cache._url_to_filename("https://example.com/page", include_timestamp=True)
            assert "1234567890" in filename
            assert filename.endswith(".html")
    
    def test_url_to_filename_without_timestamp(self):
        """Test URL to filename conversion without timestamp."""
        cache = HtmlCache(store_folder="/tmp")
        filename = cache._url_to_filename("https://example.com/page", include_timestamp=False)
        assert filename.endswith(".html")
        # Should not contain a timestamp pattern
        import re
        assert not re.search(r'-\d+\.html$', filename)
    
    def test_url_to_filename_adds_html_extension(self):
        """Test that .html extension is added if not present."""
        cache = HtmlCache(store_folder="/tmp")
        filename = cache._url_to_filename("https://example.com/page")
        assert filename.endswith(".html")


class TestHtmlCacheGetCacheKeyToFilename:
    """Test HtmlCache._get_cache_key_to_filename() method."""
    
    def test_get_cache_key_to_filename_with_timestamp(self):
        """Test filename generation with provided timestamp."""
        cache = HtmlCache(store_folder="/tmp")
        filename = cache._get_cache_key_to_filename("https://example.com/page", timestamp=1234567890)
        assert filename.endswith("-1234567890.html")
        assert "example.com" in filename
    
    def test_get_cache_key_to_filename_without_timestamp(self):
        """Test filename generation without timestamp (uses current time)."""
        cache = HtmlCache(store_folder="/tmp")
        with patch('time.time', return_value=1234567890):
            filename = cache._get_cache_key_to_filename("https://example.com/page")
            assert filename.endswith("-1234567890.html")
    
    def test_get_cache_key_to_filename_format(self):
        """Test that filename follows correct format: {base}-{timestamp}.html"""
        cache = HtmlCache(store_folder="/tmp")
        filename = cache._get_cache_key_to_filename("https://example.com/page", timestamp=1234567890)
        # Should match pattern: {base}-{timestamp}.html
        import re
        match = re.search(r'^(.+)-(\d+)\.html$', filename)
        assert match is not None
        assert match.group(2) == "1234567890"


class TestHtmlCacheExtractTimestampFromFilename:
    """Test HtmlCache._extract_timestamp_from_filename() method."""
    
    def test_extract_timestamp_valid_filename(self):
        """Test timestamp extraction from valid filename."""
        cache = HtmlCache(store_folder="/tmp")
        filename = "example_com_page-1234567890.html"
        timestamp = cache._extract_timestamp_from_filename(filename)
        assert timestamp == 1234567890
    
    def test_extract_timestamp_invalid_format(self):
        """Test timestamp extraction from invalid filename format."""
        cache = HtmlCache(store_folder="/tmp")
        filename = "example_com_page.html"
        timestamp = cache._extract_timestamp_from_filename(filename)
        assert timestamp is None
    
    def test_extract_timestamp_missing_timestamp(self):
        """Test timestamp extraction when timestamp is missing."""
        cache = HtmlCache(store_folder="/tmp")
        filename = "example_com_page-.html"
        timestamp = cache._extract_timestamp_from_filename(filename)
        assert timestamp is None
    
    def test_extract_timestamp_non_numeric(self):
        """Test timestamp extraction with non-numeric timestamp."""
        cache = HtmlCache(store_folder="/tmp")
        filename = "example_com_page-abc123.html"
        timestamp = cache._extract_timestamp_from_filename(filename)
        # Should return None or handle gracefully
        assert timestamp is None or isinstance(timestamp, int)


class TestHtmlCacheGetBaseFilename:
    """Test HtmlCache._get_base_filename() method."""
    
    def test_get_base_filename_returns_without_timestamp(self):
        """Test that base filename is returned without timestamp."""
        cache = HtmlCache(store_folder="/tmp")
        base_filename = cache._get_base_filename("https://example.com/page")
        assert base_filename.endswith(".html")
        # Should not contain timestamp pattern
        import re
        assert not re.search(r'-\d+\.html$', base_filename)


class TestHtmlCacheFindCachedFiles:
    """Test HtmlCache._find_cached_files() method."""
    
    def test_find_cached_files_finds_matching_files(self, temp_cache_dir):
        """Test that find_cached_files finds all files matching base filename."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        url = "https://example.com/page"
        base_filename = cache._get_base_filename(url)
        base_name_without_ext = base_filename.replace(".html", "")
        
        # Create matching files
        file1 = os.path.join(temp_cache_dir, f"{base_name_without_ext}-1000.html")
        file2 = os.path.join(temp_cache_dir, f"{base_name_without_ext}-2000.html")
        other_file = os.path.join(temp_cache_dir, "other-1000.html")
        
        with open(file1, 'w') as f:
            f.write("content 1")
        with open(file2, 'w') as f:
            f.write("content 2")
        with open(other_file, 'w') as f:
            f.write("other content")
        
        cached_files = cache._find_cached_files(url)
        
        assert len(cached_files) == 2
        assert file1 in cached_files
        assert file2 in cached_files
        assert other_file not in cached_files
    
    def test_find_cached_files_handles_nonexistent_directory(self, tmp_path):
        """Test that find_cached_files handles non-existent directory."""
        cache_dir = str(tmp_path / "nonexistent")
        cache = HtmlCache(store_folder=cache_dir)
        
        cached_files = cache._find_cached_files("https://example.com/page")
        assert cached_files == []
    
    def test_find_cached_files_ignores_files_without_timestamp_pattern(self, temp_cache_dir):
        """Test that find_cached_files ignores files without timestamp pattern."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        url = "https://example.com/page"
        base_filename = cache._get_base_filename(url)
        base_name_without_ext = base_filename.replace(".html", "")
        
        # Create file with timestamp pattern
        valid_file = os.path.join(temp_cache_dir, f"{base_name_without_ext}-1000.html")
        # Create file without timestamp pattern
        invalid_file = os.path.join(temp_cache_dir, f"{base_name_without_ext}.html")
        
        with open(valid_file, 'w') as f:
            f.write("valid")
        with open(invalid_file, 'w') as f:
            f.write("invalid")
        
        cached_files = cache._find_cached_files(url)
        
        # Should only find file with timestamp pattern
        assert len(cached_files) == 1
        assert valid_file in cached_files
        assert invalid_file not in cached_files


class TestHtmlCacheFileOperations:
    """Test HtmlCache file read/write operations."""
    
    def test_read_file_reads_html_content(self, temp_cache_dir):
        """Test that _read_file reads HTML content correctly."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.html")
        html_content = "<html><body>Test content</body></html>"
        
        with open(test_file, 'w', encoding='utf-8') as f:
            f.write(html_content)
        
        result = cache._read_file(test_file)
        assert result == html_content
    
    def test_read_file_handles_utf8_encoding(self, temp_cache_dir):
        """Test that _read_file handles UTF-8 encoding correctly."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.html")
        html_content = "<html><body>Test with émojis 🎉 and unicode 中文</body></html>"
        
        with open(test_file, 'w', encoding='utf-8') as f:
            f.write(html_content)
        
        result = cache._read_file(test_file)
        assert result == html_content
    
    def test_write_file_writes_html_content(self, temp_cache_dir):
        """Test that _write_file writes HTML content correctly."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.html")
        html_content = "<html><body>Test content</body></html>"
        
        cache._write_file(test_file, html_content)
        
        assert os.path.exists(test_file)
        with open(test_file, 'r', encoding='utf-8') as f:
            assert f.read() == html_content
    
    def test_write_file_handles_utf8_encoding(self, temp_cache_dir):
        """Test that _write_file handles UTF-8 encoding correctly."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        
        test_file = os.path.join(temp_cache_dir, "test.html")
        html_content = "<html><body>Test with émojis 🎉 and unicode 中文</body></html>"
        
        cache._write_file(test_file, html_content)
        
        with open(test_file, 'r', encoding='utf-8') as f:
            assert f.read() == html_content


class TestHtmlCachePublicAPI:
    """Test HtmlCache public API methods."""
    
    def test_load_returns_html_string(self, temp_cache_dir, mock_time):
        """Test that load returns HTML string when valid cache exists."""
        cache = HtmlCache(store_folder=temp_cache_dir, cache_ttl=1000.0)
        
        mock_time.return_value = 2000.0
        
        url = "https://example.com/page"
        base_filename = cache._get_base_filename(url)
        base_name_without_ext = base_filename.replace(".html", "")
        
        cached_file = os.path.join(temp_cache_dir, f"{base_name_without_ext}-1900.html")
        html_content = "<html><body>Test</body></html>"
        
        with open(cached_file, 'w', encoding='utf-8') as f:
            f.write(html_content)
        
        result = cache.load(url)
        assert result == html_content
        assert isinstance(result, str)
    
    def test_load_returns_none_when_expired(self, temp_cache_dir, mock_time):
        """Test that load returns None when cache is expired."""
        cache = HtmlCache(store_folder=temp_cache_dir, cache_ttl=100.0)
        
        mock_time.return_value = 2000.0
        
        url = "https://example.com/page"
        base_filename = cache._get_base_filename(url)
        base_name_without_ext = base_filename.replace(".html", "")
        
        # Create expired file (timestamp 1800, age = 200 > TTL 100)
        cached_file = os.path.join(temp_cache_dir, f"{base_name_without_ext}-1800.html")
        with open(cached_file, 'w') as f:
            f.write("<html>Old</html>")
        
        result = cache.load(url)
        assert result is None
    
    def test_load_returns_none_when_no_cache(self, temp_cache_dir):
        """Test that load returns None when no cache exists."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        result = cache.load("https://example.com/nonexistent")
        assert result is None
    
    def test_save_saves_html_and_removes_old_files(self, temp_cache_dir):
        """Test that save saves HTML and removes old files."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        
        url = "https://example.com/page"
        base_filename = cache._get_base_filename(url)
        base_name_without_ext = base_filename.replace(".html", "")
        
        # Create old files
        old_file1 = os.path.join(temp_cache_dir, f"{base_name_without_ext}-1000.html")
        old_file2 = os.path.join(temp_cache_dir, f"{base_name_without_ext}-1500.html")
        
        with open(old_file1, 'w') as f:
            f.write("old 1")
        with open(old_file2, 'w') as f:
            f.write("old 2")
        
        cache.save(url, "<html>New content</html>")
        
        # Old files should be removed
        assert not os.path.exists(old_file1)
        assert not os.path.exists(old_file2)
        
        # New file should exist
        files = [f for f in os.listdir(temp_cache_dir) 
                 if f.startswith(base_name_without_ext + "-") and f.endswith(".html")]
        assert len(files) == 1
        
        # Verify content
        with open(os.path.join(temp_cache_dir, files[0]), 'r', encoding='utf-8') as f:
            assert f.read() == "<html>New content</html>"
    
    def test_clear_removes_all_cached_files_for_url(self, temp_cache_dir):
        """Test that clear removes all cached files for a URL."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        
        url = "https://example.com/page"
        base_filename = cache._get_base_filename(url)
        base_name_without_ext = base_filename.replace(".html", "")
        
        # Create multiple files
        file1 = os.path.join(temp_cache_dir, f"{base_name_without_ext}-1000.html")
        file2 = os.path.join(temp_cache_dir, f"{base_name_without_ext}-2000.html")
        other_file = os.path.join(temp_cache_dir, "other-1000.html")
        
        with open(file1, 'w') as f:
            f.write("content 1")
        with open(file2, 'w') as f:
            f.write("content 2")
        with open(other_file, 'w') as f:
            f.write("other")
        
        result = cache.clear(url)
        
        assert result == 2
        assert not os.path.exists(file1)
        assert not os.path.exists(file2)
        assert os.path.exists(other_file)  # Other file should remain
    
    def test_clear_returns_zero_when_no_files(self, temp_cache_dir):
        """Test that clear returns 0 when no files exist."""
        cache = HtmlCache(store_folder=temp_cache_dir)
        result = cache.clear("https://example.com/nonexistent")
        assert result == 0
