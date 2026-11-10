using Hangfire;

namespace Klasly.Worker.Jobs;

/// <summary>
/// The Worker's one recurring job. It only calls the services, in order; the rules live in the services.
/// Hangfire passes the server's shutdown token in place of <c>CancellationToken.None</c>.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 600)] // two ticks never overlap, even across Worker processes
[AutomaticRetry(Attempts = 0)] // a failed tick waits for the next one; the email outbox (Milestone 12) does its own retries
public class WorkerTickJob(ILogger<WorkerTickJob> logger)
{
    public Task RunAsync(CancellationToken ct)
    {
        // Milestone 9 Tasks 3–4: close ended sessions, mark Absent. Milestone 11: risk flags. Milestone 12: alerts + emails.
        logger.LogInformation("Worker tick at {UtcNow:u}", DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
