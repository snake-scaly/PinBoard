using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Util;
using PinBoard.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard.UI;

public class BoardPinCropEditor : BoardControl
{
    private readonly PinViewModel _pin;
    private readonly PanZoomModel _viewModel;
    private readonly Settings _settings;

    private readonly ReactiveValue<HitZone> _hitZone = new();
    private readonly ContextMenu _contextMenu;

    private bool _drag;
    private PointF _dragOffset;

    public BoardPinCropEditor(
        PinViewModel pin,
        PanZoomModel viewModel,
        ICommand applyCommand,
        ICommand cancelCommand,
        Settings settings)
    {
        _pin = pin;
        _viewModel = viewModel;
        _settings = settings;

        ImageRect = new RectangleF(
            _pin.Pin.Center - _pin.Pin.CropRect!.Value.Center * _pin.Pin.Scale!.Value,
            _pin.Image!.Size * _pin.Pin.Scale.Value);
        CropRect = _pin.DisplayRect!.Value;

        _viewModel.Changed.Subscribe(_ => Invalidate())
            .DisposeWith(Disposables);

        _hitZone.WhenAnyValue(x => x.Value)
            .Select(Utils.HitZoneToCursor)
            .BindTo(this, x => x.Cursor)
            .DisposeWith(Disposables);

        var applyMenuItem = new Command { MenuText = "Apply", DelegatedCommand = applyCommand };
        var cancelMenuItem = new Command { MenuText = "Cancel", DelegatedCommand = cancelCommand };
        _contextMenu = new ContextMenu(applyMenuItem, cancelMenuItem);
    }

    public RectangleF ImageRect { get; }
    [Reactive] public RectangleF CropRect { get; private set; }

    public override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Buttons == MouseButtons.Primary)
        {
            _drag = true;
            _dragOffset = CropRect.Location - _viewModel.ViewBoardTransform.TransformPoint(e.Location);
        }
        else if (e.Buttons == MouseButtons.Alternate)
        {
            _contextMenu.Show();
        }
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        _drag = false;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        if (_drag)
        {
            var minSize = _settings.DragMargin * 2;
            var boardLocation = _viewModel.ViewBoardTransform.TransformPoint(e.Location);

            var cropRect = CropRect;

            if (_hitZone.Value is HitZone.Center)
            {
                cropRect.Location =
                    new PointF(
                        ClampSafe(boardLocation.X + _dragOffset.X, ImageRect.Left, ImageRect.Right - cropRect.Width),
                        ClampSafe(boardLocation.Y + _dragOffset.Y, ImageRect.Top, ImageRect.Bottom - cropRect.Height));
            }

            if (_hitZone.Value is HitZone.Left or HitZone.TopLeft or HitZone.BottomLeft)
                cropRect.Left = ClampSafe(boardLocation.X, ImageRect.Left, cropRect.Right - minSize);
            if (_hitZone.Value is HitZone.Top or HitZone.TopLeft or HitZone.TopRight)
                cropRect.Top = ClampSafe(boardLocation.Y, ImageRect.Top, cropRect.Bottom - minSize);
            if (_hitZone.Value is HitZone.Right or HitZone.TopRight or HitZone.BottomRight)
                cropRect.Right = ClampSafe(boardLocation.X, cropRect.Left + minSize, ImageRect.Right);
            if (_hitZone.Value is HitZone.Bottom or HitZone.BottomLeft or HitZone.BottomRight)
                cropRect.Bottom = ClampSafe(boardLocation.Y, cropRect.Top + minSize, ImageRect.Bottom);

            CropRect = cropRect;

            Invalidate();
        }
        else
        {
            var viewCropRect = _viewModel.BoardViewTransform.TransformRectangle(CropRect);
            viewCropRect.Offset(-Location);
            _hitZone.Value = Utils.HitTest(viewCropRect, e.Location, _settings.DragMargin);
        }
    }

    public override void OnMouseEnter(MouseEventArgs e)
    {
        OnMouseMove(e);
    }

    public override void OnMouseLeave(MouseEventArgs e)
    {
        if (!_drag && _hitZone.Value is not HitZone.Outside)
            _hitZone.Value = HitZone.Outside;
    }

    public override void OnPaint(PaintEventArgs e)
    {
        var viewCropRect = _viewModel.BoardViewTransform.TransformRectangle(CropRect);
        var viewImageRect = _viewModel.BoardViewTransform.TransformRectangle(ImageRect);

        e.Graphics.DrawImage(_pin.Image, viewImageRect);
        foreach (var r in Cutout(e.ClipRectangle, viewCropRect))
            e.Graphics.FillRectangle(new Color(1, 1, 1, .5f), r);

        var frameRect = viewCropRect;
        frameRect.TopLeft -= 1;
        var path = GraphicsPath.GetRoundRect(frameRect, 3);
        e.Graphics.DrawPath(new Pen(_settings.BackgroundColor, 4), path);
        e.Graphics.DrawPath(new Pen(_settings.ImageOutlineFocusColor, 2), path);
    }

    private static IEnumerable<RectangleF> Cutout(RectangleF rect, RectangleF hole)
    {
        hole.Restrict(rect);
        hole = new RectangleF((float)Math.Round(hole.X), (float)Math.Round(hole.Y), (float)Math.Round(hole.Width), (float)Math.Round(hole.Height));

        if (hole.IsEmpty)
        {
            yield return rect;
            yield break;
        }

        if (hole.Top > rect.Top)
            yield return new RectangleF(rect.Left, rect.Top, rect.Width, hole.Top - rect.Top);
        if (hole.Bottom < rect.Bottom)
            yield return new RectangleF(rect.Left, hole.Bottom, rect.Width, rect.Bottom - hole.Bottom);
        if (hole.Left > rect.Left)
            yield return new RectangleF(rect.Left, hole.Top, hole.Left - rect.Left, hole.Height);
        if (hole.Right < rect.Right)
            yield return new RectangleF(hole.Right, hole.Top, rect.Right - hole.Right, hole.Height);
    }

    private static float ClampSafe(float x, float min, float max) => Math.Clamp(x, min, Math.Max(min, max));
}
