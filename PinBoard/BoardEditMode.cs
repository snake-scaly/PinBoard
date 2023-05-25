using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Eto.Drawing;
using Eto.Forms;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard;

public sealed class BoardEditMode : ReactiveObject, IEditMode
{
    private readonly Board _board;
    private readonly PanZoomModel _viewModel;
    private readonly Settings _settings;

    private HitZone _hitZone;
    private bool _drag;
    private PointF _dragOffset;

    private readonly Subject<Unit> _invalidated = new();
    private readonly Subject<Cursor> _cursor = new();
    private readonly Subject<PointF> _showContextMenu = new();
    private readonly Subject<IEditMode> _newEditMode = new();
    
    private readonly CompositeDisposable _disposables = new();

    public BoardEditMode(Board board, PanZoomModel viewModel, Settings settings)
    {
        _board = board;
        _viewModel = viewModel;
        _settings = settings;

        Cursor = _cursor.DistinctUntilChanged();

        var pullForwardCommand = new Command(PullForwardExecute) { MenuText = "Pull Forward" };
        var pushBackCommand = new Command(PushBackExecute) { MenuText = "Push Back" };
        var delPinCommand = new Command(DelPinExecute) { MenuText = "Remove" };
        var cropCommand = new Command(CropExecute) { MenuText = "Crop" };

        ContextMenu = new ContextMenu(pullForwardCommand, pushBackCommand, delPinCommand, cropCommand);

        var pinListChangeSets = _board.Pins.Connect().Publish();
        var pinChanges = pinListChangeSets.MergeMany(x => x.Changed.Select(_ => default(Unit))).Publish();

        pinListChangeSets.Connect().DisposeWith(_disposables);
        pinChanges.Connect().DisposeWith(_disposables);

        var selectionChanges = this.WhenAnyValue(x => x.UnderCursor).Select(_ => default(Unit));
        var listChanges = pinListChangeSets.Select(_ => default(Unit));
        var orderChanges = listChanges.Merge(selectionChanges);
        var anyChanges = orderChanges.Merge(pinChanges);

        orderChanges.Subscribe(_ => pullForwardCommand.Enabled = PullForwardCanExecute());
        orderChanges.Subscribe(_ => pushBackCommand.Enabled = PushBackCanExecute());
        selectionChanges.Subscribe(_ => delPinCommand.Enabled = DelPinCanExecute());
        selectionChanges.Subscribe(_ => cropCommand.Enabled = CropCanExecute());
        anyChanges.Subscribe(_ => _invalidated.OnNext(default));
    }

    public ContextMenu ContextMenu { get; }
    public IObservable<Unit> Invalidated => _invalidated.AsObservable();
    public IObservable<Cursor> Cursor { get; }
    public IObservable<PointF> ShowContextMenu => _showContextMenu.AsObservable();
    public IObservable<IEditMode> NewEditMode => _newEditMode.AsObservable();

    [Reactive]
    private Pin? UnderCursor { get; set; }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void OnMouseDown(MouseEventArgs e)
    {
        if (e.Buttons == MouseButtons.Primary && UnderCursor != null)
        {
            _drag = true;
            _dragOffset = _viewModel.BoardViewTransform.TransformPoint(UnderCursor.Center) - e.Location;
        }
    }

    public void OnMouseUp(MouseEventArgs e)
    {
        _drag = false;
        if (e.Buttons == MouseButtons.Alternate && UnderCursor != null)
            _showContextMenu.OnNext(e.Location);
    }

    public void OnMouseMove(MouseEventArgs e)
    {
        if (_drag)
            MoveUnderCursor(e.Location);
        else
            FindUnderCursor(e.Location);
        _cursor.OnNext(Utils.HitZoneToCursor(_hitZone));
    }

    public void OnPaint(PaintEventArgs e)
    {
        e.Graphics.ImageInterpolation = ImageInterpolation.Low;
        e.Graphics.Clear(_settings.BackgroundColor);
        foreach (var pin in _board.Pins.Items)
            DrawPin(pin, e.Graphics);
    }

    private void FindUnderCursor(PointF location)
    {
        var viewTransform = _viewModel.BoardViewTransform;
        foreach (var pin in _board.Pins.Items.Reverse())
        {
            var r = pin.GetViewBounds(viewTransform);
            HitZone hitZone;
            if (pin.CanResize)
                hitZone = Utils.HitTest(r, location, _settings.DragMargin);
            else
                hitZone = r.Contains(location) ? HitZone.Center : HitZone.Outside;

            if (hitZone != HitZone.Outside)
            {
                UnderCursor = pin;
                _hitZone = hitZone;
                return;
            }
        }

        UnderCursor = null;
        _hitZone = HitZone.Outside;
    }

    private void MoveUnderCursor(PointF location)
    {
        var viewRect = UnderCursor!.GetViewBounds(_viewModel.BoardViewTransform);

        if (_hitZone is HitZone.Center)
            viewRect.Center = location + _dragOffset;
        if (_hitZone is HitZone.Left or HitZone.TopLeft or HitZone.BottomLeft)
            viewRect.Left = location.X;
        if (_hitZone is HitZone.Top or HitZone.TopLeft or HitZone.TopRight)
            viewRect.Top = location.Y;
        if (_hitZone is HitZone.Right or HitZone.TopRight or HitZone.BottomRight)
            viewRect.Right = location.X;
        if (_hitZone is HitZone.Bottom or HitZone.BottomLeft or HitZone.BottomRight)
            viewRect.Bottom = location.Y;

        if (_hitZone is HitZone.TopLeft)
            viewRect.TopLeft = FixProportions(viewRect.TopLeft, viewRect.BottomRight, UnderCursor.OriginalSize);
        if (_hitZone is HitZone.TopRight)
            viewRect.TopRight = FixProportions(viewRect.TopRight, viewRect.BottomLeft, UnderCursor.OriginalSize);
        if (_hitZone is HitZone.BottomRight)
            viewRect.BottomRight = FixProportions(viewRect.BottomRight, viewRect.TopLeft, UnderCursor.OriginalSize);
        if (_hitZone is HitZone.BottomLeft)
            viewRect.BottomLeft = FixProportions(viewRect.BottomLeft, viewRect.TopRight, UnderCursor.OriginalSize);

        UnderCursor.Center = _viewModel.ViewBoardTransform.TransformPoint(viewRect.Center);
        var boardSize = _viewModel.ViewBoardTransform.TransformSize(viewRect.Size);

        if (_hitZone is HitZone.Top or HitZone.Bottom)
            UnderCursor.Scale = boardSize.Height / UnderCursor.OriginalSize.Height;
        else
            UnderCursor.Scale = boardSize.Width / UnderCursor.OriginalSize.Width;
    }

    private static PointF FixProportions(PointF guess, PointF anchor, SizeF original)
    {
        var diff = guess - anchor;
        var scale = Math.Max(Math.Abs(diff.X) / original.Width, Math.Abs(diff.Y) / original.Height);
        return anchor + new SizeF(original.Width * scale * Math.Sign(diff.X), original.Height * scale * Math.Sign(diff.Y));
    }

    private void DrawPin(Pin pin, Graphics g)
    {
        var boardViewTransform = _viewModel.BoardViewTransform;
        pin.Render(g, boardViewTransform);

        RectangleF r = pin.GetViewBounds(boardViewTransform);
        if (pin == UnderCursor)
        {
            r.BottomRight -= 1;
            var path = GraphicsPath.GetRoundRect(r, 3);
            g.DrawPath(new Pen(_settings.BackgroundColor, 4), path);
            g.DrawPath(new Pen(Colors.White, 2), path);
        }
        else
        {
            r.TopLeft -= 1;
            var path = GraphicsPath.GetRoundRect(r, 3);
            g.DrawPath(new Pen(_settings.BackgroundColor, 2), path);
        }
    }

    private bool PullForwardCanExecute()
    {
        return UnderCursor != null && _board.Pins.Items.IndexOf(UnderCursor) < _board.Pins.Count - 1;
    }

    private void PullForwardExecute(object? sender, EventArgs e)
    {
        var i = _board.Pins.Items.IndexOf(UnderCursor);
        _board.Pins.Move(i, i + 1);
    }

    private bool PushBackCanExecute()
    {
        return UnderCursor != null && _board.Pins.Items.IndexOf(UnderCursor) > 0;
    }

    private void PushBackExecute(object? sender, EventArgs e)
    {
        var i = _board.Pins.Items.IndexOf(UnderCursor);
        _board.Pins.Move(i, i - 1);
    }

    private bool DelPinCanExecute()
    {
        return UnderCursor != null;
    }

    private void DelPinExecute(object? sender, EventArgs e)
    {
        _board.Pins.Remove(UnderCursor);
    }

    private bool CropCanExecute()
    {
        return UnderCursor is { CanCrop: true };
    }

    private void CropExecute(object? sender, EventArgs e)
    {
        _newEditMode.OnNext(new CropEditMode(_viewModel, UnderCursor, _settings, this));
    }
}
