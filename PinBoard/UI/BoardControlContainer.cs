using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Eto.Forms;
using PinBoard.Models;
using PinBoard.Util;
using ReactiveUI;

namespace PinBoard.UI;

public class BoardControlContainer : Drawable
{
    private readonly SourceList<BoardControl> _controls = new();
    private readonly CompositeDisposable _disposables = new();
    private readonly ReactiveValue<BoardControl?> _controlUnderCursor = new();
    private readonly Subject<Unit> _misclickSubject = new();

    private bool _mouseCaptured;
    private bool _insideCaptured;

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

        _controlUnderCursor.WhenAnyValue(x => x.Value)
            .Select(x => x?.Cursor)
            .Merge(_controlUnderCursor.WhenAnyValue(x => x.Value!.Cursor))
            .DistinctUntilChanged()
            .BindTo(this, x => x.Cursor)
            .DisposeWith(_disposables);
    }

    public SourceList<BoardControl> BoardControls => _controls;
    public IObservable<Unit> Misclick => _misclickSubject.AsObservable();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _disposables.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _controlUnderCursor.Value?.OnMouseDown(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
        if (_controlUnderCursor.Value != null)
        {
            _mouseCaptured = true;
            _insideCaptured = true;
        }
        else if (e.Buttons == MouseButtons.Primary)
        {
            _misclickSubject.OnNext(Unit.Default);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _mouseCaptured = false;
        _controlUnderCursor.Value?.OnMouseUp(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
        if (TrackControlUnderCursor(e))
            _controlUnderCursor.Value?.OnMouseMove(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_mouseCaptured)
        {
            var inside = _controlUnderCursor.Value!.Bounds.Contains(e.Location);
            if (inside != _insideCaptured)
            {
                if (inside)
                    _controlUnderCursor.Value.OnMouseEnter(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
                else
                    _controlUnderCursor.Value.OnMouseLeave(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
                _insideCaptured = inside;
            }
        }
        else
        {
            TrackControlUnderCursor(e);
        }

        _controlUnderCursor.Value?.OnMouseMove(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        if (_mouseCaptured)
            _controlUnderCursor.Value?.OnMouseEnter(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
        else
            TrackControlUnderCursor(e);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _controlUnderCursor.Value?.OnMouseLeave(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
        if (!_mouseCaptured)
            _controlUnderCursor.Value = null;
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

    private bool TrackControlUnderCursor(MouseEventArgs e)
    {
        foreach (var control in _controls.Items.Reverse())
        {
            if (control.Bounds.Contains(e.Location))
            {
                if (control != _controlUnderCursor.Value)
                {
                    _controlUnderCursor.Value?.OnMouseLeave(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
                    _controlUnderCursor.Value = control;
                    control.OnMouseEnter(TranslateMouseEventArgs(e, control));
                    return true;
                }

                return false;
            }
        }

        _controlUnderCursor.Value?.OnMouseLeave(TranslateMouseEventArgs(e, _controlUnderCursor.Value));
        _controlUnderCursor.Value = null;
        return false;
    }

    private MouseEventArgs TranslateMouseEventArgs(MouseEventArgs e, BoardControl control)
    {
        return new MouseEventArgs(e.Buttons, e.Modifiers, e.Location - control.Location, e.Delta, e.Pressure);
    }
}
