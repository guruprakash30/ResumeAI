using Azure.Core;
using Azure.Search.Documents.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Services;
using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Api.Services.Declaration;
using ResumeAI.RagwithGraph.Api.Services.Implementation;
using ResumeAI.RagwithGraph.Api.Utility;
using ResumeAI.RagwithGraph.Common;
using ResumeAI.RagwithGraph.Common.Model;
using System.Net;

namespace ResumeAI.RagwithGraph.Api.Endpoints
{
    public static class FileEndpoints
    {
        public class FileExternalAPi { }
        public static void MapFileEndpoints(this WebApplication app)
        {
            app.MapPost("resume-rag-aiservice/v1/upload-resume", async (ILogger<FileExternalAPi> logger, HttpContext context, BlobStorageService blobStorageService, IResumeLLMOrchestrationService resumeLLMOrchestrationService, INeo4jGraphService neo4JGraphService) =>
            {

                var request = context.Request;
                var formOptions = new FormOptions();

                try
                {

                    if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType) || !MultipartRequestHelper.HasRequestHeadersAsExpected(request))
                    {
                        throw new Exception(message: "Invalid form-data request.");
                    }

                    var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(request.ContentType), formOptions.MultipartBoundaryLengthLimit);

                    var reader = new MultipartReader(boundary, request.Body);

                    var requestDTO = new RequestFileUpload
                    {
                        Reader = reader,
                        BlobStorageService = blobStorageService,
                    };

                    var res = await MultipartRequestHelper.HandleMultipartUploadAsync(requestDTO);

                    if(res.ExecutionState == ExecutionState.Pending || res.ExecutionState == ExecutionState.Failure)
                    {
                        var apiError = new ApiError
                        {
                            Type = $"{request.Scheme}://{WebUtility.HtmlEncode(request.Host.ToString())}/errors/#expectation-failed",
                            Title = "Expectation failed",
                            Status = StatusCodes.Status417ExpectationFailed,
                            Detail = "Please try again later or contact your system administrator for assistance.",
                            Instance = WebUtility.HtmlEncode(request.Path)
                        };

                        return Results.Json(apiError, statusCode: StatusCodes.Status417ExpectationFailed);
                    }

                    var resumeGraphNormalized = await resumeLLMOrchestrationService.ProcessResumeFromBlobAsync(res.Data.FileLocation);

                    if(resumeGraphNormalized.ExecutionState == ExecutionState.Pending || resumeGraphNormalized.ExecutionState == ExecutionState.Failure)
                    {
                        {
                            var apiError = new ApiError
                            {
                                Type = $"{request.Scheme}://{WebUtility.HtmlEncode(request.Host.ToString())}/errors/#expectation-failed",
                                Title = "Expectation failed",
                                Status = StatusCodes.Status417ExpectationFailed,
                                Detail = "Please try again later or contact your system administrator for assistance.",
                                Instance = WebUtility.HtmlEncode(request.Path)
                            };

                            return Results.Json(apiError, statusCode: StatusCodes.Status417ExpectationFailed);
                        }
                    }

                    var candidateId = await neo4JGraphService.PersistResumeAsync(resumeGraphNormalized.Data!);

                    return Results.Ok(new
                    {
                        CandidateId = candidateId,
                    });
                }
                catch (Exception ex)
                {
                    await CommonLogger.LogExceptionAsync(logger, ex, null);
                    ApiError apiError = new ApiError
                    {
                        Type = $"{request.Scheme}://{WebUtility.HtmlEncode(request.Host.ToString())}/errors/#expectation-failed",
                        Title = "Expectation failed",
                        Status = StatusCodes.Status417ExpectationFailed,
                        Detail = "Please try again later or contact your system administrator for assistance.",
                        Instance = WebUtility.HtmlEncode(request.Path)
                    };

                    return Results.Json(apiError, statusCode: StatusCodes.Status417ExpectationFailed);
                }
            }).WithName("FileUpload")
               .WithSummary("only one file can be uploaded using multipart/form-data.")
               .WithDescription("This endpoint supports only one file upload. Requests must either use 'Transfer-Encoding: chunked' or 'Content-Length' Header. ")
               .WithTags("File Uploads")
              .Accepts<IFormFile>("multipart/form-data")
              .Produces<FileUploadResponse>(StatusCodes.Status200OK)
              .Produces<ApiError>(StatusCodes.Status417ExpectationFailed);

            app.MapPost("resume-rag-aiservice/v1/ask-me/{jobId:guid}", async (Guid jobId, HttpContext context, ILLMAdapterService llmAdapterService, IAISearchService aiSearchService, INeo4jGraphService neo4JGraphService) =>
            {
                var request = context.Request;
                var res = await neo4JGraphService.GetJobDescriptionSummaryByJobIdAsync(jobId);

                if (res.ExecutionState == ExecutionState.Pending || res.ExecutionState == ExecutionState.Failure)
                {
                    var apiError = new ApiError
                    {
                        Type = $"{request.Scheme}://{WebUtility.HtmlEncode(request.Host.ToString())}/errors/#expectation-failed",
                        Title = "Expectation failed",
                        Status = StatusCodes.Status417ExpectationFailed,
                        Detail = "Please try again later or contact your system administrator for assistance.",
                        Instance = WebUtility.HtmlEncode(request.Path)
                    };

                    return Results.Json(apiError, statusCode: StatusCodes.Status417ExpectationFailed);
                }


                List<Task<IReadOnlyList<(SearchDocument Document, double Score)>>> tasks = new();
                var subqueries = await llmAdapterService.GenerateSearchQueriesAsync(res.Data!);
                foreach(var q in subqueries)
                {
                    tasks.Add(aiSearchService.HybridSearchAsync(q));
                }

                var searchResults = await Task.WhenAll(tasks);

                var chunkFrequency = searchResults
                    .SelectMany(resultList => resultList)
                    .Select(r => r.Document["chunk_id"]!.ToString())             
                    .GroupBy(id => id!)                                          
                    .ToDictionary(g => g.Key, g => g.Count());                  

                var orderedChunkFrequency = chunkFrequency
                    .OrderByDescending(kvp => kvp.Value)
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            });

            app.MapPost("resume-rag-aiservice/v1/post-job", async (HttpContext context, ILLMAdapterService llmAdapterService, IAISearchService aiSearchService, INeo4jGraphService neo4JGraphService) =>
            {
                var request = context.Request;
                using var reader = new StreamReader(request.Body);
                var jobDescription = await reader.ReadToEndAsync();
                var normalizedContract = await llmAdapterService.NormalizeJobDescriptionAsync(jobDescription);

                var res = await neo4JGraphService.PersistNormalizedJobAsync(normalizedContract);

                if (res.ExecutionState == ExecutionState.Pending || res.ExecutionState == ExecutionState.Failure)
                {
                    var apiError = new ApiError
                    {
                        Type = $"{request.Scheme}://{WebUtility.HtmlEncode(request.Host.ToString())}/errors/#expectation-failed",
                        Title = "Expectation failed",
                        Status = StatusCodes.Status417ExpectationFailed,
                        Detail = "Please try again later or contact your system administrator for assistance.",
                        Instance = WebUtility.HtmlEncode(request.Path)
                    };

                    return Results.Json(apiError, statusCode: StatusCodes.Status417ExpectationFailed);
                }

                return Results.Ok(new
                {
                    JobId = res.Data,
                    status = "success",
                    message = "Job description normalized and persisted successfully."
                });
            });
        }
    }
}
