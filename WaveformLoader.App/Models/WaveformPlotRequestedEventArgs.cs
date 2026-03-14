using WaveformLoader.Core.Models;

namespace WaveformLoader.App.Models;

public sealed class WaveformPlotRequestedEventArgs : EventArgs
{
    public WaveformPlotRequestedEventArgs(WaveformSeries series)
    {
        Series = series;
    }

    public WaveformSeries Series { get; }
}
