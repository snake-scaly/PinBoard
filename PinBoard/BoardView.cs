using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using DynamicData;
using Eto.Drawing;
using Eto.Forms;
using ReactiveUI;
using Splat;

namespace PinBoard;

public class BoardView : Panel, INotifyPropertyChanged, IEnableLogger
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

        ViewModel
            .WhenAnyValue(x => x.BoardViewTransform)
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
        var screenSize = Math.Min(Width / 2f, Height / 2f);
        var x = Random.Shared.NextSingle() * (Width - screenSize) + screenSize / 2;
        var y = Random.Shared.NextSingle() * (Height - screenSize) + screenSize / 2;
        var boardLocation = ViewModel.ViewBoardTransform.TransformPoint(new PointF(x, y));
        var boardSize = ViewModel.ViewBoardTransform.TransformSize(new SizeF(screenSize, 0)).Width;

        _board.Pins.Add(new Pin(url, boardLocation, boardSize));
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

    private void LogDrag(string method, DragEventArgs e)
    {
        this.Log().Debug(
            "{method}: Effects={effects}, ContainsUris={ContainsUris}, Uris={Uris}",
            method, e.Effects, e.Data.ContainsUris, e.Data.ContainsUris ? e.Data.Uris : null);
    }

    protected override void OnDragEnter(DragEventArgs e)
    {
        base.OnDragEnter(e);
        e.Effects = e.AllowedEffects & DragEffects.Copy | DragEffects.Link;
        LogDrag(nameof(OnDragEnter), e);
    }

    private DateTimeOffset _lastDragLog;
    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        if (DateTimeOffset.Now - _lastDragLog >= TimeSpan.FromSeconds(1))
        {
            LogDrag(nameof(OnDragOver), e);
            _lastDragLog = DateTimeOffset.Now;
        }
    }

    protected override void OnDragDrop(DragEventArgs e)
    {
        base.OnDragDrop(e);
        LogDrag(nameof(OnDragDrop), e);

        var boardLocation = ViewModel.ViewBoardTransform.TransformPoint(e.Location);
        var boardSize = ViewModel.ViewBoardTransform.TransformSize(new SizeF(Width / 2f, Height / 2f));

        if (e.Data.ContainsImage)
        {
            var boardScale = boardSize / e.Data.Image.Size;
            _board.Pins.Add(new Pin(e.Data.Image, boardLocation, Math.Min(boardScale.Width, boardScale.Height)));
        }
        else if (e.Data.ContainsUris)
        {
            if (e.Data.Uris.Length == 1)
                _board.Pins.Add(new Pin(e.Data.Uris[0], boardLocation, Math.Min(boardSize.Width, boardSize.Height)));
            else
                foreach (var uri in e.Data.Uris)
                    Add(uri);
        }
        else if (e.Data.ContainsText)
        {
            _board.Pins.Add(new Pin(new Uri(e.Data.Text), boardLocation, Math.Min(boardSize.Width, boardSize.Height)));
        }
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
