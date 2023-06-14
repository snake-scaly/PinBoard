using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard.Util;

public class ReactiveValue<T> : ReactiveObject
{
    [Reactive]
    public T Value { get; set; }
}
