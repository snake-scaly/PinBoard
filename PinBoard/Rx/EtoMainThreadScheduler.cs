using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Eto.Forms;

namespace PinBoard.Rx;

public class EtoMainThreadScheduler : LocalScheduler
{
    private readonly EventLoopScheduler _delayedScheduler = new();

    public override IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        if (dueTime <= TimeSpan.Zero)
            return QueueNow(state, action);
        return _delayedScheduler.Schedule(Unit.Default, dueTime, (_, _) => QueueNow(state, action));
    }

    private IDisposable QueueNow<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        var d = new SingleAssignmentDisposable();
        Application.Instance.AsyncInvoke(() =>
        {
            if (!d.IsDisposed)
                d.Disposable = action(this, state);
        });
        return d;
    }
}
