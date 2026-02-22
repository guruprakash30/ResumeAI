namespace ResumeAI.RagwithGraph.Api.Model
{
    public sealed class RankedCandidateResult
    {
        public string CandidateId { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public double TotalExperience { get; set; }
        public string ResumeId { get; set; } = default!;
        public double Score { get; set; }
    }

    public sealed class RankedCandidatesResponse
    {
        public Guid JobId { get; set; }
        public int TotalCandidates { get; set; }
        public List<RankedCandidateResult> Candidates { get; set; } = new();
        public List<string> RankedCandidateIds { get; set; } = new();
    }
}
