using System.Reactive.Disposables;
using System.Reactive.Linq;
using Eto.Drawing;
using Eto.Forms;

namespace PinBoard;

public sealed class PanZoomController : IDisposable
{
    private static readonly float[] _scaleGrid = { .2f, .3f, .5f, .75f, 1f, 1.5f, 2f, 3f, 5f };

    private readonly PanZoomModel _model;
    private readonly CompositeDisposable _subscriptions = new();

    private bool _pan;
    private PointF _panOffset;

    public PanZoomController(PanZoomModel model, Control control)
    {
        _model = model;
        
        Observable
            .FromEventPattern<MouseEventArgs>(control, nameof(control.MouseDown))
            .Subscribe(x => OnMouseDown(x.EventArgs))
            .DisposeWith(_subscriptions);

        Observable
            .FromEventPattern<MouseEventArgs>(control, nameof(control.MouseUp))
            .Subscribe(x => OnMouseUp(x.EventArgs))
            .DisposeWith(_subscriptions);

        Observable
            .FromEventPattern<MouseEventArgs>(control, nameof(control.MouseMove))
            .Subscribe(x => OnMouseMove(x.EventArgs))
            .DisposeWith(_subscriptions);

        Observable
            .FromEventPattern<MouseEventArgs>(control, nameof(control.MouseWheel))
            .Subscribe(x => OnMouseWheel(x.EventArgs))
            .DisposeWith(_subscriptions);
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }
    
    private void OnMouseDown(MouseEventArgs e)
    {
        if (e.Buttons == MouseButtons.Middle)
        {
            e.Handled = true;
            _panOffset = _model.ViewBoardTransform.TransformPoint(e.Location);
            _pan = true;
        }
    }

    private void OnMouseUp(MouseEventArgs e)
    {
        if (_pan)
        {
            e.Handled = true;
            _pan = false;
        }
    }

    private void OnMouseMove(MouseEventArgs e)
    {
        if (_pan)
        {
            e.Handled = true;
            _model.Pan(_panOffset, e.Location);
        }
    }

    void OnMouseWheel(MouseEventArgs e)
    {
        if (e.Buttons != 0)
            return;

        e.Handled = true;

        var (below, above) = FindClosest(_model.Scale);

        if (e.Delta.Height > 0)
            _model.Zoom(e.Location, above);
        else if (e.Delta.Height < 0)
            _model.Zoom(e.Location, below);
    }

    private static (float below, float above) FindClosest(float s)
    {
        var below = _scaleGrid[0];

        foreach (var g in _scaleGrid.Skip(1))
        {
            if (Math.Abs(s - g) <= s / 100)
                continue; // same scale, ignore
            if (g > s)
                return (below, g);
            below = g;
        }

        return (below, _scaleGrid.Last());
    }
}
