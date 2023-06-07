using System.Reactive.Linq;
using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard.Models;

public class PanZoomModel : ReactiveObject
{
    private IMatrix _matrix = Matrix.Create();

    public PanZoomModel()
    {
        BoardViewTransform = _matrix.Clone();

        this.WhenAnyValue(x => x.BoardViewTransform)
            .Select(x => x.Xx)
            .ToPropertyEx(this, x => x.Scale);

        this.WhenAnyValue(x => x.BoardViewTransform)
            .Select(x => x.Inverse())
            .ToPropertyEx(this, x => x.ViewBoardTransform);
    }

    [Reactive]
    public IMatrix BoardViewTransform { get; private set; }

    [ObservableAsProperty]
    public IMatrix ViewBoardTransform { get; }

    [ObservableAsProperty]
    public float Scale { get; }

    /// <summary>
    /// Pan the view such that a point in board coordinates appears at a given point in view coordinates.
    /// </summary>
    public void Pan(PointF boardLocation, PointF viewLocation)
    {
        var newBoardLocation = ViewBoardTransform.TransformPoint(viewLocation);
        _matrix.Translate(newBoardLocation - boardLocation);
        BoardViewTransform = _matrix.Clone();
    }

    /// <summary>
    /// Zoom the view such that the given point in view coordinates stays at the same board coordinates.
    /// </summary>
    public void Zoom(PointF viewLocation, float newScale)
    {
        var boardLocation = ViewBoardTransform.TransformPoint(viewLocation);
        _matrix.ScaleAt(newScale / Scale, boardLocation);
        BoardViewTransform = _matrix.Clone();
    }
}
