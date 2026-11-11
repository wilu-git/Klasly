using Hangfire;
using Klasly.Worker.Services;

namespace Klasly.Worker.Jobs;

/// <summary>
/// The Worker's one recurring job. It only calls the services, in order; the rules live in the services.
/// Hangfire passes the server's shutdown token in place of <c>CancellationToken.None</c>.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 600)] // two ticks never overlap, even across Worker processes
[AutomaticRetry(Attempts = 0)] // a failed tick waits for the next one; the email outbox does its own retries
public class WorkerTickJob(
    AttendanceFinalizer finalizer, RiskCalculator risk, AlertQueuer alerts, EmailSender email, ILogger<WorkerTickJob> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var (closed, cancelled) = await finalizer.CloseEndedSessionsAsync(now, ct);
        var finalized = await finalizer.MarkAbsentAndFinalizeAsync(now, ct);
        logger.LogInformation(
            "Tick {UtcNow:u}: closed {Closed}, cancelled {Cancelled}, finalized {Finalized} sessions", now, closed, cancelled, finalized);

        // After finalizing, so this tick's new Absents already count.
        var (enrollments, snapshots, atRisk) = await risk.RecomputeAsync(now, ct);
        logger.LogInformation(
            "Tick {UtcNow:u}: risk for {Enrollments} enrollments, {Snapshots} new snapshots, {AtRisk} at-risk", now, enrollments, snapshots, atRisk);

        // After risk, so this tick's new at-risk snapshots are alerted; then send, so they can go out in the same tick.
        var (absence, atRiskAlerts) = await alerts.QueueAsync(now, ct);
        logger.LogInformation("Tick {UtcNow:u}: queued {Absence} absence and {AtRisk} at-risk emails", now, absence, atRiskAlerts);

        var (sent, retrying, failed) = await email.SendDueAsync(now, ct);
        logger.LogInformation("Tick {UtcNow:u}: emails sent {Sent}, retrying {Retrying}, failed {Failed}", now, sent, retrying, failed);
    }
}
