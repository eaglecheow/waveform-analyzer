using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Measurements;

public readonly record struct WaveformMeasurementContext
{
    public WaveformMeasurementContext(
        WaveformSeries series,
        AxisRange? range = null,
        WaveformHistogramOrientation histogramOrientation = WaveformHistogramOrientation.Horizontal,
        int histogramBinCount = 128)
    {
        Series = series ?? throw new ArgumentNullException(nameof(series));
        Range = range;
        HistogramOrientation = histogramOrientation;
        HistogramBinCount = histogramBinCount > 0
            ? histogramBinCount
            : throw new ArgumentOutOfRangeException(nameof(histogramBinCount), "Histogram bin count must be positive.");
    }

    public WaveformSeries Series { get; }

    public AxisRange? Range { get; }

    public WaveformHistogramOrientation HistogramOrientation { get; }

    public int HistogramBinCount { get; }
}
