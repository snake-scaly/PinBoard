using Eto.Drawing;
using Eto.Forms;

namespace PinBoard;

public interface IEditMode : IDisposable
{
    ContextMenu ContextMenu { get; }

    IObservable<bool> Invalidated { get; }

    IObservable<Cursor> Cursor { get; }
    
    IObservable<PointF> ShowContextMenu { get; }

    void OnMouseDown(MouseEventArgs e);

    void OnMouseUp(MouseEventArgs e);

    void OnMouseMove(MouseEventArgs e);

    void OnPaint(PaintEventArgs e);
}
