namespace Digdir.BDB.Dialogporten.ServiceProvider.Services;

public interface IBackgroundTaskQueue
{
    void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem);
    public Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Queue<Func<CancellationToken, Task>> _workItems = new Queue<Func<CancellationToken, Task>>();
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

    public void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));

        lock (_workItems)
        {
            _workItems.Enqueue(workItem);
        }

        _signal.Release();
    }

    public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        lock (_workItems)
        {
            return _workItems.Dequeue();
        }
    }
}
