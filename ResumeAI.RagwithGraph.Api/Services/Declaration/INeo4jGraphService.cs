using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Common.Model;

namespace ResumeAI.RagwithGraph.Api.Services.Declaration
{
    public interface INeo4jGraphService
    {
        public Task<OperationResult<Guid>> PersistNormalizedJobAsync(JobNormalizationResult normalized);
        public Task<OperationResult<string?>> GetJobDescriptionSummaryByJobIdAsync(Guid jobId);

        public Task<OperationResult<Guid>> PersistResumeAsync(ResumeGraphNormalizationResult resume);
        public Task<OperationResult<RankedCandidatesResponse>> GetRankedCandidatesAsync(Guid jobId);

        /// <summary>
        /// Executes a list of Cypher queries on the Neo4j candidate graph and aggregates the results.
        /// 
        /// Each query is executed with the provided parameters, and all records returned are serialized
        /// as JSON strings. The method returns a list of these serialized record strings.
        /// </summary>
        Task<List<string>> ExecuteHrQueryAndAggregateAsync(List<string> cypherQueries, List<string> rankedCandidateIds);

    }
}
