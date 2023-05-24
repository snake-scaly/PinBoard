using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;
using Eto.Drawing;
using Eto.Forms;
using ReactiveUI;

namespace PinBoard;

public class BoardView : Panel, INotifyPropertyChanged
{
    private readonly Board _board;
    private readonly Drawable _canvas;
    private readonly CompositeDisposable _disposables = new();

    private IEditMode _editMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public BoardView(Board board, Settings settings)
    {
        _board = board;
        _canvas = new Drawable().DisposeWith(_disposables);
        new PanZoomController(ViewModel, this).DisposeWith(_disposables);

        Content = _canvas;

        ViewModel
            .WhenAnyValue(x => x.Origin, x => x.Scale)
            .Subscribe(_ => Invalidate())
            .DisposeWith(_disposables);

        Observable
            .FromEventPattern<PaintEventArgs>(_canvas, nameof(_canvas.Paint))
            .Subscribe(x => EditMode.OnPaint(x.EventArgs))
            .DisposeWith(_disposables);

        _editMode = new BoardEditMode(_board, ViewModel, settings);

        this.WhenAnyValue(x => x.EditMode.ContextMenu).Subscribe(x => ContextMenu = x).DisposeWith(_disposables);
        this.WhenAnyObservable(x => x.EditMode.Invalidated).Subscribe(_ => Invalidate()).DisposeWith(_disposables);
        this.WhenAnyObservable(x => x.EditMode.Cursor).Subscribe(x => Cursor = x).DisposeWith(_disposables);
        this.WhenAnyObservable(x => x.EditMode.ShowContextMenu).Subscribe(x => ContextMenu.Show(PointToScreen(x))).DisposeWith(_disposables);
        this.WhenAnyObservable(x => x.EditMode.NewEditMode).Subscribe(x => EditMode = x).DisposeWith(_disposables);
        this.WhenAnyValue(x => x.EditMode).Subscribe(_ => Invalidate()).DisposeWith(_disposables);
    }

    public PanZoomModel ViewModel { get; } = new();

    private IEditMode EditMode
    {
        get => _editMode;
        set => SetField(ref _editMode, value);
    }

    public void Add(Image image)
    {
        // Scale to 1/2 of the view and position at random fully visible.
        const float initialSize = .5f;

        var scaleX = Width * initialSize / image.Width;
        var scaleY = Height * initialSize / image.Height;
        var scale = Math.Min(scaleX, scaleY);
        var w = image.Width * scale;
        var h = image.Height * scale;
        var x = Random.Shared.NextSingle() * (Width - w) + w / 2;
        var y = Random.Shared.NextSingle() * (Height - h) + h / 2;
        _board.Pins.Add(new Pin { Image = new PinImage(image, false), Center = new PointF(x, y), Scale = scale });
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _disposables.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Handled)
            return;
        EditMode.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Handled)
            return;
        EditMode.OnMouseUp(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Handled)
            return;
        EditMode.OnMouseMove(e);
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
