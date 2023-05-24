using System.Text.Json;
using Eto;
using Eto.Drawing;
using Xunit;
using Xunit.Abstractions;

namespace PinBoard.Tests;

public class Tests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        if (Platform.Instance == null)
            Platform.Initialize(Platform.Detect);
    }

    [Fact]
    public void Test()
    {
        var m = Matrix.Create();
        _testOutputHelper.WriteLine(JsonSerializer.Serialize(m.Elements));
        m.Translate(3, 3);
        m.Scale(2);
        _testOutputHelper.WriteLine(JsonSerializer.Serialize(m.Elements));
        var p = m.TransformPoint(new PointF(5, 5));
        _testOutputHelper.WriteLine($"{p.X} {p.Y}");
    }
}
