using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using PinBoard.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard.ViewModels;

public sealed class MainViewModel : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public MainViewModel(Board board)
    {
        board.WhenAnyValue(x => x.Filename, x => x.Modified)
            .Select(x =>
            {
                var boardName = x.Item1 == null ? "Untitled" : Path.GetFileNameWithoutExtension(x.Item1);
                var modified = x.Item2 ? "*" : string.Empty;
                var appName = Assembly.GetExecutingAssembly().GetName().Name;
                return $"{boardName}{modified} - {appName}";
            })
            .ToPropertyEx(this, x => x.Title)
            .DisposeWith(_disposables);

        board.WhenAnyValue(x => x.Modified)
            .ToPropertyEx(this, x => x.EnableSave)
            .DisposeWith(_disposables);
    }

    [ObservableAsProperty]
    public string Title { get; }

    [ObservableAsProperty]
    public bool EnableSave { get; }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
