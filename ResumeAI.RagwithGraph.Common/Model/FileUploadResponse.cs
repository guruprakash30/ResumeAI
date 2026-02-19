using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeAI.RagwithGraph.Common.Model
{
    public class FileUploadResponse
    {
        public Guid FileID { get; set; }
        public string FileName { get; set; }
        public long FileSizeInBytes { get; set; }
        public string? FileLocation { get; set; } = null;
        public string? Error { get; set; } = null;
    }
}
