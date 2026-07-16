namespace NoMoreBets.Application.Common;

public interface IDocumentChunkIndexScheduler
{
  void Enqueue(string sourceType, int sourceId);
}
