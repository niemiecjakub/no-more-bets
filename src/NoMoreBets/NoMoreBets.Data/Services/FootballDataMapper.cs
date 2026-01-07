using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using NoMoreBets.Data.Models;

namespace NoMoreBets.Data.Services;

/// <summary>
/// Service responsible for loading and mapping CSV files to domain models.
/// Handles file discovery, CSV reading, and transformation to structured Match objects.
/// </summary>
public class FootballDataMapper
{
    private readonly string _resourcesPath;
    private readonly MatchTransformer _transformer;

    public FootballDataMapper(string? resourcesPath = null, MatchTransformer? transformer = null)
    {
        _resourcesPath = resourcesPath ?? GetDefaultResourcesPath();
        _transformer = transformer ?? new MatchTransformer();
    }

    /// <summary>
    /// Loads all matches from all CSV files in the Resources directory.
    /// </summary>
    public IEnumerable<Match> LoadAllMatches()
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
        return _transformer.Transform(rawMatches);
    }

    /// <summary>
    /// Reads raw match data from CSV file without transformation.
    /// </summary>
    private IEnumerable<MatchRaw> ReadRawMatchesFromFile(string filePath)
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
        // Try multiple possible locations for the Resources folder
        var possiblePaths = new List<string>();

        // 1. Try relative to current working directory (for development)
        var currentDirectory = Directory.GetCurrentDirectory();
        possiblePaths.Add(Path.Combine(currentDirectory, "Resources"));
        possiblePaths.Add(Path.Combine(currentDirectory, "..", "Resources"));
        possiblePaths.Add(Path.Combine(currentDirectory, "..", "..", "Resources"));

        // 2. Try relative to assembly location (for runtime)
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                possiblePaths.Add(Path.Combine(assemblyDirectory, "Resources"));
            }
        }

        // 3. Try AppContext.BaseDirectory
        var baseDirectory = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            possiblePaths.Add(Path.Combine(baseDirectory, "Resources"));
        }

        // 4. Try relative to project structure
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        possiblePaths.Add(Path.Combine(projectPath, "src", "NoMoreBets", "NoMoreBets.Data", "Resources"));

        // Find the first existing path
        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new DirectoryNotFoundException($"Resources directory not found. Searched in: {string.Join(", ", possiblePaths.Select(Path.GetFullPath))}");
    }
}

