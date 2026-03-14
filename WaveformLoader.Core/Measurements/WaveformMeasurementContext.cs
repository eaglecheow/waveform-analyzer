using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Measurements;

public readonly record struct WaveformMeasurementContext
{
    public WaveformMeasurementContext(WaveformSeries series, AxisRange? range = null)
    {
        Series = series ?? throw new ArgumentNullException(nameof(series));
        Range = range;
    }

    public WaveformSeries Series { get; }

    public AxisRange? Range { get; }
}
