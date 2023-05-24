using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard;

public class PanZoomModel : ReactiveObject
{
    [Reactive]
    public PointF Origin { get; set; }

    [Reactive]
    public float Scale { get; set; } = 1.0f;

    /// <summary>
    /// Pan the view such that a point in board coordinates appears at a given point in view coordinates.
    /// </summary>
    public void Pan(PointF boardLocation, PointF viewLocation) => Origin = boardLocation - viewLocation / Scale;

    /// <summary>
    /// Zoom the view such that the given point in view coordinates stays at the same board coordinates.
    /// </summary>
    public void Zoom(PointF viewLocation, float newScale)
    {
        var boardLocation = ViewBoardTransform.TransformPoint(viewLocation);
        Scale = newScale;
        Pan(boardLocation, viewLocation);
    }

    public IMatrix BoardViewTransform
    {
        get
        {
            var m = Matrix.Create();
            m.Scale(Scale);
            m.Translate(-Origin);
            return m;
        }
    }

    public IMatrix ViewBoardTransform => BoardViewTransform.Inverse();
}
