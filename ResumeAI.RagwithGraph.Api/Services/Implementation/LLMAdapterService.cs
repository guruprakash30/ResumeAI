using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Api.Services.Declaration;
using ResumeAI.RagwithGraph.Api.Utility;
using System.Text.Json;

namespace ResumeAI.RagwithGraph.Api.Services.Implementation
{
    public class LLMAdapterService : ILLMAdapterService
    {
        private readonly ChatClient _chatClient;

        public LLMAdapterService()
        {
            var endpoint = new Uri("https://openai-llm-for-hr-resume-rag.openai.azure.com/");
            var deploymentName = "gpt-4.1-mini";

            var azureClient = new AzureOpenAIClient(endpoint, new DefaultAzureCredential());

            _chatClient = azureClient.GetChatClient(deploymentName);
        }

        /// <summary>
        /// Converts a job description or general prompt into concise, vector-search-ready queries.
        /// </summary>
        public async Task<List<string>> GenerateSearchQueriesAsync(string prompt)
        {
            try
            {
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(LLMSystemMessages.SearchQueryGenerator),
                    new UserChatMessage(prompt)
                };

                var options = new ChatCompletionOptions()
                {
                    MaxOutputTokenCount = 1024,
                    Temperature = 0.7f,
                    TopP = 0.95f,
                };

                var response = await _chatClient.CompleteChatAsync(messages, options);

                string output = response.Value.Content[0].Text;

                var subQueries = JsonSerializer.Deserialize<List<string>>(output);

                return subQueries ?? new List<string>();
            }
            catch (Exception ex)
            {
                return new List<string> { ex.Message };
            }
        }

        public async Task<JobNormalizationResult?> NormalizeJobDescriptionAsync(string jobDescription)
        {
            try
            {
                var messages = new List<ChatMessage>
                               {
                                   new SystemChatMessage(LLMSystemMessages.JobDescriptionNormalizationContract),
                                   new UserChatMessage(jobDescription)
                               };

                var options = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 3000,   // summaries can be long
                    Temperature = 0.0f,           // IMPORTANT: determinism
                    TopP = 1.0f
                };

                var response = await _chatClient.CompleteChatAsync(messages, options);

                var json = response.Value.Content[0].Text;

                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                return JsonSerializer.Deserialize<JobNormalizationResult>(json, serializerOptions);
            }
            catch (JsonException ex)
            {
                // LLM returned malformed JSON (rare but possible)
                throw new InvalidOperationException("Failed to deserialize job normalization response.", ex);
            }
        }

        public async Task<ResumeGraphNormalizationResult?> NormalizeResumeAsync(string resumeText)
        {
            var messages = new List<ChatMessage>
                           {
                               new SystemChatMessage(LLMSystemMessages.ResumeSemanticGraphNormalizationSystemPrompt),
                               new UserChatMessage(resumeText)
                           };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 6000,
                Temperature = 0.0f,
                TopP = 1.0f
            };

            var response = await _chatClient.CompleteChatAsync(messages, options);

            var json = response.Value.Content[0].Text;

            return JsonSerializer.Deserialize<ResumeGraphNormalizationResult>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });
        }

        public async Task<List<string>> GenerateHrCypherQueriesAsync(string hrQuery)
        {
            try
            {
                var messages = new List<ChatMessage>
                                   {
                                       new SystemChatMessage(LLMSystemMessages.Neo4jGraphdbQueryGenerator),
                                       new UserChatMessage(hrQuery)
                                   };

                var options = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 2000,
                    Temperature = 0.0f,   // deterministic
                    TopP = 1.0f
                };

                var response = await _chatClient.CompleteChatAsync(messages, options);

                var output = response.Value.Content[0].Text;

                return JsonSerializer.Deserialize<List<string>>(output)
                       ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }


        public async Task<string> ReasonHrQueryWithCandidatesAsync( string hrQuery, RankedCandidatesResponse rankedCandidates, object answerToHrQuery, string aiSearchChunks)
        {
            // Serialize candidates and graph answer
            var rankedCandidatesJson = JsonSerializer.Serialize(
                rankedCandidates.Candidates,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            var answerToHrQueryJson = JsonSerializer.Serialize(
                answerToHrQuery,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Build system message by replacing placeholders
            var systemMessage = LLMSystemMessages.HrQueryReasoningWithChunks
                .Replace("{hrQuery}", hrQuery)
                .Replace("{rankedCandidates}", rankedCandidatesJson)
                .Replace("{answerToHrQuery}", answerToHrQueryJson)
                .Replace("{aiSearchChunks}", aiSearchChunks);

            // Prepare messages
            var messages = new List<ChatMessage>
                           {
                               new SystemChatMessage(systemMessage),
                               new UserChatMessage(hrQuery) // actual user query
                           };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 6000,  // adjust as needed
                Temperature = 0.0f,          // deterministic reasoning
                TopP = 1.0f
            };

            // Send to Azure OpenAI
            var response = await _chatClient.CompleteChatAsync(messages, options);

            // Return text output
            return response.Value.Content[0].Text;
        }
    }
}
