namespace NoMoreBets.Features.SoccerData;

/// <summary>Thrown when SoccerData API returns 401 or 403.</summary>
public class SoccerDataAuthException : SoccerDataException
{
    public SoccerDataAuthException(string message) : base(message) { }
}
