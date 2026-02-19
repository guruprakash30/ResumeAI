namespace ResumeAI.RagwithGraph.Api.Model
{
    public sealed class ResumeGraphNormalizationResult
    {
        public CandidateNode Candidate { get; set; } = new();
        public LocationNode Location { get; set; } = new();
        public SeniorityNode Seniority { get; set; } = new();
        public List<SkillNode> Skills { get; set; } = [];
        public List<WorkExperienceNode> Work_Experience { get; set; } = [];
        public List<ProjectNode> Projects { get; set; } = [];
        public List<EducationNode> Education { get; set; } = [];
    }

    public sealed class CandidateNode
    {
        public Guid? Candidate_Id { get; set; }
        public string? Full_Name { get; set; }
        public string? Email { get; set; }
        public float? Total_Experience_Years { get; set; }
        public DateTime? Last_Updated { get; set; }
    }

    public sealed class LocationNode
    {
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
    }

    public sealed class SeniorityNode
    {
        public string? Name { get; set; }
    }

    public sealed class SkillNode
    {
        public string? Name { get; set; }
        public string? Category { get; set; }
        public float? Years { get; set; }
        public string? Proficiency { get; set; }
        public int? Last_Used_Year { get; set; }
    }


    public sealed class WorkExperienceNode
    {
        public RoleNode Role { get; set; } = new();
        public CompanyNode Company { get; set; } = new();
        public TimePeriodNode Time_Period { get; set; } = new();
    }

    public sealed class RoleNode
    {
        public Guid? Role_Id { get; set; }
        public string? Title { get; set; }
        public string? Level { get; set; }
    }

    public sealed class CompanyNode
    {
        public string? Name { get; set; }
        public string? Industry { get; set; }
    }

    public sealed class TimePeriodNode
    {
        public DateTime? From_Date { get; set; }
        public DateTime? To_Date { get; set; }
    }

    public sealed class ProjectNode
    {
        public Guid? Project_Id { get; set; }
        public string? Name { get; set; }
        public string? Domain { get; set; }
        public string? Complexity { get; set; }
        public string? Scale { get; set; }
    }

    public sealed class EducationNode
    {
        public DegreeNode Degree { get; set; } = new();
        public FieldOfStudyNode Field_Of_Study { get; set; } = new();
        public InstitutionNode Institution { get; set; } = new();
        public TimePeriodNode Time_Period { get; set; } = new();
    }

    public sealed class DegreeNode
    {
        public string? Name { get; set; }
    }

    public sealed class FieldOfStudyNode
    {
        public string? Name { get; set; }
    }

    public sealed class InstitutionNode
    {
        public string? Name { get; set; }
    }

}
