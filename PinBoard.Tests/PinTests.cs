using System;
using System.Reactive.Linq;
using System.Text.Json;
using DynamicData;
using Eto;
using Eto.Drawing;
using Xunit;
using Xunit.Abstractions;

namespace PinBoard.Tests;

public class PinTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public PinTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        if (Platform.Instance == null)
            Platform.Initialize(new Eto.GtkSharp.Platform());
    }

    [Fact]
    public void Test1()
    {
        var pic = new Pin();
        pic.PropertyChanged += (s, e) => _testOutputHelper.WriteLine($"{JsonSerializer.Serialize(e)}");
        pic.Update.Subscribe(_ => _testOutputHelper.WriteLine("Update"));
        pic.Image = new CroppedImage(new Bitmap(16, 16, PixelFormat.Format24bppRgb));
        pic.Center = new PointF(1, 1);
        pic.Scale = 2;
    }

    [Fact]
    public void Test2()
    {
        var list = new SourceList<Pin>();
        var changes = list
            .Connect()
            .Publish();
        var upd = changes.MergeMany(x => x.Update);
        var rem = changes.Where(x => x.Removes > 0).Select(_ => true);
        upd.Merge(rem).Subscribe(_ => _testOutputHelper.WriteLine(" : Update"));
        changes.Connect();

        var p1 = new Pin();
        var p2 = new Pin();
        var p3 = new Pin();

        var b = new Bitmap(16, 16, PixelFormat.Format24bppRgb);

        _testOutputHelper.WriteLine("p1");
        list.Add(p1);
        p1.Image = new CroppedImage(b);
        _testOutputHelper.WriteLine("p2");
        list.Add(p2);
        p2.Image = new CroppedImage(b);
        _testOutputHelper.WriteLine("Mod p1");
        p1.Center = new PointF(1, 1);
        _testOutputHelper.WriteLine("p3");
        list.Add(p3);
        p3.Image = new CroppedImage(b);
        _testOutputHelper.WriteLine("Remove p1");
        list.Remove(p1);
        _testOutputHelper.WriteLine("Mod p1");
        p1.Scale = 2;
        _testOutputHelper.WriteLine("Mod p2");
        p2.Scale = 2;
        _testOutputHelper.WriteLine("Mod p3");
        p3.Scale = 2;
    }
}
