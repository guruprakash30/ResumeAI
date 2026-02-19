using Microsoft.Extensions.Options;
using ResumeAI.RagwithGraph.Api.Model;
using ResumeAI.RagwithGraph.Api.Repository.Declaration;
using Neo4j.Driver;
using ResumeAI.RagwithGraph.Common.Model;
using ResumeAI.RagwithGraph.Common;

namespace ResumeAI.RagwithGraph.Api.Repositories.Implementation
{
    public sealed class Neo4jGraphRepo : INeo4jGraphRepo, IAsyncDisposable
    {
        private readonly IDriver _driver;
        private readonly ILogger<Neo4jGraphRepo> _logger;

        public Neo4jGraphRepo(IOptions<Neo4jOptions> options, ILogger<Neo4jGraphRepo> logger)
        {
            var cfg = options.Value;

            _driver = GraphDatabase.Driver(cfg.ConnectionUrl, AuthTokens.Basic(cfg.Username, cfg.Password));
            _logger = logger;
        }

        public async Task<OperationResult> PersistJobAsync(Guid jobId,JobNode job,IEnumerable<SkillRequirement> skills)
        {
            IAsyncSession? session = null;
            OperationResult rRes = new();
            try
            {
                session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

                await session.ExecuteWriteAsync(async tx =>
                {
                    var jobCursor = await tx.RunAsync(
                                    """
                                        MERGE (j:Job { job_id: $jobId })
                                        SET
                                            j.title = $title,
                                            j.location = $location,
                                            j.min_experience = $minExperience,
                                            j.job_description = $jobDescription,
                                            j.posted_at = $postedAt
                                     """,
                                    new
                                    {
                                        jobId = jobId.ToString(),
                                        title = job.Title,
                                        location = job.Location,
                                        minExperience = job.Min_Experience,
                                        jobDescription = job.Job_Description,
                                        postedAt = job.Posted_At
                                    });

                    var jobSummary = await jobCursor.ConsumeAsync();

                    await CommonLogger.LogSummaryAsync(_logger, "Job upsert", jobSummary);

                    foreach (var skill in skills)
                    {
                        var skillCursor =await tx.RunAsync(
                                    """
                                          MATCH (j:Job { job_id: $jobId })
                                          MERGE (s:Skill { name: $name })
                                          SET s.category = $category
                                          MERGE (j)-[r:REQUIRES]->(s)
                                          SET
                                              r.weight = $weight,
                                              r.min_years = $minYears
                                     """,
                                    new
                                    {
                                        jobId = jobId.ToString(),
                                        name = skill.Name,
                                        category = skill.Category,
                                        weight = skill.Weight,
                                        minYears = skill.Min_Years
                                    });

                        var skillSummary = await skillCursor.ConsumeAsync();
                        await CommonLogger.LogSummaryAsync(_logger, $"Skill '{skill.Name}'", skillSummary);
                    }


                });

                return rRes.Success();
            }
            catch(Exception ex)
            {
                await CommonLogger.LogExceptionAsync(_logger, ex, null);
                return rRes.Failure();
            }
            finally
            {
                if (session is not null)
                    await session.CloseAsync();
            }
        }

        public async Task<OperationResult<string?>> GetJobDescriptionSummaryByJobIdAsync(Guid jobId)
        {
            IAsyncSession? session = null;
            OperationResult<string> rRes = new();
            try
            {
                const string query = """
                                 MATCH (j:Job { job_id: $jobId })
                                 RETURN j.job_description AS jobDescription
                                 """;

                session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));

                var cursor = await session.RunAsync(query, new
                {
                    jobId = jobId.ToString()
                });

                var records = await cursor.ToListAsync();

                var summary = await cursor.ConsumeAsync();
                await CommonLogger.LogSummaryAsync(_logger, "jobdescriptionsummary query by id", summary);
                return rRes.Success(records.FirstOrDefault()?["jobDescription"]?.As<string>());
            }
            catch (Exception ex)
            {
                await CommonLogger.LogExceptionAsync(_logger, ex, null);
                return rRes.Failure();
            }
            finally
            {
                if (session is not null)
                    await session.CloseAsync();
            }
        }

        public async Task<OperationResult> PersistResumeGraphAsync(Guid candidateId, ResumeGraphNormalizationResult resume)
        {
            var result = new OperationResult();

            IAsyncSession? session = null;

            try
            {
                session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Write));

                await session.ExecuteWriteAsync(async tx =>
                {
                    // ---------- Candidate Node ----------
                    await tx.RunAsync(@"
                    MERGE (c:Candidate { candidate_id: $candidateId })
                    SET c.full_name = $fullName,
                        c.email = $email,
                        c.total_experience_years = $totalExperience,
                        c.last_updated = $lastUpdated
                ", new
                    {
                        candidateId = candidateId.ToString(),
                        fullName = resume.Candidate.Full_Name,
                        email = resume.Candidate.Email,
                        totalExperience = resume.Candidate.Total_Experience_Years,
                        lastUpdated = resume.Candidate.Last_Updated
                    });

                    // ---------- Location ----------
                    if (resume.Location != null)
                    {
                        await tx.RunAsync(@"
                        MERGE (l:Location { city: $city, state: $state, country: $country })
                        WITH l
                        MATCH (c:Candidate { candidate_id: $candidateId })
                        MERGE (c)-[:LOCATED_IN]->(l)
                    ", new
                        {
                            candidateId = candidateId.ToString(),
                            city = resume.Location.City,
                            state = resume.Location.State,
                            country = resume.Location.Country
                        });
                    }

                    // ---------- Seniority ----------
                    if (resume.Seniority != null)
                    {
                        await tx.RunAsync(@"
                        MERGE (s:SeniorityLevel { name: $level })
                        WITH s
                        MATCH (c:Candidate { candidate_id: $candidateId })
                        MERGE (c)-[:HAS_SENIORITY]->(s)
                    ", new
                        {
                            candidateId = candidateId.ToString(),
                            level = resume.Seniority.Name
                        });
                    }

                    // ---------- Skills ----------
                    if (resume.Skills != null && resume.Skills.Count > 0)
                    {
                        foreach (var skill in resume.Skills)
                        {
                            await tx.RunAsync(@"
                            MERGE (s:Skill { name: $name })
                            SET s.category = $category
                            WITH s
                            MATCH (c:Candidate { candidate_id: $candidateId })
                            MERGE (c)-[r:HAS_SKILL]->(s)
                            SET r.years = $years,
                                r.proficiency = $proficiency,
                                r.last_used_year = $lastUsed
                        ", new
                            {
                                candidateId = candidateId.ToString(),
                                name = skill.Name,
                                category = skill.Category,
                                years = skill.Years,
                                proficiency = skill.Proficiency,
                                lastUsed = skill.Last_Used_Year
                            });
                        }
                    }

                    // ---------- Work Experience ----------
                    if (resume.Work_Experience != null && resume.Work_Experience.Count > 0)
                    {
                        foreach (var work in resume.Work_Experience)
                        {
                            await tx.RunAsync(@"
                            MERGE (r:Role { role_id: $roleId })
                            SET r.title = $title,
                                r.level = $level
                            MERGE (co:Company { name: $companyName })
                            SET co.industry = $industry
                            MERGE (tp:TimePeriod { from_date: $from, to_date: $to })
                            WITH r, co, tp
                            MATCH (c:Candidate { candidate_id: $candidateId })
                            MERGE (c)-[:WORKED_AS]->(r)-[:AT_COMPANY]->(co)-[:During]->(tp)
                        ", new
                            {
                                candidateId = candidateId.ToString(),
                                roleId = work.Role.Role_Id?.ToString(),
                                title = work.Role.Title,
                                level = work.Role.Level,
                                companyName = work.Company.Name,
                                industry = work.Company.Industry,
                                from = work.Time_Period.From_Date,
                                to = work.Time_Period.To_Date
                            });
                        }
                    }

                    // ---------- Projects ----------
                    if (resume.Projects != null && resume.Projects.Count > 0)
                    {
                        foreach (var project in resume.Projects)
                        {
                            await tx.RunAsync(@"
                            MERGE (p:Project { project_id: $projectId })
                            SET p.name = $name,
                                p.domain = $domain,
                                p.complexity = $complexity,
                                p.scale = $scale
                            WITH p
                            MATCH (c:Candidate { candidate_id: $candidateId })
                            MERGE (c)-[:WORKED_ON]->(p)
                        ", new
                            {
                                candidateId = candidateId.ToString(),
                                projectId = project.Project_Id?.ToString(),
                                name = project.Name,
                                domain = project.Domain,
                                complexity = project.Complexity,
                                scale = project.Scale
                            });
                        }
                    }

                    // ---------- Education ----------
                    if (resume.Education != null && resume.Education.Count > 0)
                    {
                        foreach (var edu in resume.Education)
                        {
                            await tx.RunAsync(@"
                            MERGE (d:Degree { name: $degreeName })
                            MERGE (f:FieldOfStudy { name: $fieldName })
                            MERGE (i:Institution { name: $institutionName })
                            MERGE (tp:TimePeriod { from_date: $from, to_date: $to })
                            WITH d,f,i,tp
                            MATCH (c:Candidate { candidate_id: $candidateId })
                            MERGE (c)-[:EARNED_DEGREE]->(d)-[:IN_FIELD]->(f)-[:AT_INSTITUTION]->(i)-[:DURING]->(tp)
                        ", new
                            {
                                candidateId = candidateId.ToString(),
                                degreeName = edu.Degree.Name,
                                fieldName = edu.Field_Of_Study.Name,
                                institutionName = edu.Institution.Name,
                                from = edu.Time_Period.From_Date,
                                to = edu.Time_Period.To_Date
                            });
                        }
                    }
                });

                return result.Success();
            }
            catch (Exception ex)
            {
                await CommonLogger.LogExceptionAsync(_logger, ex, candidateId.ToString());
                return result.Failure();
            }
            finally
            {
                if (session is not null)
                    await session.CloseAsync();
            }
        }

        public async Task<OperationResult<List<RankedCandidateResult>>> GetRankedCandidatesByJobIdAsync(Guid jobId)
        {
            IAsyncSession? session = null;
            OperationResult<List<RankedCandidateResult>> rRes = new();

            try
            {
                session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));

                var currentYear = DateTime.UtcNow.Year;

                const string query = """
                                      MATCH (j:Job { job_id: $jobId })
                                      WITH j, j.min_experience AS minExp
                                      
                                      // Filter candidates by total experience
                                      MATCH (c:Candidate)
                                      WHERE c.total_experience_years >= minExp
                                      
                                      // Match only required skills
                                      MATCH (j)-[r:REQUIRES]->(s:Skill)
                                      MATCH (c)-[cs:HAS_SKILL]->(s)
                                      
                                      WITH 
                                          c,
                                          r,
                                          cs,
                                          CASE 
                                              WHEN cs.last_used_year IS NULL 
                                              THEN 1.0
                                              ELSE (1.0 / (1 + ($currentYear - cs.last_used_year)))
                                          END AS recencyFactor
                                      
                                      WITH 
                                          c,
                                          SUM(
                                              r.weight *
                                              COALESCE(cs.years, 0) *
                                              recencyFactor
                                          ) AS totalScore
                                      
                                      RETURN 
                                          c.candidate_id AS candidateId,
                                          c.full_name AS fullName,
                                          c.total_experience_years AS totalExperience,
                                          totalScore AS score
                                      ORDER BY score DESC
                                      """;

                var cursor = await session.RunAsync(query, new
                {
                    jobId = jobId.ToString(),
                    currentYear
                });

                var records = await cursor.ToListAsync();

                var results = records.Select(r => new RankedCandidateResult
                {
                    CandidateId = r["candidateId"].As<string>(),
                    FullName = r["fullName"].As<string>(),
                    TotalExperience = r["totalExperience"].As<double>(),
                    Score = r["score"].As<double>()
                }).ToList();

                var summary = await cursor.ConsumeAsync();
                await CommonLogger.LogSummaryAsync(_logger, "Ranked candidates query", summary);

                return rRes.Success(results);
            }
            catch (Exception ex)
            {
                await CommonLogger.LogExceptionAsync(_logger, ex, jobId.ToString());
                return rRes.Failure();
            }
            finally
            {
                if (session is not null)
                    await session.CloseAsync();
            }
        }

        public async Task<List<string>> ExecuteHrQueryAsync(string cypherQuery, List<string> rankedCandidateIds)
        {
            IAsyncSession? session = null;

            try
            {
                session = _driver.AsyncSession(o =>
                    o.WithDefaultAccessMode(AccessMode.Read));

                var cursor = await session.RunAsync(cypherQuery, new
                {
                    ranked_candidate_ids = rankedCandidateIds
                });

                var records = await cursor.ToListAsync();

                return records
                    .Select(r => r["candidate_id"].As<string>())
                    .ToList();
            }
            finally
            {
                if (session != null)
                    await session.CloseAsync();
            }
        }

        public async Task<IReadOnlyList<IRecord>> ExecuteHrQueryAsync(string cypherQuery, IDictionary<string, object> parameters)
        {
            IAsyncSession? session = null;
            try
            {
                session = _driver.AsyncSession(o => o.WithDefaultAccessMode(AccessMode.Read));

                var cursor = await session.RunAsync(cypherQuery, parameters);

                var records = await cursor.ToListAsync();

                var summary = await cursor.ConsumeAsync();
                _logger.LogInformation("HR query executed: {Summary}", summary);

                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing HR query.");
                return Array.Empty<IRecord>();
            }
            finally
            {
                if (session is not null)
                    await session.CloseAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogWarning("Neo4j graph database connection is about to close");
            await _driver.DisposeAsync();
        }
    }

}
