using Microsoft.Extensions.Logging;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeAI.RagwithGraph.Common
{
    public static class CommonLogger
    {
        public static async Task LogExceptionAsync<T>(ILogger<T> logger, Exception ex, string? message)
        {
            logger.LogError(exception: ex, message);
        }

        public static async Task LogExceptionAsync(ILogger logger, Exception ex, string? message)
        {
            logger.LogError(exception: ex, message);
        }

        public static async Task LogSummaryAsync(ILogger logger, string operation, IResultSummary summary)
        {
            logger.LogInformation(
                "{Operation} | NodesCreated={NodesCreated}, RelationshipsCreated={RelationshipsCreated}, PropertiesSet={PropertiesSet}, ExecutionMs={ExecutionMs}",
                operation,
                summary.Counters.NodesCreated,
                summary.Counters.RelationshipsCreated,
                summary.Counters.PropertiesSet,
                summary.ResultAvailableAfter.TotalMilliseconds);
        }
    }
}
