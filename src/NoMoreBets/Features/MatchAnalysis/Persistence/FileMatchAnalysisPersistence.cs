using System.Text.Json;
using Microsoft.Extensions.Options;
using NoMoreBets.Features.MatchAnalysis.Options;

namespace NoMoreBets.Features.MatchAnalysis.Persistence;

/// <summary>
/// Persists match analysis results to a JSON file with timestamped name.
/// </summary>
public sealed class FileMatchAnalysisPersistence : IMatchAnalysisPersistence
{
  private readonly MatchAnalysisOptions _options;
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  public FileMatchAnalysisPersistence(IOptions<MatchAnalysisOptions> options)
  {
    _options = options.Value;
  }

  /// <inheritdoc />
  public async Task<string> SaveResultsAsync(IReadOnlyList<Model.MatchAnalysis> results, CancellationToken cancellationToken = default)
  {
    var dir = _options.OutputDirectory;

    Directory.CreateDirectory(dir);

    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var fileName = $"match_analysis_{timestamp}.json";
    var path = Path.Combine(dir, fileName);

    await using var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    await JsonSerializer.SerializeAsync(fs, results, JsonOptions, cancellationToken).ConfigureAwait(false);

    return path;
  }
}
