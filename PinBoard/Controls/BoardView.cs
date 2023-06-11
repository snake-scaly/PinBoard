using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Services;
using ReactiveUI;
using Splat;

namespace PinBoard.Controls;

public class BoardView : Panel, INotifyPropertyChanged, IEnableLogger
{
    private readonly Board _board;
    private readonly Drawable _canvas;
    private readonly CompositeDisposable _disposables = new();

    private IEditMode _editMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public BoardView(Board board, IEditModeFactory editModeFactory)
    {
        _board = board;
        _canvas = new Drawable().DisposeWith(_disposables);

        ViewModel
            .WhenAnyValue(x => x.BoardViewTransform)
            .Subscribe(_ => Invalidate())
            .DisposeWith(_disposables);

        Observable
            .FromEventPattern<PaintEventArgs>(_canvas, nameof(_canvas.Paint))
            .Subscribe(x => EditMode.OnPaint(x.EventArgs))
            .DisposeWith(_disposables);

        _editMode = editModeFactory.CreateBoardEditMode(_board, ViewModel);
        _editMode.Attach(this);

        this.WhenAnyValue(x => x.EditMode.ContextMenu).Subscribe(x => ContextMenu = x).DisposeWith(_disposables);
        this.WhenAnyObservable(x => x.EditMode.Invalidated).Subscribe(_ => Invalidate()).DisposeWith(_disposables);
        this.WhenAnyObservable(x => x.EditMode.Cursor).Subscribe(x => Cursor = x).DisposeWith(_disposables);
        this.WhenAnyObservable(x => x.EditMode.ShowContextMenu).Subscribe(x => ContextMenu.Show(PointToScreen(x))).DisposeWith(_disposables);
        this.WhenAnyObservable(x => x.EditMode.NewEditMode).Subscribe(x => EditMode = x).DisposeWith(_disposables);

        this.WhenAnyValue(x => x.EditMode)
            .Buffer(2, 1)
            .Subscribe(
                x =>
                {
                    x[0].Detach(this);
                    x[1].Attach(this);
                    Invalidate();
                })
            .DisposeWith(_disposables);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Content = _canvas;
        AllowDrop = true;
        new PanZoomController(ViewModel, this).DisposeWith(_disposables);
    }

    public PanZoomModel ViewModel { get; } = new();

    private IEditMode EditMode
    {
        get => _editMode;
        set => SetField(ref _editMode, value);
    }

    public void Add(Uri url)
    {
        _board.Add(url, ViewModel.ViewBoardTransform.TransformRectangle(new RectangleF(default, Size)));
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _disposables.Dispose();
        base.Dispose(disposing);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
