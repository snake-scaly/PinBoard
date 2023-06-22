using System.Reactive.Concurrency;

namespace PinBoard.Util;

public sealed class CallbackDisposable : IDisposable
{
    private readonly Action _callback;
    private readonly IScheduler _scheduler;
    private bool _disposed;

    public CallbackDisposable(Action callback, IScheduler? scheduler = null)
    {
        _callback = callback;
        _scheduler = scheduler ?? ImmediateScheduler.Instance;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _scheduler.Schedule(_callback);
    }
}
