namespace PinBoard.Util;

public sealed class CallbackDisposable : IDisposable
{
    private readonly Action _callback;
    private bool _disposed;

    public CallbackDisposable(Action callback)
    {
        _callback = callback;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _callback();
    }
}
