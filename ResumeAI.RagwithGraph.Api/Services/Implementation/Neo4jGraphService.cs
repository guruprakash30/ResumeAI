using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Api.Repository.Declaration;
using ResumeAI.RagwithGraph.Api.Services.Declaration;
using ResumeAI.RagwithGraph.Common;
using ResumeAI.RagwithGraph.Common.Model;

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
    }
}
