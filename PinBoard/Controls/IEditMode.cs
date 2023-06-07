using System.Reactive;
using Eto.Drawing;
using Eto.Forms;

namespace PinBoard.Controls;

public interface IEditMode : IDisposable
{
    ContextMenu ContextMenu { get; }

    IObservable<Unit> Invalidated { get; }

    IObservable<Cursor> Cursor { get; }
    
    IObservable<PointF> ShowContextMenu { get; }

    IObservable<IEditMode> NewEditMode { get; }

    void Attach(Control owner);

    void Detach(Control owner);

    void OnPaint(PaintEventArgs e);
}
