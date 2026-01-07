using NoMoreBets.Data.Models;
using NoMoreBets.Data.Services;

var footballService = new FootballDataMapper();
IEnumerable<Match> result = footballService.LoadAllMatches();
