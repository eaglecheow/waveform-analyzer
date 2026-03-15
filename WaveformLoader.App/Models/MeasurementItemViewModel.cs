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

    public WaveformHistogramData? HistogramData => Result.HistogramData;

    public WaveformHistogramStatistics? HistogramStatistics => Result.HistogramStatistics;

    public string HistogramMinimumText => FormatHistogramValue(HistogramStatistics?.Min);

    public string HistogramMaximumText => FormatHistogramValue(HistogramStatistics?.Max);

    public string HistogramMeanText => FormatHistogramValue(HistogramStatistics?.Mean);

    public string HistogramMedianText => FormatHistogramValue(HistogramStatistics?.Median);

    public string HistogramModeText => FormatHistogramValue(HistogramStatistics?.Mode);

    public string HistogramVarianceText => FormatHistogramValue(HistogramStatistics?.Variance);

    public string HistogramStandardDeviationText => FormatHistogramValue(HistogramStatistics?.StandardDeviation);

    public string HistogramSampleCountText => HistogramStatistics is { } statistics
        ? statistics.SampleCount.ToString("N0")
        : "0";

    private static string FormatHistogramValue(double? value) => value is double numericValue
        ? numericValue.ToString("G8")
        : "Unavailable";
}
