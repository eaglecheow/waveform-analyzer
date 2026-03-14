namespace WaveformLoader.Core.Measurements;

public interface IWaveformMeasurement
{
    string Id { get; }

    string DisplayName { get; }

    WaveformMeasurementResult Evaluate(WaveformMeasurementContext context);
}
