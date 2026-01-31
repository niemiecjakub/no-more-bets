using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NoMoreBets.Infrastructure.Storage;

namespace NoMoreBets.Tests.Infrastructure.Storage;

public class JsonCacheTests
{
    private static string CreateTempDir() => Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    private static JsonCache CreateSut(
        string? storeFolder = null,
        bool store = true,
        bool useCache = true,
        double cacheTtlSeconds = 86400)
    {
        var folder = storeFolder ?? CreateTempDir();
        Directory.CreateDirectory(folder);
        var options = Options.Create(new JsonCacheOptions
        {
            StoreFolder = folder,
            Store = store,
            UseCache = useCache,
            CacheTtlSeconds = cacheTtlSeconds
        });
        var logger = NullLogger<JsonCache>.Instance;
        var env = Substitute.For<Microsoft.Extensions.Hosting.IHostEnvironment>();
        env.ContentRootPath.Returns(Path.GetTempPath());
        return new JsonCache(options, logger, env);
    }

    [Fact]
    public async Task LoadAsync_WhenUseCacheDisabled_ReturnsNull()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir, useCache: false);
        var key = "test-key";

        // Act
        var result = await sut.LoadAsync(key);

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
        var key = "nonexistent";

        // Act
        var result = await sut.LoadAsync(key);

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
        var key = "endpoint";
        var data = JsonSerializer.SerializeToElement(new { value = 42 });
        await sut.SaveAsync(key, data);

        // Act
        var result = await sut.LoadAsync(key);

        // Assert
        result.Should().NotBeNull();
        result.Value.GetProperty("value").GetInt32().Should().Be(42);
    }

    [Fact]
    public async Task LoadAsync_WhenCacheExpired_ReturnsNull()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir, cacheTtlSeconds: 3600);
        var key = "endpoint";
        var oldTimestamp = 1000000L; // Very old
        var filename = $"{key}_{oldTimestamp}.json";
        var filepath = Path.Combine(tempDir, filename);
        await File.WriteAllTextAsync(filepath, """{"expired":true}""");

        // Act
        var result = await sut.LoadAsync(key);

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
        var key = "key";
        var data = JsonSerializer.SerializeToElement(new { x = 1 });

        // Act
        await sut.SaveAsync(key, data);

        // Assert
        var files = Directory.EnumerateFiles(tempDir, $"{key}_*.json").ToList();
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WhenStoreEnabled_WritesFileAndRemovesOldEntries()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir);
        var key = "key";
        var data = JsonSerializer.SerializeToElement(new { a = 1 });
        await sut.SaveAsync(key, data);
        var firstFileCount = Directory.EnumerateFiles(tempDir, $"{key}_*.json").Count();

        // Act
        await sut.SaveAsync(key, JsonSerializer.SerializeToElement(new { a = 2 }));

        // Assert
        var files = Directory.EnumerateFiles(tempDir, $"{key}_*.json").ToList();
        files.Should().ContainSingle();
        var content = await File.ReadAllTextAsync(files[0]);
        content.Should().Contain("\"a\": 2");
    }

    [Fact]
    public async Task ClearAsync_WhenKeyExists_ReturnsRemovedCount()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir);
        var key = "clear-me";
        await sut.SaveAsync(key, JsonSerializer.SerializeToElement(new { }));

        // Act
        var count = await sut.ClearAsync(key);

        // Assert
        count.Should().Be(1);
        Directory.EnumerateFiles(tempDir, $"{key}_*.json").Should().BeEmpty();
    }

    [Fact]
    public async Task ClearAsync_WhenNoFiles_ReturnsZero()
    {
        // Arrange
        var tempDir = CreateTempDir();
        Directory.CreateDirectory(tempDir);
        var sut = CreateSut(storeFolder: tempDir);
        var key = "nonexistent";

        // Act
        var count = await sut.ClearAsync(key);

        // Assert
        count.Should().Be(0);
    }
}
