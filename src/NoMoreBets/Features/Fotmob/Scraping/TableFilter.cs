namespace NoMoreBets.Features.Fotmob.Scraping;

/// <summary>
/// Filter for league table view (all, home, away, or form).
/// </summary>
public enum TableFilter
{
    /// <summary>Full league table.</summary>
    All,

    /// <summary>Home matches only.</summary>
    Home,

    /// <summary>Away matches only.</summary>
    Away,

    /// <summary>Table sorted by form (last 5 games).</summary>
    Form
}
