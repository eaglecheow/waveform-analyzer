using WaveformLoader.Core.Models;
using WaveformLoader.Core.Utilities;

namespace WaveformLoader.Tests;

public sealed class WaveformDownsamplerTests
{
    [Fact]
    public void DownsampleForVisibleRange_PreservesBucketExtremes()
    {
        var xs = Enumerable.Range(0, 100).Select(index => (double)index).ToArray();
        var ys = xs.Select(index => index % 10 == 0 ? 10.0 : (index % 10 == 5 ? -10.0 : index / 10.0)).ToArray();

        var reduced = WaveformDownsampler.DownsampleForVisibleRange(xs, ys, new AxisRange(0, 99), 10);

        Assert.NotEmpty(reduced.XValues);
        Assert.NotEmpty(reduced.YValues);
        Assert.True(reduced.XValues.Length <= 20);
        Assert.Contains(10.0, reduced.YValues);
        Assert.Contains(-10.0, reduced.YValues);
    }
}
