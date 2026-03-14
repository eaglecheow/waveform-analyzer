using WaveformLoader.Core.Models;
using WaveformLoader.Core.Utilities;

namespace WaveformLoader.Tests;

public sealed class WaveformPointSelectorTests
{
    [Fact]
    public void SelectNearestPoint_UsesImplicitIndexes()
    {
        var series = new WaveformSeries("file", "/y", null, 4, new[] { 0.0, 10.0, 20.0, 30.0 }, null, "Sample Index", "y", Array.Empty<string>());

        var point = WaveformPointSelector.SelectNearestPoint(series, 1.6);

        Assert.Equal(2, point.Index);
        Assert.Equal(2.0, point.X);
        Assert.Equal(20.0, point.Y);
    }

    [Fact]
    public void SelectNearestPoint_UsesExplicitXData()
    {
        var series = new WaveformSeries(
            "file",
            "/y",
            "/x",
            4,
            new[] { 0.0, 5.0, 10.0, 15.0 },
            new[] { 0.0, 0.5, 1.0, 1.5 },
            "x",
            "y",
            Array.Empty<string>());

        var point = WaveformPointSelector.SelectNearestPoint(series, 0.76);

        Assert.Equal(2, point.Index);
        Assert.Equal(1.0, point.X);
        Assert.Equal(10.0, point.Y);
    }
}
