using Neo4j.Driver;
using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Common.Model;

namespace ResumeAI.RagwithGraph.Api.Repository.Declaration
{
    public interface INeo4jGraphRepo
    {
        public Task<OperationResult> PersistJobAsync(Guid jobId,JobNode job,IEnumerable<SkillRequirement> skills);
        public Task<OperationResult<string?>> GetJobDescriptionSummaryByJobIdAsync(Guid jobId);
        public Task<OperationResult> PersistResumeGraphAsync(Guid candidateId, ResumeGraphNormalizationResult resume);
        public Task<OperationResult<List<RankedCandidateResult>>> GetRankedCandidatesByJobIdAsync(Guid jobId);
        
        // <summary>
        /// Executes a Cypher query against the candidate graph.
        /// Expects $ranked_candidate_ids parameter to be passed dynamically.
        /// </summary>
        /// <param name="cypherQuery">The Cypher query to execute</param>
        /// <param name="parameters">Query parameters, e.g., ranked_candidate_ids</param>
        /// <returns>List of raw records returned from the query</returns>
        public Task<IReadOnlyList<IRecord>> ExecuteHrQueryAsync(string cypherQuery, IDictionary<string, object> parameters);
    }
}
