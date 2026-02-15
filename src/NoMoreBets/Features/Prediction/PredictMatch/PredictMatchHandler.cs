using MediatR;

namespace NoMoreBets.Features.Prediction.PredictMatch;

/// <summary>
/// CQRS handler entrypoint for match prediction feature.
/// </summary>
public sealed class PredictMatchHandler(IPredictMatchAgentOrchestrator orchestrator) : IRequestHandler<PredictMatchQuery, PredictMatchResult>
{
    public Task<PredictMatchResult> Handle(PredictMatchQuery request, CancellationToken cancellationToken)
    {
        return orchestrator.RunAsync(request, cancellationToken);
    }
}
