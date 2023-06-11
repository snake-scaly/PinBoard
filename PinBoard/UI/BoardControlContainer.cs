using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Eto.Forms;
using PinBoard.Models;

namespace PinBoard.UI;

public class BoardControlContainer : Drawable
{
    private readonly SourceList<BoardControl> _controls = new();
    private readonly CompositeDisposable _disposables = new();

    private BoardControl? _controlUnderCursor;
    private bool _mouseCaptured;

    public BoardControlContainer(Settings settings)
    {
        BackgroundColor = settings.BackgroundColor;

        _controls.DisposeWith(_disposables);

        var controlListChanges = _controls.Connect()
            .Publish();
        controlListChanges.Connect();

        controlListChanges.MergeMany(x => x.Invalidated)
            .Merge(controlListChanges.Select(_ => Unit.Default))
            .Subscribe(_ => Invalidate())
            .DisposeWith(_disposables);
    }

    public SourceList<BoardControl> BoardControls => _controls;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _disposables.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        TrackControlUnderCursor(e);
        _controlUnderCursor?.OnMouseDown(TranslateMouseEventArgs(e, _controlUnderCursor));
        _mouseCaptured = true;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _mouseCaptured = false;
        _controlUnderCursor?.OnMouseUp(TranslateMouseEventArgs(e, _controlUnderCursor));
        TrackControlUnderCursor(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        TrackControlUnderCursor(e);
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        TrackControlUnderCursor(e);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (_mouseCaptured)
        {
            _mouseCaptured = false;
            _controlUnderCursor?.OnMouseUp(TranslateMouseEventArgs(e, _controlUnderCursor));
        }

        _controlUnderCursor?.OnMouseLeave(TranslateMouseEventArgs(e, _controlUnderCursor));
        _controlUnderCursor = null;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        foreach (var control in _controls.Items)
        {
            using var saveTransform = e.Graphics.SaveTransformState();
            e.Graphics.TranslateTransform(control.Location);
            control.OnPaint(new PaintEventArgs(e.Graphics, e.Graphics.ClipBounds));
        }
    }

    private void TrackControlUnderCursor(MouseEventArgs e)
    {
        if (_mouseCaptured)
        {
            _controlUnderCursor?.OnMouseMove(TranslateMouseEventArgs(e, _controlUnderCursor));
            return;
        }

        foreach (var control in _controls.Items.Reverse())
        {
            if (control.Bounds.Contains(e.Location))
            {
                if (control == _controlUnderCursor)
                {
                    control.OnMouseMove(TranslateMouseEventArgs(e, control));
                }
                else
                {
                    _controlUnderCursor?.OnMouseLeave(TranslateMouseEventArgs(e, _controlUnderCursor));
                    _controlUnderCursor = control;
                    control.OnMouseEnter(TranslateMouseEventArgs(e, control));
                    control.OnMouseMove(TranslateMouseEventArgs(e, control));
                }

                return;
            }
        }

        _controlUnderCursor?.OnMouseLeave(TranslateMouseEventArgs(e, _controlUnderCursor));
        _controlUnderCursor = null;
    }

    private MouseEventArgs TranslateMouseEventArgs(MouseEventArgs e, BoardControl control)
    {
        return new MouseEventArgs(e.Buttons, e.Modifiers, e.Location - control.Location, e.Delta, e.Pressure);
    }
}
