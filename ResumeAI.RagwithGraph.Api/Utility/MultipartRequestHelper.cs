using Microsoft.Net.Http.Headers;
using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Common.Model;
using System.Net.Mime;

namespace ResumeAI.RagwithGraph.Api.Utility
{
    public static class MultipartRequestHelper
    {
        public static bool HasRequestHeadersAsExpected(HttpRequest request)
        {
            bool contentLengthCheck = request.Headers.ContainsKey("Content-Length");
            bool transferEncodingCheck = (request.Headers.ContainsKey("Transfer-Encoding")
                && request.Headers["Transfer-Encoding"] == "chunked");

            return (contentLengthCheck && !transferEncodingCheck) || (transferEncodingCheck && !contentLengthCheck);
        }

        public static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new Exception("Missing content-type boundary.");
            }

            if (boundary.Length > lengthLimit)
            {
                throw new Exception(
                    $"Multipart boundary length limit {lengthLimit} exceeded.");
            }

            return boundary;
        }

        public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="key";
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data")
                && string.IsNullOrEmpty(contentDisposition.FileName.Value)
                && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
        }

        public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                    || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

        public static async Task<OperationResult<FileUploadResponse>> HandleMultipartUploadAsync(RequestFileUpload requestDTO)
        {
            OperationResult<FileUploadResponse> operationResult = null;

            var section = await requestDTO.Reader.ReadNextSectionAsync();

            if(section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);

                if (!hasContentDispositionHeader || !MultipartRequestHelper.HasFileContentDisposition(contentDisposition) || MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                {
                    throw new Exception(message: "The error could be File is required and its missing");
                }

                requestDTO.FileId = Guid.NewGuid();
                requestDTO.FileName = contentDisposition.FileName.Value;

                var res = await requestDTO.BlobStorageService.SaveFileToBlobAsync(requestDTO, section.Body);

                operationResult = res;
            }
            else operationResult = new OperationResult<FileUploadResponse>().Failure();

            return operationResult;
        }
    }
}
