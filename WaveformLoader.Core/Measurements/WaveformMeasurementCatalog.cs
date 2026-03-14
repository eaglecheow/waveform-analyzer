namespace WaveformLoader.Core.Measurements;

public static class WaveformMeasurementCatalog
{
    public static IReadOnlyList<IWaveformMeasurement> Default { get; } =
    [
        new PeakToPeakMeasurement()
    ];
}
