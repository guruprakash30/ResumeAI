using Microsoft.OpenApi.Models;
using ResumeAI.RagwithGraph.Api.Endpoints;
using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Api.Repositories.Implementation;
using ResumeAI.RagwithGraph.Api.Repository.Declaration;
using ResumeAI.RagwithGraph.Api.Services.Declaration;
using ResumeAI.RagwithGraph.Api.Services.Implementation;
using ResumeAI.RagwithGraph.Api.Swagger;
using ResumeAI.RagwithGraph.Common;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// Configure Kestrel to handle requests up to 500 MB
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000; // 500 MB
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ResumeRag with Graph Service",
        Description = "Backend API for ResumeRag Service"
    });

    options.OperationFilter<AddHeaderParameter>();
});

builder.Services.Configure<Neo4jOptions>(builder.Configuration.GetSection("Neo4j"));

builder.Services.AddScoped<INeo4jGraphRepo, Neo4jGraphRepo>();
builder.Services.AddScoped<INeo4jGraphService, Neo4jGraphService>();
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddScoped<ILLMAdapterService, LLMAdapterService>();
builder.Services.AddScoped<IAISearchService, AISearchService>();
builder.Services.AddScoped<IResumeLLMOrchestrationService, ResumeLLMOrchestrationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapFileEndpoints();

app.Run();
