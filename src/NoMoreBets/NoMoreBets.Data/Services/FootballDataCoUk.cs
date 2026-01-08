using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using NoMoreBets.Data.Mapper;
using NoMoreBets.Data.Models;

namespace NoMoreBets.Data.Services;

public class FootballDataCoUk
{
  private readonly string _resourcesPath;

  public FootballDataCoUk(string? resourcesPath = null)
  {
    _resourcesPath = resourcesPath ?? GetDefaultResourcesPath();
  }

  /// <summary>
  /// Loads all matches from all CSV files.
  /// </summary>
  public List<Match> LoadAllMatches()
  {
    var csvFiles = Directory.GetFiles(_resourcesPath, "*.csv", SearchOption.TopDirectoryOnly);

    if (csvFiles.Length == 0)
    {
      throw new FileNotFoundException($"No CSV files found in directory: {_resourcesPath}");
    }

    var allMatches = new List<Match>();

    foreach (var csvFile in csvFiles)
    {
      try
      {
        var matches = LoadMatchesFromFile(csvFile);
        allMatches.AddRange(matches);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Error loading matches from file '{csvFile}': {ex.Message}", ex);
      }
    }

    return allMatches;
  }

  /// <summary>
  /// Loads matches from a single CSV file.
  /// </summary>
  private IEnumerable<Match> LoadMatchesFromFile(string filePath)
  {
    var rawMatches = ReadRawMatchesFromFile(filePath);
    return rawMatches.ToMatches();
  }

  /// <summary>
  /// Reads raw match data from CSV file.
  /// </summary>
  private List<MatchRaw> ReadRawMatchesFromFile(string filePath)
  {
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
      HasHeaderRecord = true,
      MissingFieldFound = null,
      HeaderValidated = null
    };

    using var reader = new StreamReader(filePath);
    using var csv = new CsvReader(reader, config);

    return csv.GetRecords<MatchRaw>().ToList();
  }

  private static string GetDefaultResourcesPath()
  {
    var currentDirectory = Directory.GetCurrentDirectory();
    var path = Path.Combine(currentDirectory, "Resources");

    var fullPath = Path.GetFullPath(path);
    if (Directory.Exists(fullPath))
    {
      return fullPath;
    }

    throw new DirectoryNotFoundException($"Resources directory not found.");
  }
}

