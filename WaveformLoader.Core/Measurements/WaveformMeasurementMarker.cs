namespace WaveformLoader.Core.Measurements;

public sealed record WaveformMeasurementMarker(
    int SampleIndex,
    double X,
    double Y,
    string Label,
    WaveformMeasurementMarkerKind Kind);
