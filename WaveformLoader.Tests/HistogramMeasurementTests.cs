using WaveformLoader.Core.Measurements;
using WaveformLoader.Core.Models;

namespace WaveformLoader.Tests;

public sealed class HistogramMeasurementTests
{
    private readonly HistogramMeasurement _measurement = new();

    [Fact]
    public void Evaluate_HorizontalHistogramUsesYValues()
    {
        var series = new WaveformSeries("file", "/y", "/x", 3, [10.0, 20.0, 30.0], [100.0, 200.0, 300.0], "Time", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series));

        Assert.True(result.IsAvailable);
        Assert.Null(result.NumericValue);
        Assert.NotNull(result.HistogramData);
        Assert.NotNull(result.HistogramStatistics);
        Assert.Equal(WaveformHistogramOrientation.Horizontal, result.HistogramData!.Orientation);
        Assert.Equal(10.0, result.HistogramStatistics!.Min);
        Assert.Equal(30.0, result.HistogramStatistics.Max);
        Assert.Equal(20.0, result.HistogramStatistics.Mean);
        Assert.Equal(128, result.HistogramData.Counts.Length);
        Assert.Equal(129, result.HistogramData.BinEdges.Length);
        Assert.Equal(128, result.HistogramData.BinCenters.Length);
        Assert.Equal(3, result.HistogramData.Counts.Sum());
    }

    [Fact]
    public void Evaluate_VerticalHistogramUsesExplicitXValues()
    {
        var series = new WaveformSeries("file", "/y", "/x", 3, [10.0, 20.0, 30.0], [100.0, 200.0, 300.0], "Time", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series, histogramOrientation: WaveformHistogramOrientation.Vertical));

        Assert.True(result.IsAvailable);
        Assert.NotNull(result.HistogramStatistics);
        Assert.Equal(WaveformHistogramOrientation.Vertical, result.HistogramData!.Orientation);
        Assert.Equal(100.0, result.HistogramStatistics!.Min);
        Assert.Equal(300.0, result.HistogramStatistics.Max);
        Assert.Equal(200.0, result.HistogramStatistics.Mean);
    }

    [Fact]
    public void Evaluate_VerticalHistogramUsesImplicitSampleIndexValues()
    {
        var series = new WaveformSeries("file", "/y", null, 4, [5.0, 6.0, 7.0, 8.0], null, "Sample Index", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series, histogramOrientation: WaveformHistogramOrientation.Vertical));

        Assert.True(result.IsAvailable);
        Assert.NotNull(result.HistogramStatistics);
        Assert.Equal(0.0, result.HistogramStatistics!.Min);
        Assert.Equal(3.0, result.HistogramStatistics.Max);
        Assert.Equal(1.5, result.HistogramStatistics.Mean);
    }

    [Fact]
    public void Evaluate_VisibleRangeFiltersIncludedSamples()
    {
        var series = new WaveformSeries("file", "/y", "/x", 4, [10.0, 20.0, 30.0, 40.0], [0.0, 1.0, 2.0, 3.0], "Time", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(
            new WaveformMeasurementContext(
                series,
                new AxisRange(1.5, 3.0),
                WaveformHistogramOrientation.Horizontal,
                128));

        Assert.True(result.IsAvailable);
        Assert.NotNull(result.HistogramStatistics);
        Assert.Equal(2, result.HistogramStatistics!.SampleCount);
        Assert.Equal(30.0, result.HistogramStatistics.Min);
        Assert.Equal(40.0, result.HistogramStatistics.Max);
        Assert.Equal(35.0, result.HistogramStatistics.Mean);
    }

    [Fact]
    public void Evaluate_ReturnsUnavailableWhenRangeContainsNoSamples()
    {
        var series = new WaveformSeries("file", "/y", "/x", 3, [10.0, 20.0, 30.0], [0.0, 1.0, 2.0], "Time", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series, new AxisRange(10.0, 20.0)));

        Assert.False(result.IsAvailable);
        Assert.Null(result.HistogramData);
        Assert.Null(result.HistogramStatistics);
    }

    [Fact]
    public void Evaluate_ReturnsZeroVarianceAndStandardDeviationForSingleSample()
    {
        var series = new WaveformSeries("file", "/y", "/x", 3, [10.0, 20.0, 30.0], [0.0, 2.0, 4.0], "Time", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series, new AxisRange(1.5, 2.5)));

        Assert.True(result.IsAvailable);
        Assert.NotNull(result.HistogramStatistics);
        Assert.Equal(0.0, result.HistogramStatistics!.Variance);
        Assert.Equal(0.0, result.HistogramStatistics.StandardDeviation);
    }

    [Fact]
    public void Evaluate_ModeChoosesLowerBinWhenCountsTie()
    {
        var series = new WaveformSeries("file", "/y", null, 4, [0.0, 0.0, 1.0, 1.0], null, "Sample Index", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series, histogramBinCount: 2));

        Assert.True(result.IsAvailable);
        Assert.NotNull(result.HistogramStatistics);
        Assert.Equal(0.25, result.HistogramStatistics!.Mode, 10);
    }

    [Fact]
    public void Evaluate_PopulatesDefaultBinPayload()
    {
        var series = new WaveformSeries("file", "/y", null, 5, [1.0, 3.0, 5.0, 7.0, 9.0], null, "Sample Index", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series));

        Assert.True(result.IsAvailable);
        Assert.NotNull(result.HistogramData);
        Assert.Equal(128, result.HistogramData!.Counts.Length);
        Assert.Equal(129, result.HistogramData.BinEdges.Length);
        Assert.Equal(128, result.HistogramData.BinCenters.Length);
        Assert.Equal(5, result.HistogramData.Counts.Sum());
    }

    [Fact]
    public void Evaluate_UsesRequestedBinCount()
    {
        var series = new WaveformSeries("file", "/y", null, 5, [1.0, 3.0, 5.0, 7.0, 9.0], null, "Sample Index", "Amplitude", Array.Empty<string>());

        var result = _measurement.Evaluate(new WaveformMeasurementContext(series, histogramBinCount: 256));

        Assert.True(result.IsAvailable);
        Assert.NotNull(result.HistogramData);
        Assert.Equal(256, result.HistogramData!.Counts.Length);
        Assert.Equal(257, result.HistogramData.BinEdges.Length);
        Assert.Equal(256, result.HistogramData.BinCenters.Length);
    }
}
