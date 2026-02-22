using Azure.Search.Documents.Models;

namespace ResumeAI.RagwithGraph.Api.Services.Declaration
{
    public interface IAISearchService
    {
        public Task<IReadOnlyList<(SearchDocument Document, double Score)>> HybridSearchAsync(string query);
        public Task<string> GetChunksForLLmAsync(IEnumerable<string> chunkIds);
    }
}
