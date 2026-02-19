using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Common.Model;

namespace ResumeAI.RagwithGraph.Api.Services.Declaration
{
    public interface INeo4jGraphService
    {
        public Task<OperationResult<Guid>> PersistNormalizedJobAsync(JobNormalizationResult normalized);
        public Task<OperationResult<string?>> GetJobDescriptionSummaryByJobIdAsync(Guid jobId);

        public Task<OperationResult<Guid>> PersistResumeAsync(ResumeGraphNormalizationResult resume);
    }
}
