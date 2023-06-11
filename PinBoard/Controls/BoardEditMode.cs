using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Services;
using PinBoard.Util;
using PinBoard.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard.Controls;

public sealed class BoardEditMode : ReactiveObject, IEditMode
{
    private readonly Board _board;
    private readonly PanZoomModel _viewModel;
    private readonly Settings _settings;
    private readonly IEditModeFactory _editModeFactory;

    private readonly IObservableList<PinViewModel> _pinViews;
    private bool _drag;
    private PointF _dragOffset;
    private PointF? _lastMouseLocation;

    private readonly Subject<Unit> _invalidated = new();
    private readonly Subject<PointF> _showContextMenu = new();
    private readonly Subject<IEditMode> _newEditMode = new();
    
    private readonly CompositeDisposable _disposables = new();

    public BoardEditMode(Board board, PanZoomModel viewModel, Settings settings, IEditModeFactory editModeFactory, IPinViewModelFactory pinViewModelFactory)
    {
        _board = board;
        _viewModel = viewModel;
        _settings = settings;
        _editModeFactory = editModeFactory;

        Cursor = this.WhenAnyValue(x => x.CurrentHitZone)
            .Select(Utils.HitZoneToCursor)
            .DistinctUntilChanged();

        var pullForwardCommand = new Command(PullForwardExecute) { MenuText = "Pull Forward" };
        var pushBackCommand = new Command(PushBackExecute) { MenuText = "Push Back" };
        var cropCommand = new Command(CropExecute) { MenuText = "Crop" };
        var delPinCommand = new Command(DelPinExecute) { MenuText = "Close" };

        ContextMenu = new ContextMenu(pullForwardCommand, pushBackCommand, cropCommand, delPinCommand);

        _pinViews = _board.Pins.Connect()
            .Transform(pinViewModelFactory.CreatePinViewModel)
            .AsObservableList();
        _pinViews.DisposeWith(_disposables);

        var pinListChanges = _pinViews.Connect()
            .Publish();
        pinListChanges.Connect()
            .DisposeWith(_disposables);

        var pinContentChanges = pinListChanges.MergeMany(x => x.Updates)
            .Publish();
        pinContentChanges.Connect()
            .DisposeWith(_disposables);

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
        this.WhenAnyObservable(x => x.UnderCursor!.Updates)
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
    private PinViewModel? UnderCursor { get; set; }

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
        foreach (var pinView in _pinViews.Items)
            DrawPin(pinView, e.Graphics);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _lastMouseLocation = e.Location;
        FindUnderCursor(e.Location);
        if (e.Buttons == MouseButtons.Primary && UnderCursor != null)
        {
            _drag = true;
            _dragOffset = _viewModel.BoardViewTransform.TransformPoint(UnderCursor.Pin.Center) - e.Location;
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
        foreach (var pin in _pinViews.Items.Reverse())
        {
            HitZone hitZone = HitZone.Outside;

            if (pin.Image != null)
            {
                var r = viewTransform.TransformRectangle(pin.DisplayRect.Value);
                hitZone = Utils.HitTest(r, location, _settings.DragMargin);
            }
            else if (pin.Icon != null)
            {
                var center = viewTransform.TransformPoint(pin.Pin.Center);
                var r = new RectangleF(default, pin.Icon.Size) { Center = center };
                hitZone = r.Contains(location) ? HitZone.Center : HitZone.Outside;
            }

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
        if (UnderCursor!.Icon != null)
        {
            UnderCursor.Pin.Edit(pin => pin.Center = _viewModel.ViewBoardTransform.TransformPoint(location + _dragOffset));
            return;
        }

        var viewRect = _viewModel.BoardViewTransform.TransformRectangle(UnderCursor.DisplayRect.Value);

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
            viewRect.TopLeft = FixProportions(viewRect.TopLeft, viewRect.BottomRight, UnderCursor.Pin.CropRect.Value.Size);
        if (CurrentHitZone is HitZone.TopRight)
            viewRect.TopRight = FixProportions(viewRect.TopRight, viewRect.BottomLeft, UnderCursor.Pin.CropRect.Value.Size);
        if (CurrentHitZone is HitZone.BottomRight)
            viewRect.BottomRight = FixProportions(viewRect.BottomRight, viewRect.TopLeft, UnderCursor.Pin.CropRect.Value.Size);
        if (CurrentHitZone is HitZone.BottomLeft)
            viewRect.BottomLeft = FixProportions(viewRect.BottomLeft, viewRect.TopRight, UnderCursor.Pin.CropRect.Value.Size);

        UnderCursor.Pin.Edit(
            pin =>
            {
                pin.Center = _viewModel.ViewBoardTransform.TransformPoint(viewRect.Center);
                var boardSize = _viewModel.ViewBoardTransform.TransformSize(viewRect.Size);

                if (CurrentHitZone is HitZone.Top or HitZone.Bottom)
                    pin.Scale = boardSize.Height / pin.CropRect.Value.Height;
                else
                    pin.Scale = boardSize.Width / pin.CropRect.Value.Width;
            });
    }

    private static PointF FixProportions(PointF guess, PointF anchor, SizeF original)
    {
        var diff = guess - anchor;
        var scale = Math.Max(Math.Abs(diff.X) / original.Width, Math.Abs(diff.Y) / original.Height);
        return anchor + new SizeF(original.Width * scale * Math.Sign(diff.X), original.Height * scale * Math.Sign(diff.Y));
    }

    private void DrawPin(PinViewModel pinView, Graphics g)
    {
        var boardViewTransform = _viewModel.BoardViewTransform;
        RectangleF r;
        
        if (pinView.Image != null)
        {
            r = boardViewTransform.TransformRectangle(pinView.DisplayRect.Value); 
            g.DrawImage(pinView.Image, pinView.Pin.CropRect.Value, r);
        }
        else if (pinView.Icon != null)
        {
            var location = boardViewTransform.TransformPoint(pinView.Pin.Center) - pinView.Icon.Size / 2;
            g.DrawImage(pinView.Icon, location);
            r = new RectangleF(location, pinView.Icon.Size);
        }
        else
        {
            return;
        }
        
        if (pinView == UnderCursor)
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
        return UnderCursor != null && _board.Pins.Items.IndexOf(UnderCursor?.Pin) < _board.Pins.Count - 1;
    }

    private void PullForwardExecute(object? sender, EventArgs e)
    {
        var i = _board.Pins.Items.IndexOf(UnderCursor?.Pin);
        _board.Pins.Move(i, i + 1);
    }

    private bool PushBackCanExecute()
    {
        return UnderCursor != null && _board.Pins.Items.IndexOf(UnderCursor.Pin) > 0;
    }

    private void PushBackExecute(object? sender, EventArgs e)
    {
        var i = _board.Pins.Items.IndexOf(UnderCursor?.Pin);
        _board.Pins.Move(i, i - 1);
    }

    private bool DelPinCanExecute()
    {
        return UnderCursor != null;
    }

    private void DelPinExecute(object? sender, EventArgs e)
    {
        _board.Pins.Remove(UnderCursor?.Pin);
    }

    private bool CropCanExecute()
    {
        return UnderCursor?.Image != null;
    }

    private void CropExecute(object? sender, EventArgs e)
    {
        _newEditMode.OnNext(_editModeFactory.CreateCropEditMode(_viewModel, UnderCursor!, this));
    }
}
