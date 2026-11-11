using System.Threading.Channels;

namespace Klasly.API.Push;

/// <summary>
/// Notification ids waiting to be sent (spec answer 3). The grade save enqueues after its commit; <see cref="PushSender"/>
/// reads them in the background, so the teacher never waits for Firebase. Singleton.
/// </summary>
public class PushQueue
{
    private readonly Channel<int> channel = Channel.CreateUnbounded<int>();

    public void Enqueue(int notificationId) => channel.Writer.TryWrite(notificationId);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
