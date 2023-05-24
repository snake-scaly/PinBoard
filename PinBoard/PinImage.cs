using Eto.Drawing;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace PinBoard;

public class PinImage : ReactiveObject
{
    public PinImage(Image source, bool isIcon)
    {
        Source = source;
        IsIcon = isIcon;
        SourceRect = new RectangleF(default, source.Size);
        this.WhenAnyValue(x => x.SourceRect.Width).ToPropertyEx(this, x => x.Width);
        this.WhenAnyValue(x => x.SourceRect.Height).ToPropertyEx(this, x => x.Height);
        this.WhenAnyValue(x => x.SourceRect.Size).ToPropertyEx(this, x => x.Size);
    }

    public Image Source { get; }

    public bool IsIcon { get; }

    [Reactive]
    public RectangleF SourceRect { get; set; }

    [ObservableAsProperty]
    public float Width { get; }

    [ObservableAsProperty]
    public float Height { get; }
    
    [ObservableAsProperty]
    public SizeF Size { get; }

    public RectangleF GetViewRect(IMatrix viewTransform)
    {
        if (IsIcon)
        {
            return new RectangleF(Source.Size) { Center = viewTransform.TransformPoint(default) };
        }

        return viewTransform.TransformRectangle(CenterRect(SourceRect.Size));
    }

    public void Render(Graphics g, IMatrix imageViewTransform)
    {
        if (IsIcon)
            g.DrawImage(Source, imageViewTransform.TransformPoint(default) - Source.Size / 2);
        else
            g.DrawImage(Source, SourceRect, imageViewTransform.TransformRectangle(CenterRect(SourceRect.Size)));
    }

    private static RectangleF CenterRect(SizeF size) => new(-size.Width / 2, -size.Height / 2, size.Width, size.Height);
}
