namespace WaveformLoader.Core.Measurements;

public sealed record WaveformMeasurementResult
{
    public WaveformMeasurementResult(
        string id,
        string displayName,
        bool isAvailable,
        double? numericValue,
        string detailText,
        IReadOnlyList<WaveformMeasurementMarker>? markers = null)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("A measurement result must have an identifier.", nameof(id))
            : id;
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? throw new ArgumentException("A measurement result must have a display name.", nameof(displayName))
            : displayName;
        IsAvailable = isAvailable;
        NumericValue = numericValue;
        DetailText = detailText ?? string.Empty;
        Markers = markers ?? Array.Empty<WaveformMeasurementMarker>();
    }

    public string Id { get; }

    public string DisplayName { get; }

    public bool IsAvailable { get; }

    public double? NumericValue { get; }

    public string DetailText { get; }

    public IReadOnlyList<WaveformMeasurementMarker> Markers { get; }
}
