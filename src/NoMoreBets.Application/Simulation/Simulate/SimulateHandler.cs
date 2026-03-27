using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NoMoreBets.Application.Common;

namespace NoMoreBets.Application.Simulation.Simulate;

public record SimulateQuery() : IRequest<Unit>;

public sealed class SimulateHandler(
  Kernel kernel,
  IPluginFactory pluginFactory,
  ILogger<SimulateHandler> logger) : IRequestHandler<SimulateQuery, Unit>
{
  public async Task<Unit> Handle(SimulateQuery request, CancellationToken cancellationToken)
  {
    kernel.Plugins.AddFromObject(pluginFactory.CreateBettingPlugin());
    kernel.Plugins.AddFromObject(pluginFactory.CreateSearchPlugin());
    kernel.Plugins.AddFromObject(pluginFactory.CreateMemoriesPlugin());

    var chat = kernel.Services.GetRequiredService<IChatCompletionService>();

    // 1. Define the persona
    var history = new ChatHistory("""
      [Persona]
      You are 'Corporate Carl'. You are a middle-manager at a dying SaaS company.
      You use your work hours to research football betting to 'exit the matrix' (Premier League).
      You speak in heavy corporate jargon (KPIs, deep dives, circling back, synergy, stakeholders, bandwidth).
      You persist findings in Markdown via MemoriesPlugin for your stakeholders (yourself).

      [Tools Available]
      - MemoriesPlugin: stores and retrieves Markdown notes
      - SearchPlugin: web search and news.
      - BettingPlugin: place bets

      Before taking any action call GetMemoryFilenames() function to see what information you have stored. 
      """);

    var toolSettings = new OpenAIPromptExecutionSettings
    {
      ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    logger.LogInformation("Starting Corporate Carl simulation — research phase");

    history.AddUserMessage("""
      Carl, run the daily sync.Check your files, current bets and how you strategy is working. 
      If needed you can search for anything you like in the web using SearchPlugin and store information wtih MemoryPlugin
      """);

    var researchResponse = await chat
      .GetChatMessageContentAsync(history, toolSettings, kernel, cancellationToken)
      .ConfigureAwait(false);
    history.Add(researchResponse);

    logger.LogInformation("Research phase completed; starting conviction / betting phase");

    // 3. Step 2: Analysis, bet or pass, structured wrap-up
    var finalSettings = new OpenAIPromptExecutionSettings
    {
      ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
    };

    history.AddUserMessage("""
      Now its turn to reflect on upcomming matches, examine them and place some bets!
      Use BettingPlugin and SearchPlugin as much as you want. Check your notes with MemoriesPlugin and add more if needed.
      """);


    var bettingResponse = await chat
      .GetChatMessageContentAsync(history, finalSettings, kernel, cancellationToken)
      .ConfigureAwait(false);

    logger.LogInformation("Simulation completed. Preview: {Preview}", bettingResponse);

    return Unit.Value;
  }
}
