using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Tests.Infrastructure.Storage;

public class HtmlCacheTests
{
    private static string CreateTempDir() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private static HtmlCache CreateSut(
        string? storeFolder = null,
        bool store = true,
        bool useCache = true,
        double cacheTtlSeconds = 3600)
    {
        var folder = storeFolder ?? CreateTempDir();
        Directory.CreateDirectory(folder);
        var options = Options.Create(new HtmlCacheOptions
        {
            StoreFolder = folder,
            Store = store,
            UseCache = useCache,
            CacheTtlSeconds = cacheTtlSeconds
        });
        var logger = NullLogger<HtmlCache>.Instance;
        var env = Substitute.For<Microsoft.Extensions.Hosting.IHostEnvironment>();
        env.ContentRootPath.Returns(Path.GetTempPath());
        return new HtmlCache(options, logger, env);
    }

    [Fact]
    public async Task LoadAsync_WhenUseCacheDisabled_ReturnsNull()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir, useCache: false);
        var url = "https://example.com/page";

        // Act
        var result = await sut.LoadAsync(url);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_WhenCacheMiss_ReturnsNull()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir);
        var url = "https://example.com/nonexistent";

        // Act
        var result = await sut.LoadAsync(url);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_WhenValidCachedFileExists_ReturnsContent()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir);
        var url = "https://example.com/page";
        var html = "<html><body>Hello</body></html>";
        await sut.SaveAsync(url, html);

        // Act
        var result = await sut.LoadAsync(url);

        // Assert
        result.Should().Be(html);
    }

    [Fact]
    public async Task LoadAsync_WhenCacheExpired_ReturnsNull()
    {
        // Arrange: HtmlCache uses host_path (with / replaced by _) then -{timestamp}.html
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir, cacheTtlSeconds: 3600);
        var url = "https://example.com/old";
        var baseWithoutExt = "example.com_old"; // Host + path with / -> _
        var oldTimestamp = 1000000L;
        var filename = $"{baseWithoutExt}-{oldTimestamp}.html";
        var filepath = Path.Combine(tempDir, filename);
        await File.WriteAllTextAsync(filepath, "<html>expired</html>");

        // Act
        var result = await sut.LoadAsync(url);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_WhenStoreDisabled_DoesNotWriteFile()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir, store: false);
        var url = "https://example.com/page";
        var html = "<html></html>";

        // Act
        await sut.SaveAsync(url, html);

        // Assert
        var files = Directory.EnumerateFiles(tempDir, "*.html").ToList();
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WhenStoreEnabled_WritesFileAndRemovesOldEntries()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir);
        var url = "https://example.com/page";
        await sut.SaveAsync(url, "<html>v1</html>");

        // Act
        await sut.SaveAsync(url, "<html>v2</html>");

        // Assert
        var files = Directory.EnumerateFiles(tempDir, "*.html").ToList();
        files.Should().ContainSingle();
        var content = await File.ReadAllTextAsync(files[0]);
        content.Should().Be("<html>v2</html>");
    }

    [Fact]
    public async Task ClearAsync_WhenKeyExists_ReturnsRemovedCount()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir);
        var url = "https://example.com/clear-me";
        await sut.SaveAsync(url, "<html></html>");

        // Act
        var count = await sut.ClearAsync(url);

        // Assert
        count.Should().Be(1);
        Directory.EnumerateFiles(tempDir, "*.html").Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsync_WhenNoFiles_ReturnsZero()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir);
        var url = "https://example.com/nonexistent";

        // Act
        var count = await sut.ClearAsync(url);

        // Assert
        count.Should().Be(0);
    }
}
