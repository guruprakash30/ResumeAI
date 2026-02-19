using Neo4j.Driver;
using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Api.Repository.Declaration;
using ResumeAI.RagwithGraph.Api.Services.Declaration;
using ResumeAI.RagwithGraph.Common;
using ResumeAI.RagwithGraph.Common.Model;
using System.Text.Json;

namespace ResumeAI.RagwithGraph.Api.Services.Implementation
{
    public class Neo4jGraphService : INeo4jGraphService
    {
        private readonly INeo4jGraphRepo _repository;
        private readonly ILogger<Neo4jGraphService> _logger;

        public Neo4jGraphService(INeo4jGraphRepo repository, ILogger<Neo4jGraphService> logger)
        {
            _repository = repository;
            _logger = logger;

        }

        public async Task<OperationResult<Guid>> PersistNormalizedJobAsync(JobNormalizationResult normalized)
        {
            OperationResult<Guid> sRes = new();
            try
            {
                var jobId = Guid.NewGuid();

                var res = await _repository.PersistJobAsync(jobId, normalized.Job, normalized.Skills);

                if (res.ExecutionState == ExecutionState.Pending || res.ExecutionState == ExecutionState.Failure)
                    return sRes.Failure();

                return sRes.Success(jobId);
            }
            catch(Exception ex)
            {
                await CommonLogger.LogExceptionAsync(_logger, ex, null);
                return sRes.Failure();
            }
        }

        public async Task<OperationResult<string?>> GetJobDescriptionSummaryByJobIdAsync(Guid jobId)
        {
            OperationResult<string?> sRes = new();
            try
            {
                var res = await _repository.GetJobDescriptionSummaryByJobIdAsync(jobId);
                if (res.ExecutionState == ExecutionState.Pending || res.ExecutionState == ExecutionState.Failure)
                    return sRes.Failure();

                return sRes.Success(res.Data);
            }
            catch (Exception ex)
            {
                await CommonLogger.LogExceptionAsync(_logger, ex, jobId.ToString());
                return sRes.Failure();
            }
        }

        public async Task<OperationResult<Guid>> PersistResumeAsync(ResumeGraphNormalizationResult resume)
        {
            var sRes = new OperationResult<Guid>();
            try
            {
                var candidateId = resume.Candidate.Candidate_Id ?? Guid.NewGuid();

                var res = await _repository.PersistResumeGraphAsync(candidateId, resume);

                if (res.ExecutionState == ExecutionState.Failure || res.ExecutionState == ExecutionState.Pending)
                    return sRes.Failure();

                return sRes.Success(candidateId);
            }
            catch (Exception ex)
            {
                await CommonLogger.LogExceptionAsync(_logger, ex, null);
                return sRes.Failure();
            }
        }

        public async Task<OperationResult<RankedCandidatesResponse>> GetRankedCandidatesAsync(Guid jobId)
        {
            OperationResult<RankedCandidatesResponse> result = new();

            try
            {
                if (jobId == Guid.Empty) return result.Failure();

                var repoResult = await _repository.GetRankedCandidatesByJobIdAsync(jobId);

                if (repoResult.ExecutionState != ExecutionState.Success || 
                    repoResult.ExecutionState == ExecutionState.Pending || 
                    repoResult.Data is null)
                    return result.Failure();

                var candidates = repoResult.Data;

                var response = new RankedCandidatesResponse
                {
                    JobId = jobId,
                    TotalCandidates = candidates.Count,
                    Candidates = candidates,
                    RankedCandidateIds = candidates
                        .Select(c => c.CandidateId)
                        .ToList()
                };

                _logger.LogInformation(
                    "Ranked {Count} candidates for JobId {JobId}",
                    response.TotalCandidates,
                    jobId);

                return result.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error ranking candidates for JobId {JobId}", jobId);

                return result.Failure();
            }
        }
        public async Task<List<string>> ExecuteHrQueryAndAggregateAsync(List<string> cypherQueries, List<string> rankedCandidateIds)
        {
            var aggregatedResults = new List<string>();

            foreach (var query in cypherQueries)
            {
                // 2. Execute each query on Neo4j with the ranked candidate IDs
                var records = await _repository.ExecuteHrQueryAsync(query, new Dictionary<string, object>
                {
                    ["ranked_candidate_ids"] = rankedCandidateIds
                });

                // 3. Serialize each record as JSON text for LLM input
                foreach (var record in records)
                {
                    // Convert all fields in record to dictionary
                    var dict = record.Keys.ToDictionary(k => k, k => record[k]?.ToString());
                    var json = JsonSerializer.Serialize(dict);
                    aggregatedResults.Add(json);
                }
            }

            return aggregatedResults;
        }

    }
}
