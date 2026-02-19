using Microsoft.AspNetCore.WebUtilities;
using ResumeAI.RagwithGraph.Common;

namespace ResumeAI.RagwithGraph.Api.Model
{
    public class RequestFileUpload
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; }
        public MultipartReader Reader { get; set; }
        public BlobStorageService BlobStorageService { get; set; }
    }
}
