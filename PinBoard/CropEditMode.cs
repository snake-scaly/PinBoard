using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eto.Drawing;
using Eto.Forms;

namespace PinBoard;

public sealed class CropEditMode : IEditMode
{
    private readonly PanZoomModel _viewModel;
    private readonly Pin _pin;
    private readonly Settings _settings;
    private readonly IEditMode _previousMode;

    private HitZone _hitZone;
    private bool _drag;
    private PointF _dragOffset;

    private readonly RectangleF _imageRect;
    private RectangleF _cropRect;

    private readonly Subject<bool> _invalidated = new();
    private readonly Subject<Cursor> _cursor = new();
    private readonly Subject<PointF> _showContextMenu = new();
    private readonly Subject<IEditMode> _newEditMode = new();

    private readonly CompositeDisposable _disposables = new();

    public CropEditMode(PanZoomModel viewModel, Pin pin, Settings settings, IEditMode previousMode)
    {
        _viewModel = viewModel;
        _pin = pin;
        _settings = settings;
        _previousMode = previousMode;

        _imageRect = new RectangleF(
            _pin.Center - (_pin.Image.SourceRect.TopLeft + _pin.Image.SourceRect.Size / 2) * _pin.Scale,
            _pin.Image.Source.Size * _pin.Scale);
        _cropRect = _pin.Bounds;

        Cursor = _cursor.DistinctUntilChanged();

        var doneCommand = new Command(DoneExecute) { MenuText = "Done" };
        var cancelCommand = new Command(CancelExecute) { MenuText = "Cancel" };
        ContextMenu = new ContextMenu(doneCommand, cancelCommand);
    }

    public ContextMenu ContextMenu { get; }
    public IObservable<bool> Invalidated => _invalidated.AsObservable();
    public IObservable<Cursor> Cursor { get; }
    public IObservable<PointF> ShowContextMenu => _showContextMenu.AsObservable();
    public IObservable<IEditMode> NewEditMode => _newEditMode.AsObservable();

    public void Dispose()
    {
        _disposables.Dispose();
    }

    public void OnMouseDown(MouseEventArgs e)
    {
        if (e.Buttons == MouseButtons.Primary && _hitZone != HitZone.Outside)
        {
            _drag = true;
            _dragOffset = _cropRect.Location - _viewModel.ViewToBoard(e.Location);
        }
    }

    public void OnMouseUp(MouseEventArgs e)
    {
        _drag = false;
        if (e.Buttons == MouseButtons.Alternate)
            _showContextMenu.OnNext(e.Location);
    }

    public void OnMouseMove(MouseEventArgs e)
    {
        if (_drag)
            MoveUnderCursor(e.Location);
        else

            _hitZone = Utils.HitTest(_viewModel.BoardToView(_cropRect), e.Location, _settings.DragMargin);
        _cursor.OnNext(Utils.HitZoneToCursor(_hitZone));
    }

    public void OnPaint(PaintEventArgs e)
    {
        e.Graphics.ImageInterpolation = ImageInterpolation.Low;
        e.Graphics.Clear(_settings.BackgroundColor);

        e.Graphics.DrawImage(_pin.Image.Source, _viewModel.BoardToView(_imageRect));

        e.Graphics.FillRectangle(new Color(1, 1, 1, .5f), e.ClipRectangle);

        var sourceRect = (_cropRect - _imageRect.TopLeft) / _pin.Scale;
        var viewRect = _viewModel.BoardToView(_cropRect);

        e.Graphics.DrawImage(_pin.Image.Source, sourceRect, viewRect);

        var path = GraphicsPath.GetRoundRect(viewRect, 3);
        e.Graphics.DrawPath(new Pen(_settings.BackgroundColor, 4), path);
        e.Graphics.DrawPath(new Pen(Colors.White, 2), path);
    }

    private void MoveUnderCursor(PointF location)
    {
        var boardLocation = _viewModel.ViewToBoard(location);
        var minSize = _settings.DragMargin * 2;

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

        _invalidated.OnNext(true);
    }

    private void DoneExecute(object? sender, EventArgs e)
    {
        _pin.Image.SourceRect = (_cropRect - _imageRect.TopLeft) / _pin.Scale;
        _pin.Center = _cropRect.Center;
        _newEditMode.OnNext(_previousMode);
    }

    private void CancelExecute(object? sender, EventArgs e)
    {
        _newEditMode.OnNext(_previousMode);
    }
}
