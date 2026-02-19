namespace ResumeAI.RagwithGraph.Api.Model
{
    public class JobNormalizationResult
    {
        public JobNode Job { get; set; } = default!;
        public List<SkillRequirement> Skills { get; set; } = new();
    }

    public sealed class JobNode
    {
        public Guid? Job_Id { get; set; }   // always null from LLM
        public string Title { get; set; } = string.Empty;
        public string? Location { get; set; }
        public float? Min_Experience { get; set; }
        public string Job_Description { get; set; } = string.Empty;
        public DateTime? Posted_At { get; set; }
    }

    public sealed class SkillRequirement
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public float Weight { get; set; }
        public float? Min_Years { get; set; }
    }
}
