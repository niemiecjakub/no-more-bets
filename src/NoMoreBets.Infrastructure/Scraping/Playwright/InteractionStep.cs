namespace NoMoreBets.Infrastructure.Scraping.Playwright;

/// <summary>
/// Describes a single interaction to perform on a page (e.g. click) before capturing HTML.
/// Used by <see cref="PlaywrightPageFetcher"/> for interactive fetch; selectors are supplied by the caller.
/// </summary>
/// <param name="Selector">CSS selector for the element to interact with.</param>
/// <param name="Action">Action to perform (e.g. Click).</param>
/// <param name="DelayAfterMs">Optional delay in milliseconds after the action. Default 300.</param>
public record InteractionStep(
    string Selector,
    InteractionAction Action = InteractionAction.Click,
    int DelayAfterMs = 300);

/// <summary>Action to perform on an element.</summary>
public enum InteractionAction
{
    Click
}
