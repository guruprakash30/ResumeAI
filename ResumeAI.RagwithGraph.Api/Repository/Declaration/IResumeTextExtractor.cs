namespace ResumeAI.RagwithGraph.Api.Repository.Declaration
{
    public interface IResumeTextExtractor
    {
        Task<string> ExtractTextAsync(Stream fileStream, string fileName);
    }
}
