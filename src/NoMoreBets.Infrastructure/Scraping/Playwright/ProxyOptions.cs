namespace NoMoreBets.Infrastructure.Scraping.Playwright;

public class ProxyOptions
{
  public const string SectionName = "ProxyOptions";
  public string ProxyUser { get; set; } = string.Empty;
  public string ProxyPassword { get; set; } = string.Empty;
  public string ProxyServer { get; set; } = string.Empty;

  public bool IsValid() =>
    !string.IsNullOrWhiteSpace(ProxyUser) &&
    !string.IsNullOrWhiteSpace(ProxyPassword) &&
    !string.IsNullOrWhiteSpace(ProxyServer);
}
