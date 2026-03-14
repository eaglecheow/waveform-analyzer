using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Measurements;

public sealed class PeakToPeakMeasurement : IWaveformMeasurement
{
    public const string MeasurementId = "peak-to-peak";

    public string Id => MeasurementId;

    public string DisplayName => "Peak-to-peak";

    public WaveformMeasurementResult Evaluate(WaveformMeasurementContext context)
    {
        var series = context.Series;

        if (context.Range is { } invalidRange && !invalidRange.IsValid)
        {
            return CreateUnavailableResult();
        }

        var minimumIndex = -1;
        var maximumIndex = -1;
        var minimumValue = 0d;
        var maximumValue = 0d;

        for (var index = 0; index < series.SampleCount; index++)
        {
            if (!IsIncluded(series, context.Range, index))
            {
                continue;
            }

            var sample = series.YValues[index];

            if (minimumIndex < 0 || sample < minimumValue)
            {
                minimumIndex = index;
                minimumValue = sample;
            }

            if (maximumIndex < 0 || sample > maximumValue)
            {
                maximumIndex = index;
                maximumValue = sample;
            }
        }

        if (minimumIndex < 0 || maximumIndex < 0)
        {
            return CreateUnavailableResult();
        }

        var peakToPeak = maximumValue - minimumValue;
        var minimumX = series.GetXValue(minimumIndex);
        var maximumX = series.GetXValue(maximumIndex);

        return new WaveformMeasurementResult(
            Id,
            DisplayName,
            isAvailable: true,
            numericValue: peakToPeak,
            detailText: BuildDetailText(minimumValue, minimumIndex, minimumX, maximumValue, maximumIndex, maximumX),
            markers:
            [
                new WaveformMeasurementMarker(maximumIndex, maximumX, maximumValue, "Max", WaveformMeasurementMarkerKind.Maximum),
                new WaveformMeasurementMarker(minimumIndex, minimumX, minimumValue, "Min", WaveformMeasurementMarkerKind.Minimum)
            ]);
    }

    private static bool IsIncluded(WaveformSeries series, AxisRange? range, int index)
    {
        if (range is null)
        {
            return true;
        }

        var x = series.GetXValue(index);
        return x >= range.Value.Min && x <= range.Value.Max;
    }

    private static WaveformMeasurementResult CreateUnavailableResult() =>
        new(
            MeasurementId,
            "Peak-to-peak",
            isAvailable: false,
            numericValue: null,
            detailText: "No samples fall inside the requested range.");

    private static string BuildDetailText(
        double minimumValue,
        int minimumIndex,
        double minimumX,
        double maximumValue,
        int maximumIndex,
        double maximumX) =>
        $"Min: {minimumValue:G8} @ index {minimumIndex:N0} (X: {minimumX:G8})   Max: {maximumValue:G8} @ index {maximumIndex:N0} (X: {maximumX:G8})";
}
