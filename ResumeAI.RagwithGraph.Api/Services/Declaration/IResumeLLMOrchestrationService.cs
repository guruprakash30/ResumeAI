using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Api.Repository.Declaration;
using ResumeAI.RagwithGraph.Common;
using ResumeAI.RagwithGraph.Common.Model;

namespace ResumeAI.RagwithGraph.Api.Services.Declaration
{
    public interface IResumeLLMOrchestrationService
    {
        public Task<OperationResult<ResumeGraphNormalizationResult>> ProcessResumeFromBlobAsync(string fileLocation);
    }
}
