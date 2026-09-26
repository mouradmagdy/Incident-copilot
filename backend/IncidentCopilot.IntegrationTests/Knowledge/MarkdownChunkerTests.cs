using IncidentCopilot.Application.Knowledge;

namespace IncidentCopilot.IntegrationTests.Knowledge;

public sealed class MarkdownChunkerTests
{
    [Fact]
    public void Chunk_ShouldSplitLongMarkdown()
    {
        var markdown = """
            # Database Connection Exhaustion

            Database connection exhaustion occurs when an application consumes most or all connections available in its database connection pool. New operations must then wait for a connection and eventually time out. In this system, the affected service is typically `OrderService`, and the configured pool limit should be treated as 100 connections unless the deployed configuration says otherwise.

            ## Common symptoms

            - Active database connections remain near the pool limit; the simulator reports 95–100.
            - API average and p95 latency rise significantly.
            - Database operations time out and the application error rate increases.
            - Application CPU may remain normal or only moderately elevated.
            - Queue depth may rise as requests wait for database work.
            - Logs contain messages such as `Timeout while waiting for database connection`, `Database connection pool exhausted`, or `Database operation exceeded timeout`.

            ## Investigation

            1. Confirm which service and database are affected and when the increase began.
            2. Compare active database connections with the configured maximum pool size.
            3. Check database sessions for long-running or blocked queries and open transactions.
            4. Correlate the onset with traffic, deployments, configuration changes, and scheduled jobs.
            5. Inspect application code and traces for connections, commands, readers, or transactions that are not disposed promptly.
            6. Check database CPU, locks, storage latency, and query duration. A slow database can hold otherwise healthy connections for too long.
            7. Determine whether exhaustion is isolated to one application instance or shared by all instances.

            ## Mitigation

            - Stop or roll back the workload, deployment, or job that introduced the pressure.
            - Cancel clearly runaway queries or transactions after confirming the business impact.
            - Restart a leaking application instance only as a temporary recovery measure; preserve logs and traces first when possible.
            - Optimize slow queries, reduce transaction duration, and fix connection disposal.
            - Carefully increase the pool size only after identifying the cause and confirming that the database can safely accept the additional connections.

            ## Verification

            Confirm that active connections remain comfortably below the pool limit, timeouts stop, latency and error rate return to baseline, and the queue drains. Continue monitoring through at least one normal traffic cycle. Record the root cause, temporary actions, and permanent corrective work in the incident timeline.

            """;

        var chunker = new MarkdownChunker();

        var chunks = chunker.Chunk(markdown);

        Assert.NotEmpty(chunks);

        foreach (var chunk in chunks)
        {
            Console.WriteLine("--------");
            Console.WriteLine(chunk.content);
        }
    }
}
