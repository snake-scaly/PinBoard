using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Util;
using PinBoard.ViewModels;
using ReactiveUI;

namespace PinBoard.UI;

public class BoardPin : BoardControl
{
    private readonly PinViewModel _pin;
    private readonly PanZoomModel _viewModel;
    private readonly Settings _settings;

    private readonly ReactiveValue<HitZone> _hitZone = new();
    private readonly Subject<Unit> _clickSubject = new();
    private readonly ContextMenu _contextMenu;

    private bool _hover;
    private bool _drag;
    private PointF _dragPoint;
    private bool _didDrag;
    private bool _focused;

    public BoardPin(
        PinViewModel pin,
        PanZoomModel viewModel,
        ICommand pullForwardCommand,
        ICommand pushBackCommand,
        ICommand cropCommand,
        ICommand deleteCommand,
        Settings settings)
    {
        _pin = pin;
        _viewModel = viewModel;
        _settings = settings;

        pin.Updates.Subscribe(_ => OnPinUpdated())
            .DisposeWith(Disposables);
        _viewModel.Changed.Subscribe(_ => OnPinUpdated())
            .DisposeWith(Disposables);

        _hitZone.WhenAnyValue(x => x.Value)
            .Select(Utils.HitZoneToCursor)
            .BindTo(this, x => x.Cursor)
            .DisposeWith(Disposables);

        var pullForwardMenuItem = new Command { MenuText = "Pull Forward", DelegatedCommand = pullForwardCommand, CommandParameter = this };
        var pushBackMenuItem = new Command { MenuText = "Push Back", DelegatedCommand = pushBackCommand, CommandParameter = this };
        var cropMenuItem = new Command { MenuText = "Crop", DelegatedCommand = cropCommand, CommandParameter = this };
        var delPinMenuItem = new Command { MenuText = "Close", DelegatedCommand = deleteCommand, CommandParameter = this };

        _contextMenu = new ContextMenu(pullForwardMenuItem, pushBackMenuItem, cropMenuItem, delPinMenuItem);

        OnPinUpdated();
    }
    
    public IObservable<Unit> Click => _clickSubject.AsObservable();
    public PinViewModel PinViewModel => _pin;

    public void SetFocus(bool focus)
    {
        _focused = focus;
        Invalidate();
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Buttons == MouseButtons.Primary)
        {
            _drag = true;
            _dragPoint = e.Location;
            _didDrag = false;
        }
        else if (e.Buttons == MouseButtons.Alternate)
        {
            _contextMenu.Show();
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        _drag = false;
        if (e.Buttons == MouseButtons.Primary && !_didDrag)
            _clickSubject.OnNext(Unit.Default);
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        if (_drag)
        {
            _didDrag = true;

            var viewRect = new RectangleF(Size);
            viewRect.Inset(_settings.DragMargin);
            var hitZone = _hitZone.Value;
            var minSize = _settings.DragMargin * 3;

            if (hitZone is HitZone.Center)
                viewRect.Center += e.Location - _dragPoint;
            if (hitZone is HitZone.Left or HitZone.TopLeft or HitZone.BottomLeft)
                viewRect.Left = Math.Min(e.Location.X, viewRect.Right - minSize);
            if (hitZone is HitZone.Top or HitZone.TopLeft or HitZone.TopRight)
                viewRect.Top = Math.Min(e.Location.Y, viewRect.Bottom - minSize);
            if (hitZone is HitZone.Right or HitZone.TopRight or HitZone.BottomRight)
                viewRect.Right = Math.Max(e.Location.X, viewRect.Left + minSize);
            if (hitZone is HitZone.Bottom or HitZone.BottomLeft or HitZone.BottomRight)
                viewRect.Bottom = Math.Max(e.Location.Y, viewRect.Top + minSize);

            if (hitZone is HitZone.TopLeft)
                viewRect.TopLeft = FixProportions(viewRect.TopLeft, viewRect.BottomRight, _pin.Pin.CropRect.Value.Size);
            if (hitZone is HitZone.TopRight)
                viewRect.TopRight = FixProportions(viewRect.TopRight, viewRect.BottomLeft, _pin.Pin.CropRect.Value.Size);
            if (hitZone is HitZone.BottomRight)
                viewRect.BottomRight = FixProportions(viewRect.BottomRight, viewRect.TopLeft, _pin.Pin.CropRect.Value.Size);
            if (hitZone is HitZone.BottomLeft)
                viewRect.BottomLeft = FixProportions(viewRect.BottomLeft, viewRect.TopRight, _pin.Pin.CropRect.Value.Size);

            viewRect.Offset(Location);

            _pin.Pin.Edit(
                pin =>
                {
                    pin.Center = _viewModel.ViewBoardTransform.TransformPoint(viewRect.Center);
                    var boardSize = _viewModel.ViewBoardTransform.TransformSize(viewRect.Size);

                    if (hitZone is HitZone.Top or HitZone.Bottom)
                        pin.Scale = boardSize.Height / pin.CropRect.Value.Height;
                    else
                        pin.Scale = boardSize.Width / pin.CropRect.Value.Width;
                });

            Invalidate();
        }
        else
        {
            if (_pin.Image != null)
                _hitZone.Value = Utils.HitTest(new RectangleF(Size), e.Location, _settings.DragMargin * 2);
            else
                _hitZone.Value = HitZone.Center;
        }
    }

    public override void OnMouseEnter(MouseEventArgs e)
    {
        _hover = true;
        Invalidate();
    }

    public override void OnMouseLeave(MouseEventArgs e)
    {
        _hover = false;
        if (!_drag)
            _hitZone.Value = HitZone.Outside;
        Invalidate();
    }

    public override void OnPaint(PaintEventArgs e)
    {
        var r = new RectangleF(Size);
        r.Inset(_settings.DragMargin);

        if (_pin.Image != null)
            e.Graphics.DrawImage(_pin.Image, _pin.Pin.CropRect ?? new RectangleF(_pin.Image.Size), r);
        else if (_pin.Icon != null)
            e.Graphics.DrawImage(_pin.Icon, r.Location);

        r.TopLeft -= 1;
        var p = GraphicsPath.GetRoundRect(r, 3);
        if (_focused)
        {
            e.Graphics.DrawPath(new Pen(_settings.BackgroundColor, 4), p);
            e.Graphics.DrawPath(new Pen(_settings.ImageOutlineFocusColor, 2), p);
        }
        else if (_hover || _drag)
        {
            e.Graphics.DrawPath(new Pen(_settings.BackgroundColor, 3), p);
            e.Graphics.DrawPath(new Pen(_settings.ImageOutlineHoverColor), p);
        }
        else
        {
            e.Graphics.DrawPath(new Pen(_settings.BackgroundColor, 2), p);
        }
    }

    private void OnPinUpdated()
    {
        RectangleF pinRect;

        if (_pin.Image != null && _pin.DisplayRect != null)
        {
            pinRect = _viewModel.BoardViewTransform.TransformRectangle(_pin.DisplayRect.Value);
            pinRect.Inflate(_settings.DragMargin, _settings.DragMargin);
        }
        else if (_pin.Icon != null)
        {
            pinRect = new RectangleF(_pin.Icon.Size) { Center = _viewModel.BoardViewTransform.TransformPoint(_pin.Pin.Center) };
        }
        else
        {
            pinRect = new RectangleF(new SizeF(64, 64)) { Center = _viewModel.BoardViewTransform.TransformPoint(_pin.Pin.Center) };
        }

        Location = pinRect.Location;
        Size = pinRect.Size;
        Invalidate();
    }

    private static PointF FixProportions(PointF guess, PointF anchor, SizeF original)
    {
        var diff = guess - anchor;
        var scale = Math.Max(Math.Abs(diff.X) / original.Width, Math.Abs(diff.Y) / original.Height);
        scale = Math.Max(scale, .01f);
        return anchor + new SizeF(original.Width * scale * Math.Sign(diff.X), original.Height * scale * Math.Sign(diff.Y));
    }
}
