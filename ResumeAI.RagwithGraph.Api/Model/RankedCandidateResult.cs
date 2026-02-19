namespace ResumeAI.RagwithGraph.Api.Model
{
    public sealed class RankedCandidateResult
    {
        public string CandidateId { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public double TotalExperience { get; set; }
        public double Score { get; set; }
    }
}
