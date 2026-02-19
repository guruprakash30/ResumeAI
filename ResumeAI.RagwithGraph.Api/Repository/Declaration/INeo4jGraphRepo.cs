using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Common.Model;

namespace ResumeAI.RagwithGraph.Api.Repository.Declaration
{
    public interface INeo4jGraphRepo
    {
        public Task<OperationResult> PersistJobAsync(Guid jobId,JobNode job,IEnumerable<SkillRequirement> skills);

        public Task<OperationResult<string?>> GetJobDescriptionSummaryByJobIdAsync(Guid jobId);

        public Task<OperationResult> PersistResumeGraphAsync(Guid candidateId, ResumeGraphNormalizationResult resume);
    }
}
