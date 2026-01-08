using NoMoreBets.Data.Models;
using NoMoreBets.Data.Services;

var footballData = new FootballDataCoUk();
IEnumerable<Match> result = footballData.LoadAllMatches();

Console.WriteLine($"Done. Read {result.Count()} matches.");
Console.ReadLine();