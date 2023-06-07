using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eto.Drawing;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Util;
using PinBoard.ViewModels;

namespace PinBoard.Controls;

public sealed class CropEditMode : IEditMode
{
    private readonly PanZoomModel _viewModel;
    private readonly PinViewModel _pinView;
    private readonly Settings _settings;
    private readonly IEditMode _previousMode;

    private HitZone _hitZone;
    private bool _drag;
    private PointF _dragOffset;

    private readonly RectangleF _imageRect;
    private RectangleF _cropRect;

    private readonly Subject<Unit> _invalidated = new();
    private readonly Subject<Cursor> _cursor = new();
    private readonly Subject<PointF> _showContextMenu = new();
    private readonly Subject<IEditMode> _newEditMode = new();

    private readonly CompositeDisposable _disposables = new();

    public CropEditMode(PanZoomModel viewModel, PinViewModel pinView, Settings settings, IEditMode previousMode)
    {
        _viewModel = viewModel;
        _pinView = pinView;
        _settings = settings;
        _previousMode = previousMode;

        _imageRect = new RectangleF(
            _pinView.Pin.Center - _pinView.Pin.CropRect.Value.Center * _pinView.Pin.Scale.Value,
            _pinView.Image.Size * _pinView.Pin.Scale.Value);
        _cropRect = _pinView.DisplayRect.Value;

        Cursor = _cursor.DistinctUntilChanged();

        var doneCommand = new Command(DoneExecute) { MenuText = "Done" };
        var cancelCommand = new Command(CancelExecute) { MenuText = "Cancel" };
        ContextMenu = new ContextMenu(doneCommand, cancelCommand);
    }

    public ContextMenu ContextMenu { get; }
    public IObservable<Unit> Invalidated => _invalidated.AsObservable();
    public IObservable<Cursor> Cursor { get; }
    public IObservable<PointF> ShowContextMenu => _showContextMenu.AsObservable();
    public IObservable<IEditMode> NewEditMode => _newEditMode.AsObservable();

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void Attach(Control owner)
    {
        owner.MouseDown += OnMouseDown;
        owner.MouseUp += OnMouseUp;
        owner.MouseMove += OnMouseMove;
    }

    public void Detach(Control owner)
    {
        owner.MouseDown -= OnMouseDown;
        owner.MouseUp -= OnMouseUp;
        owner.MouseMove -= OnMouseMove;
    }

    public void OnPaint(PaintEventArgs e)
    {
        e.Graphics.ImageInterpolation = ImageInterpolation.Low;
        e.Graphics.Clear(_settings.BackgroundColor);

        var viewCropRect = _viewModel.BoardViewTransform.TransformRectangle(_cropRect);
        var viewImageRect = _viewModel.BoardViewTransform.TransformRectangle(_imageRect);

        e.Graphics.DrawImage(_pinView.Image, viewImageRect);
        foreach (var r in Cutout(e.ClipRectangle, viewCropRect))
            e.Graphics.FillRectangle(new Color(1, 1, 1, .5f), r);

        var path = GraphicsPath.GetRoundRect(viewCropRect, 3);
        e.Graphics.DrawPath(new Pen(_settings.BackgroundColor, 4), path);
        e.Graphics.DrawPath(new Pen(Colors.White, 2), path);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Buttons == MouseButtons.Primary && _hitZone != HitZone.Outside)
        {
            _drag = true;
            _dragOffset = _cropRect.Location - _viewModel.ViewBoardTransform.TransformPoint(e.Location);
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        _drag = false;
        if (e.Buttons == MouseButtons.Alternate)
            _showContextMenu.OnNext(e.Location);
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_drag)
            MoveUnderCursor(e.Location);
        else
            _hitZone = Utils.HitTest(_viewModel.BoardViewTransform.TransformRectangle(_cropRect), e.Location, _settings.DragMargin);
        _cursor.OnNext(Utils.HitZoneToCursor(_hitZone));
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

    private void MoveUnderCursor(PointF location)
    {
        var minSize = _settings.DragMargin * 2;
        var boardLocation = _viewModel.ViewBoardTransform.TransformPoint(location);

        if (_hitZone is HitZone.Center)
        {
            _cropRect.Location =
                new PointF(
                    Math.Clamp(boardLocation.X + _dragOffset.X, _imageRect.Left, _imageRect.Right - _cropRect.Width),
                    Math.Clamp(boardLocation.Y + _dragOffset.Y, _imageRect.Top, _imageRect.Bottom - _cropRect.Height));
        }

        if (_hitZone is HitZone.Left or HitZone.TopLeft or HitZone.BottomLeft)
            _cropRect.Left = Math.Clamp(boardLocation.X, _imageRect.Left, _cropRect.Right - minSize);
        if (_hitZone is HitZone.Top or HitZone.TopLeft or HitZone.TopRight)
            _cropRect.Top = Math.Clamp(boardLocation.Y, _imageRect.Top, _cropRect.Bottom - minSize);
        if (_hitZone is HitZone.Right or HitZone.TopRight or HitZone.BottomRight)
            _cropRect.Right = Math.Clamp(boardLocation.X, _cropRect.Left + minSize, _imageRect.Right);
        if (_hitZone is HitZone.Bottom or HitZone.BottomLeft or HitZone.BottomRight)
            _cropRect.Bottom = Math.Clamp(boardLocation.Y, _cropRect.Top + minSize, _imageRect.Bottom);

        _invalidated.OnNext(default);
    }

    private void DoneExecute(object? sender, EventArgs e)
    {
        var pinCropRect = (_cropRect - _imageRect.TopLeft) / _pinView.Pin.Scale.Value;
        pinCropRect.Restrict(_pinView.Image.Size);
        _pinView.Pin.Edit(
            pin =>
            {
                pin.CropRect = (_cropRect - _imageRect.TopLeft) / _pinView.Pin.Scale;
                pin.Center = _cropRect.Center;
            });
        _newEditMode.OnNext(_previousMode);
    }

    private void CancelExecute(object? sender, EventArgs e)
    {
        _newEditMode.OnNext(_previousMode);
    }
}
