using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using ResumeAI.RagwithGraph.Api.Services.Declaration;
using System.Numerics;

namespace ResumeAI.RagwithGraph.Api.Services.Implementation
{
    public class AISearchService : IAISearchService
    {
        private readonly SearchClient _searchClient;
        private readonly AzureOpenAIClient _azureOpenAIClient;
        private readonly string _embeddingDeployment = "text-embedding-3-small";

        public AISearchService()
        {
            var credential = new DefaultAzureCredential();

            // Azure Cognitive Search client
            _searchClient = new SearchClient(new Uri("https://resume-rag-ai-search.search.windows.net"),"rag-1768540826117",credential);

            // Azure OpenAI client for embeddings
            _azureOpenAIClient = new AzureOpenAIClient(new Uri("https://openai-llm-for-hr-resume-rag.openai.azure.com/"),credential);
        }

        public async Task<IReadOnlyList<(SearchDocument Document, double Score)>> HybridSearchAsync(string query)
        {
            
            var embeddingClient =  _azureOpenAIClient.GetEmbeddingClient(_embeddingDeployment);
            var embeddingResponse = await embeddingClient.GenerateEmbeddingAsync(query);
            var queryEmbedding = embeddingResponse.Value.ToFloats();

            var vectorOptions = new VectorizedQuery(queryEmbedding)
            {
                KNearestNeighborsCount = 50,
                Fields = { "text_vector" }
            };

            var options = new SearchOptions
            {
                Size = 10,
                Select = { "chunk_id" },
                VectorSearch = new()
                {
                    Queries = { vectorOptions }
                }
            };

            var response = await _searchClient.SearchAsync<SearchDocument>(
                searchText: query, // keyword search
                options: options
            );

            var results = response.Value.GetResults()
                .Select(r => (r.Document, r.Score.HasValue ? r.Score.Value : 0))
                .OrderByDescending(r => r.Item2)
                .Take(10)
                .ToList();

            return results;
        }
    }
}
