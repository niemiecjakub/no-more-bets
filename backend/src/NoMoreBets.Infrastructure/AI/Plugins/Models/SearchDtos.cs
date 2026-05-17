using System.Collections.Generic;
using System.ComponentModel;

namespace NoMoreBets.Infrastructure.AI.Plugins.Models;

public record SearchNewsArticleDto(
    [Description("The headline or title of the news article.")] string Title,
    [Description("The name of the news outlet or domain")] string Source,
    [Description("A list of text excerpts.")] IReadOnlyList<string> Snippets,
    [Description("How long ago the article was published (e.g., '5 minutes ago', '2 days ago').")] string? Age);

public record SearchLlmContextItemDto(
    [Description("A list of curated snippets from the web page that are relevant to the query and used for grounding.")]  IReadOnlyList<string> Snippets,    
    [Description("The title of the web page.")]  string? Title,  
    [Description("The hostname of the source (e.g., 'wikipedia.org').")]  string? Hostname,   
    [Description("A string representing the age of the source document.")]  string? Age);