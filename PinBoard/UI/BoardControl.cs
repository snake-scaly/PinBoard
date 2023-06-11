using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eto.Drawing;
using Eto.Forms;

namespace PinBoard.UI;

public class BoardControl : IDisposable
{
    private readonly Subject<Unit> _invalidated = new();
    private bool _disposed;

    ~BoardControl()
    {
        Dispose(false);
    }

    public IObservable<Unit> Invalidated => _invalidated.AsObservable();
    public PointF Location { get; set; }
    public SizeF Size { get; set; }
    public RectangleF Bounds => new RectangleF(Location, Size);

    public virtual void OnMouseDown(MouseEventArgs e) {}
    public virtual void OnMouseUp(MouseEventArgs e) {}
    public virtual void OnMouseMove(MouseEventArgs e) {}
    public virtual void OnMouseEnter(MouseEventArgs e) {}
    public virtual void OnMouseLeave(MouseEventArgs e) {}
    public virtual void OnPaint(PaintEventArgs e) {}

    public void Invalidate()
    {
        _invalidated.OnNext(default);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            _invalidated.Dispose();
    }
}
