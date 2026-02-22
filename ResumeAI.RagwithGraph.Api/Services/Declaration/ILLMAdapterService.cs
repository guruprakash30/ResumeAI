using ResumeAI.RagwithGraph.Api.Model;

namespace ResumeAI.RagwithGraph.Api.Services.Declaration
{
    public interface ILLMAdapterService
    {
        public Task<List<string>> GenerateSearchQueriesAsync(string prompt);
        public Task<JobNormalizationResult?> NormalizeJobDescriptionAsync(string jobDescription);
        public Task<ResumeGraphNormalizationResult?> NormalizeResumeAsync(string resumeText);
        public Task<List<string>> GenerateHrCypherQueriesAsync(string hrQuery);
        public Task<string> ReasonHrQueryWithCandidatesAsync(string hrQuery, RankedCandidatesResponse rankedCandidates, object answerToHrQuery, string aiSearchChunks);
    }
}
