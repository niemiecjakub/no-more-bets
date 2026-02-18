namespace NoMoreBets.Features.SoccerData.Exceptions;

/// <summary>Thrown when SoccerData API returns 404.</summary>
public class SoccerDataNotFoundException : SoccerDataException
{
    public SoccerDataNotFoundException(string message) : base(message) { }
}
