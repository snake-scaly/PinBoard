using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Eto.Drawing;
using Eto.Forms;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard.UI;

public class BoardControl : ReactiveObject, IDisposable
{
    private readonly Subject<Unit> _invalidated;
    private bool _disposed;

    public BoardControl()
    {
        _invalidated = new Subject<Unit>().DisposeWith(Disposables);
    }

    ~BoardControl()
    {
        Dispose(false);
    }

    public IObservable<Unit> Invalidated => _invalidated.AsObservable();
    public PointF Location { get; set; }
    public SizeF Size { get; set; }
    public RectangleF Bounds => new(Location, Size);

    [Reactive] public Cursor? Cursor { get; set; }

    protected CompositeDisposable Disposables { get; } = new();

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
            Disposables.Dispose();
    }
}
