using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Api.Repository.Declaration;
using ResumeAI.RagwithGraph.Api.Services.Declaration;
using ResumeAI.RagwithGraph.Common;
using ResumeAI.RagwithGraph.Common.Model;

namespace ResumeAI.RagwithGraph.Api.Services.Implementation
{
    public class ResumeLLMOrchestrationService : IResumeLLMOrchestrationService
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly IResumeTextExtractor _textExtractor;
        private readonly ILLMAdapterService _lLMAdapterService;
        public ResumeLLMOrchestrationService(BlobStorageService blobStorageService, IResumeTextExtractor textExtractor, ILLMAdapterService llmAdapter)
        {
            _blobStorageService = blobStorageService;
            _textExtractor = textExtractor;
            _lLMAdapterService = llmAdapter;
        }
        public async Task<OperationResult<ResumeGraphNormalizationResult>> ProcessResumeFromBlobAsync(string fileLocation)
        {
            OperationResult<ResumeGraphNormalizationResult> sRes = new();

            try
            {
                var blobStreamRes = await _blobStorageService.OpenBlobReadStreamAsync(fileLocation);

                if (blobStreamRes.ExecutionState != ExecutionState.Success)
                    return sRes.Failure();

                await using var stream = blobStreamRes.Data!;

                var fileName = Path.GetFileName(fileLocation);

                var resumeText = await _textExtractor.ExtractTextAsync(stream, fileName);

                if (string.IsNullOrWhiteSpace(resumeText))
                    throw new InvalidOperationException("Extracted resume text is empty.");

                var normalized = await _lLMAdapterService.NormalizeResumeAsync(resumeText);

                if (normalized == null)
                    return sRes.Failure();

                return sRes.Success(normalized);
            }
            catch (Exception ex)
            {
                return sRes.Failure();
            }
        }

    }
}
