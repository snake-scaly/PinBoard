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

    private readonly Subject<bool> _invalidated = new();
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

        this.WhenAnyValue(x => x.UnderCursor).Subscribe(_ => _invalidated.OnNext(true));
        
        var boardChanges = _board.Pins
            .Connect()
            .Publish();
        var listChanges = boardChanges
            .Where(x => x.Removes > 0 || x.Moves > 0)
            .Select(_ => true);
        boardChanges
            .MergeMany(x => x.Update)
            .Merge(listChanges)
            .Subscribe(_ => _invalidated.OnNext(true))
            .DisposeWith(_disposables);
        boardChanges
            .Connect()
            .DisposeWith(_disposables);

        var pinChanges = boardChanges.Select(_ => true);
        var topologyChanges = this.WhenAnyValue(x => x.UnderCursor).Select(_ => true).Merge(pinChanges);

        var pullForwardCommand = new Command(PullForwardExecute) { MenuText = "Pull Forward" };
        topologyChanges.Subscribe(_ => pullForwardCommand.Enabled = PullForwardCanExecute());

        var pushBackCommand = new Command(PushBackExecute) { MenuText = "Push Back" };
        topologyChanges.Subscribe(_ => pushBackCommand.Enabled = PushBackCanExecute());

        var delPinCommand = new Command(DelPinExecute) { MenuText = "Remove" };
        this.WhenAnyValue(x => x.UnderCursor).Subscribe(x => delPinCommand.Enabled = DelPinCanExecute());

        var cropCommand = new Command(CropExecute) { MenuText = "Crop" };
        this.WhenAnyValue(x => x.UnderCursor).Subscribe(x => cropCommand.Enabled = CropCanExecute());
        
        boardChanges.Connect().DisposeWith(_disposables);

        ContextMenu = new ContextMenu(pullForwardCommand, pushBackCommand, delPinCommand, cropCommand);
    }

    public ContextMenu ContextMenu { get; }
    public IObservable<bool> Invalidated => _invalidated.AsObservable();
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
            _dragOffset = UnderCursor.Center - _viewModel.ViewToBoard(e.Location);
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
        foreach (var pin in _board.Pins.Items.Reverse())
        {
            var hitZone = Utils.HitTest(_viewModel.BoardToView(pin.Bounds), location, _settings.DragMargin);
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
        var boardLocation = _viewModel.ViewToBoard(location);
        var r = UnderCursor!.Bounds;

        switch (_hitZone)
        {
            case HitZone.Center:
                UnderCursor.Center = boardLocation + _dragOffset;
                break;

            case HitZone.Left:
                r.Left = boardLocation.X;
                UnderCursor.Center = r.Center;
                UnderCursor.Scale = r.Width / UnderCursor.Image.Width;
                break;

            case HitZone.Top:
                r.Top = boardLocation.Y;
                UnderCursor.Center = r.Center;
                UnderCursor.Scale = r.Height / UnderCursor.Image.Height;
                break;

            case HitZone.Right:
                r.Right = boardLocation.X;
                UnderCursor.Center = r.Center;
                UnderCursor.Scale = r.Width / UnderCursor.Image.Width;
                break;

            case HitZone.Bottom:
                r.Bottom = boardLocation.Y;
                UnderCursor.Center = r.Center;
                UnderCursor.Scale = r.Height / UnderCursor.Image.Height;
                break;

            case HitZone.TopLeft:
            {
                var scaleX = (r.Right - boardLocation.X) / UnderCursor.Image.Width;
                var scaleY = (r.Bottom - boardLocation.Y) / UnderCursor.Image.Height;
                var scale = Math.Max(scaleX, scaleY);

                r.TopLeft = r.BottomRight - UnderCursor.Image.Size * scale;
                UnderCursor.Center = r.Center;
                UnderCursor.Scale = scale;

                break;
            }

            case HitZone.TopRight:
            {
                var scaleX = (boardLocation.X - r.Left) / UnderCursor.Image.Width;
                var scaleY = (r.Bottom - boardLocation.Y) / UnderCursor.Image.Height;
                var scale = Math.Max(scaleX, scaleY);

                r.TopRight = new PointF(r.Left + UnderCursor.Image.Width * scale, r.Bottom - UnderCursor.Image.Height * scale);
                UnderCursor.Center = r.Center;
                UnderCursor.Scale = scale;

                break;
            }

            case HitZone.BottomRight:
            {
                var scaleX = (boardLocation.X - r.Left) / UnderCursor.Image.Width;
                var scaleY = (boardLocation.Y - r.Top) / UnderCursor.Image.Height;
                var scale = Math.Max(scaleX, scaleY);

                r.BottomRight = r.TopLeft + UnderCursor.Image.Size * scale;
                UnderCursor.Center = r.Center;
                UnderCursor.Scale = scale;

                break;
            }

            case HitZone.BottomLeft:
            {
                var scaleX = (r.Right - boardLocation.X) / UnderCursor.Image.Width;
                var scaleY = (boardLocation.Y - r.Top) / UnderCursor.Image.Height;
                var scale = Math.Max(scaleX, scaleY);

                r.BottomLeft = new PointF(r.Right - UnderCursor.Image.Width * scale, r.Top + UnderCursor.Image.Height * scale);
                UnderCursor.Center = r.Center;
                UnderCursor.Scale = scale;

                break;
            }
        }
    }

    private void DrawPin(Pin pin, Graphics g)
    {
        var r = _viewModel.BoardToView(pin.Bounds);
        g.DrawImage(pin.Image.Source, pin.Image.SourceRect, r);

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
        return UnderCursor != null;
    }

    private void CropExecute(object? sender, EventArgs e)
    {
        _newEditMode.OnNext(new CropEditMode(_viewModel, UnderCursor, _settings, this));
    }
}
