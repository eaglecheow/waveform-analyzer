using WaveformLoader.Core.Measurements;
using WaveformLoader.Core.Models;

namespace WaveformLoader.Tests;

public sealed class PeakToPeakMeasurementTests
{
    private readonly PeakToPeakMeasurement _measurement = new();

    [Fact]
    public void Evaluate_UsesImplicitXWaveform()
    {
        var series = new WaveformSeries("file", "/y", null, 4, [2.0, -1.0, 7.5, 3.0], null, "Sample Index", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series));

        Assert.True(result.IsAvailable);
        Assert.Equal(8.5, result.NumericValue);
        Assert.Collection(
            result.Markers,
            marker =>
            {
                Assert.Equal("Max", marker.Label);
                Assert.Equal(2, marker.SampleIndex);
                Assert.Equal(2d, marker.X);
                Assert.Equal(7.5, marker.Y);
                Assert.Equal(WaveformMeasurementMarkerKind.Maximum, marker.Kind);
            },
            marker =>
            {
                Assert.Equal("Min", marker.Label);
                Assert.Equal(1, marker.SampleIndex);
                Assert.Equal(1d, marker.X);
                Assert.Equal(-1.0, marker.Y);
                Assert.Equal(WaveformMeasurementMarkerKind.Minimum, marker.Kind);
            });
    }

    [Fact]
    public void Evaluate_UsesExplicitXWaveform()
    {
        var series = new WaveformSeries("file", "/y", "/x", 4, [1.0, 5.0, -3.0, 4.0], [0.1, 0.2, 0.4, 0.8], "Time", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series));

        Assert.True(result.IsAvailable);
        Assert.Equal(8.0, result.NumericValue);
        Assert.Equal(0.2, result.Markers[0].X);
        Assert.Equal(0.4, result.Markers[1].X);
    }

    [Fact]
    public void Evaluate_ReturnsMarkersForOriginalSampleLocations()
    {
        var series = new WaveformSeries("file", "/y", "/x", 5, [4.0, 2.0, 8.0, -2.5, 5.0], [10.0, 11.0, 12.0, 13.0, 14.0], "Time", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series));

        Assert.Collection(
            result.Markers,
            marker =>
            {
                Assert.Equal(2, marker.SampleIndex);
                Assert.Equal(12.0, marker.X);
                Assert.Equal(8.0, marker.Y);
            },
            marker =>
            {
                Assert.Equal(3, marker.SampleIndex);
                Assert.Equal(13.0, marker.X);
                Assert.Equal(-2.5, marker.Y);
            });
    }

    [Fact]
    public void Evaluate_ReturnsZeroForConstantWaveform()
    {
        var series = new WaveformSeries("file", "/y", null, 3, [4.2, 4.2, 4.2], null, "Sample Index", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series));

        Assert.True(result.IsAvailable);
        Assert.Equal(0.0, result.NumericValue);
        Assert.Equal(2, result.Markers.Count);
        Assert.All(result.Markers, marker => Assert.Equal(0, marker.SampleIndex));
    }

    [Fact]
    public void Evaluate_ReturnsUnavailableWhenRangeContainsNoSamples()
    {
        var series = new WaveformSeries("file", "/y", "/x", 3, [1.0, 4.0, 2.0], [10.0, 20.0, 30.0], "Time", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series, new AxisRange(40.0, 50.0)));

        Assert.False(result.IsAvailable);
        Assert.Null(result.NumericValue);
        Assert.Empty(result.Markers);
    }
}
