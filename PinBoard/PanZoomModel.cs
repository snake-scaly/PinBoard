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

    public PointF ViewToBoard(PointF viewLocation) => viewLocation / Scale + Origin;

    public PointF BoardToView(PointF boardLocation) => (boardLocation - Origin) * Scale;

    public SizeF BoardToView(SizeF boardSize) => boardSize * Scale;

    public RectangleF BoardToView(RectangleF boardRect) => new(BoardToView(boardRect.TopLeft), BoardToView(boardRect.Size));

    /// <summary>
    /// Pan the view such that a point in board coordinates appears at a given point in view coordinates.
    /// </summary>
    public void Pan(PointF boardLocation, PointF viewLocation) => Origin = boardLocation - viewLocation / Scale;

    /// <summary>
    /// Zoom the view such that the given point in view coordinates stays at the same board coordinates.
    /// </summary>
    public void Zoom(PointF viewLocation, float newScale)
    {
        var boardLocation = ViewToBoard(viewLocation);
        Scale = newScale;
        Pan(boardLocation, viewLocation);
    }
}
