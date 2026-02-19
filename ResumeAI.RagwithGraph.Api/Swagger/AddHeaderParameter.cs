using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ResumeAI.RagwithGraph.Api.Swagger
{
    public class AddHeaderParameter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var routePath = context.ApiDescription.RelativePath;

            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            if (routePath == "resume-rag-aiservice/v1/file-upload")
            {

                operation.RequestBody = new OpenApiRequestBody
                {
                    Content =
                            {
                                ["multipart/form-data"] = new OpenApiMediaType
                                {
                                    Schema = new OpenApiSchema
                                    {
                                        Type = "object",
                                        Properties =
                                        {
                                            ["file"] = new OpenApiSchema
                                            {
                                                Type = "string",
                                                Format = "binary"
                                            }
                                        },
                                        Required = new HashSet<string> { "file" }
                                    }
                                }
                            },
                    Required = true
                };

            }
        }
    }
}
