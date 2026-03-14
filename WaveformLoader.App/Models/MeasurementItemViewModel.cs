using WaveformLoader.Core.Measurements;

namespace WaveformLoader.App.Models;

public sealed class MeasurementItemViewModel
{
    public MeasurementItemViewModel(WaveformMeasurementResult result)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public WaveformMeasurementResult Result { get; }

    public string Id => Result.Id;

    public string DisplayName => Result.DisplayName;

    public bool IsAvailable => Result.IsAvailable;

    public double? NumericValue => Result.NumericValue;

    public string ValueText => IsAvailable && NumericValue is double value
        ? value.ToString("G8")
        : "Unavailable";

    public string DetailText => string.IsNullOrWhiteSpace(Result.DetailText)
        ? "No additional details."
        : Result.DetailText;

    public IReadOnlyList<WaveformMeasurementMarker> Markers => Result.Markers;
}
