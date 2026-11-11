using Hangfire;
using Klasly.Worker.Services;

namespace Klasly.Worker.Jobs;

/// <summary>
/// The Worker's one recurring job. It only calls the services, in order; the rules live in the services.
/// Hangfire passes the server's shutdown token in place of <c>CancellationToken.None</c>.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 600)] // two ticks never overlap, even across Worker processes
[AutomaticRetry(Attempts = 0)] // a failed tick waits for the next one; the email outbox (Milestone 12) does its own retries
public class WorkerTickJob(AttendanceFinalizer finalizer, ILogger<WorkerTickJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var (closed, cancelled) = await finalizer.CloseEndedSessionsAsync(now, ct);
        var finalized = await finalizer.MarkAbsentAndFinalizeAsync(now, ct);
        logger.LogInformation(
            "Tick {UtcNow:u}: closed {Closed}, cancelled {Cancelled}, finalized {Finalized} sessions", now, closed, cancelled, finalized);

        // Milestone 11: risk flags. Milestone 12: alerts + emails.
    }
}
