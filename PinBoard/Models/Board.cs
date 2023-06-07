using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard.Models;

public sealed class Board : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public Board()
    {
        Pins.DisposeWith(_disposables);

        var pinChanges = Pins.Connect()
            .Publish();
        pinChanges.Connect();

        pinChanges.MergeMany(x => x.Updates)
            .Merge(pinChanges.Select(_ => Unit.Default))
            .Subscribe(_ => Modified = true)
            .DisposeWith(_disposables);
    }

    public SourceList<Pin> Pins { get; } = new();

    [Reactive]
    public string? Filename { get; set; }

    [Reactive]
    public bool Modified { get; set; }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
