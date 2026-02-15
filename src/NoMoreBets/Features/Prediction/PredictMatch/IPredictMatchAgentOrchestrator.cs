namespace NoMoreBets.Features.Prediction.PredictMatch;

public interface IPredictMatchAgentOrchestrator
{
    Task<PredictMatchResult> RunAsync(PredictMatchQuery query, CancellationToken cancellationToken);
}
