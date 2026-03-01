namespace NoMoreBets.Infrastructure.Scraping.External.SoccerData.Exceptions;
/// <summary>Base exception for SoccerData API errors.</summary>
public class SoccerDataException : Exception
{
    public SoccerDataException(string message) : base(message) { }

    public SoccerDataException(string message, Exception inner) : base(message, inner) { }
}
