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

    private bool _drag;
    private PointF _dragOffset;
    private PointF? _lastMouseLocation;

    private readonly Subject<Unit> _invalidated = new();
    private readonly Subject<PointF> _showContextMenu = new();
    private readonly Subject<IEditMode> _newEditMode = new();
    
    private readonly CompositeDisposable _disposables = new();

    public BoardEditMode(Board board, PanZoomModel viewModel, Settings settings)
    {
        _board = board;
        _viewModel = viewModel;
        _settings = settings;

        Cursor = this.WhenAnyValue(x => x.CurrentHitZone)
            .Select(Utils.HitZoneToCursor)
            .DistinctUntilChanged();

        var pullForwardCommand = new Command(PullForwardExecute) { MenuText = "Pull Forward" };
        var pushBackCommand = new Command(PushBackExecute) { MenuText = "Push Back" };
        var cropCommand = new Command(CropExecute) { MenuText = "Crop" };
        var delPinCommand = new Command(DelPinExecute) { MenuText = "Close" };

        ContextMenu = new ContextMenu(pullForwardCommand, pushBackCommand, cropCommand, delPinCommand);

        var pinListChanges = _board.Pins.Connect().Publish();
        pinListChanges.Connect().DisposeWith(_disposables);

        var pinContentChanges = pinListChanges.MergeMany(x => x.Changed).Select(_ => Unit.Default).Publish();
        pinContentChanges.Connect().DisposeWith(_disposables);

        // When anything changes
        this.WhenAny(x => x.UnderCursor, _ => Unit.Default)
            .Merge(pinListChanges.Select(_ => Unit.Default))
            .Merge(pinContentChanges)
            .Subscribe(_ => _invalidated.OnNext(default))
            .DisposeWith(_disposables);

        // When the list order or state of any item in the list changes
        pinListChanges.Select(_ => Unit.Default)
            .Merge(pinContentChanges)
            .Subscribe(
                _ =>
                {
                    if (_lastMouseLocation != null && !_drag)
                        FindUnderCursor(_lastMouseLocation.Value);
                })
            .DisposeWith(_disposables);

        // When selection or selected item state changes
        this.WhenAnyObservable(x => x.UnderCursor.Changed).Select(_ => Unit.Default)
            .Merge(this.WhenAny(x => x.UnderCursor, _ => Unit.Default))
            .Subscribe(_ =>
            {
                cropCommand.Enabled = CropCanExecute();
                delPinCommand.Enabled = DelPinCanExecute();
            })
            .DisposeWith(_disposables);

        // When list order or selection changes
        pinListChanges.Select(_ => Unit.Default)
            .Merge(this.WhenAny(x => x.UnderCursor, _ => Unit.Default))
            .Subscribe(
                _ =>
                {
                    pullForwardCommand.Enabled = PullForwardCanExecute();
                    pushBackCommand.Enabled = PushBackCanExecute();
                })
            .DisposeWith(_disposables);
    }

    public ContextMenu ContextMenu { get; }
    public IObservable<Unit> Invalidated => _invalidated.AsObservable();
    public IObservable<Cursor> Cursor { get; }
    public IObservable<PointF> ShowContextMenu => _showContextMenu.AsObservable();
    public IObservable<IEditMode> NewEditMode => _newEditMode.AsObservable();

    [Reactive]
    private Pin? UnderCursor { get; set; }

    [Reactive]
    private HitZone CurrentHitZone { get; set; }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void Attach(Control owner)
    {
        owner.MouseDown += OnMouseDown;
        owner.MouseUp += OnMouseUp;
        owner.MouseMove += OnMouseMove;
        owner.DragEnter += OnDragEnter;
        owner.DragDrop += OnDragDrop;
    }

    public void Detach(Control owner)
    {
        owner.MouseDown -= OnMouseDown;
        owner.MouseUp -= OnMouseUp;
        owner.MouseMove -= OnMouseMove;
        owner.DragEnter -= OnDragEnter;
        owner.DragDrop -= OnDragDrop;
    }

    public void OnPaint(PaintEventArgs e)
    {
        e.Graphics.ImageInterpolation = ImageInterpolation.Low;
        e.Graphics.Clear(_settings.BackgroundColor);
        foreach (var pin in _board.Pins.Items)
            DrawPin(pin, e.Graphics);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _lastMouseLocation = e.Location;
        FindUnderCursor(e.Location);
        if (e.Buttons == MouseButtons.Primary && UnderCursor != null)
        {
            _drag = true;
            _dragOffset = _viewModel.BoardViewTransform.TransformPoint(UnderCursor.Center) - e.Location;
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        _lastMouseLocation = e.Location;
        _drag = false;
        if (e.Buttons == MouseButtons.Alternate && UnderCursor != null)
            _showContextMenu.OnNext(e.Location);
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        _lastMouseLocation = e.Location;
        if (_drag)
            MoveUnderCursor(e.Location);
        else
            FindUnderCursor(e.Location);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effects = e.AllowedEffects & DragEffects.Copy | DragEffects.Link;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        _lastMouseLocation = e.Location;

        var control = sender as Control ?? throw new InvalidOperationException("Sender is not a control");

        var boardLocation = _viewModel.ViewBoardTransform.TransformPoint(e.Location);
        var boardViewport = _viewModel.ViewBoardTransform.TransformRectangle(new RectangleF(default, control.Size));

        if (e.Data.ContainsImage)
        {
            _board.Add(e.Data.Image, boardViewport, boardLocation);
        }
        else if (e.Data.ContainsUris)
        {
            if (e.Data.Uris.Length == 1)
                _board.Add(e.Data.Uris[0], boardViewport, boardLocation);
            else
                foreach (var uri in e.Data.Uris)
                    _board.Add(uri, boardViewport);
        }
        else if (e.Data.ContainsText)
        {
            _board.Add(new Uri(e.Data.Text), boardViewport, boardLocation);
        }
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
                CurrentHitZone = hitZone;
                return;
            }
        }

        UnderCursor = null;
        CurrentHitZone = HitZone.Outside;
    }

    private void MoveUnderCursor(PointF location)
    {
        var viewRect = UnderCursor!.GetViewBounds(_viewModel.BoardViewTransform);

        if (CurrentHitZone is HitZone.Center)
            viewRect.Center = location + _dragOffset;
        if (CurrentHitZone is HitZone.Left or HitZone.TopLeft or HitZone.BottomLeft)
            viewRect.Left = location.X;
        if (CurrentHitZone is HitZone.Top or HitZone.TopLeft or HitZone.TopRight)
            viewRect.Top = location.Y;
        if (CurrentHitZone is HitZone.Right or HitZone.TopRight or HitZone.BottomRight)
            viewRect.Right = location.X;
        if (CurrentHitZone is HitZone.Bottom or HitZone.BottomLeft or HitZone.BottomRight)
            viewRect.Bottom = location.Y;

        if (CurrentHitZone is HitZone.TopLeft)
            viewRect.TopLeft = FixProportions(viewRect.TopLeft, viewRect.BottomRight, UnderCursor.OriginalSize);
        if (CurrentHitZone is HitZone.TopRight)
            viewRect.TopRight = FixProportions(viewRect.TopRight, viewRect.BottomLeft, UnderCursor.OriginalSize);
        if (CurrentHitZone is HitZone.BottomRight)
            viewRect.BottomRight = FixProportions(viewRect.BottomRight, viewRect.TopLeft, UnderCursor.OriginalSize);
        if (CurrentHitZone is HitZone.BottomLeft)
            viewRect.BottomLeft = FixProportions(viewRect.BottomLeft, viewRect.TopRight, UnderCursor.OriginalSize);

        UnderCursor.Center = _viewModel.ViewBoardTransform.TransformPoint(viewRect.Center);
        var boardSize = _viewModel.ViewBoardTransform.TransformSize(viewRect.Size);

        if (CurrentHitZone is HitZone.Top or HitZone.Bottom)
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
        _board.Pins.Remove(UnderCursor!);
    }

    private bool CropCanExecute()
    {
        return UnderCursor is { CanCrop: true };
    }

    private void CropExecute(object? sender, EventArgs e)
    {
        _newEditMode.OnNext(new CropEditMode(_viewModel, UnderCursor!, _settings, this));
    }
}
