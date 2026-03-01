namespace NoMoreBets.Infrastructure.Scraping;

/// <summary>
/// Thrown when a fetch fails with a permanent HTTP status (403, 404, 410).
/// BaseScraper does not retry on this exception.
/// </summary>
public class PermanentScraperException : Exception
{
    public int? StatusCode { get; }

    public PermanentScraperException(string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
