using Klasly.Core.Interfaces;

namespace Klasly.API.Push;

/// <summary>
/// Background service in the Web API that sends grade pushes (guide §27.8: the API sends them, async). On start it picks up
/// Pending push rows a restart interrupted, then sends each id from <see cref="PushQueue"/> in its own DI scope.
/// </summary>
public class PushSender(PushQueue queue, IServiceScopeFactory scopes, ILogger<PushSender> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            foreach (var id in await scope.ServiceProvider.GetRequiredService<INotificationRepository>().ListPendingPushIdsAsync(stoppingToken))
                queue.Enqueue(id);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Could not load Pending pushes on startup"); // new pushes still go out
        }

        await foreach (var id in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<PushNotificationSender>().SendAsync(id, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Push {NotificationId} could not be processed", id); // keep going with the next one
            }
        }
    }
}
