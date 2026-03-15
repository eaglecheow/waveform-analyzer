using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Measurements;

public sealed class HistogramMeasurement : IWaveformMeasurement
{
    public const string MeasurementId = "histogram";

    public string Id => MeasurementId;

    public string DisplayName => "Histogram";

    public WaveformMeasurementResult Evaluate(WaveformMeasurementContext context)
    {
        if (context.Range is { } invalidRange && !invalidRange.IsValid)
        {
            return CreateUnavailableResult();
        }

        var values = GetIncludedValues(context.Series, context.Range, context.HistogramOrientation);

        if (values.Count == 0)
        {
            return CreateUnavailableResult();
        }

        var histogramData = BuildHistogram(values, context.HistogramOrientation, context.HistogramBinCount);
        var statistics = BuildStatistics(values, histogramData);
        var axisLabel = context.HistogramOrientation == WaveformHistogramOrientation.Horizontal
            ? context.Series.YAxisLabel
            : context.Series.XAxisLabel;
        var scopeLabel = context.Range is null ? "whole waveform" : "visible range";

        return new WaveformMeasurementResult(
            Id,
            DisplayName,
            isAvailable: true,
            numericValue: null,
            detailText: $"Axis: {axisLabel}   Samples: {statistics.SampleCount:N0}   Bins: {context.HistogramBinCount:N0}   Scope: {scopeLabel}",
            histogramData: histogramData,
            histogramStatistics: statistics);
    }

    private static List<double> GetIncludedValues(
        WaveformSeries series,
        AxisRange? range,
        WaveformHistogramOrientation orientation)
    {
        var values = new List<double>(series.SampleCount);

        for (var index = 0; index < series.SampleCount; index++)
        {
            if (!IsIncluded(series, range, index))
            {
                continue;
            }

            values.Add(orientation == WaveformHistogramOrientation.Horizontal
                ? series.YValues[index]
                : series.GetXValue(index));
        }

        return values;
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

    private static WaveformHistogramData BuildHistogram(
        IReadOnlyList<double> values,
        WaveformHistogramOrientation orientation,
        int binCount)
    {
        if (binCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(binCount), "Histogram bin count must be positive.");
        }

        var minimum = values.Min();
        var maximum = values.Max();
        var binEdges = new double[binCount + 1];
        var binCenters = new double[binCount];
        var counts = new int[binCount];

        if (maximum <= minimum)
        {
            var occupiedIndex = Math.Max(0, (binCount / 2) - 1);
            var binWidth = Math.Abs(minimum) < 1d ? 1d / binCount : Math.Abs(minimum) / binCount;
            var firstEdge = minimum - ((occupiedIndex + 0.5d) * binWidth);

            for (var index = 0; index < binEdges.Length; index++)
            {
                binEdges[index] = firstEdge + (index * binWidth);
            }

            for (var index = 0; index < binCenters.Length; index++)
            {
                binCenters[index] = (binEdges[index] + binEdges[index + 1]) / 2d;
            }

            counts[occupiedIndex] = values.Count;
            return new WaveformHistogramData(orientation, binEdges, binCenters, counts);
        }

        var binWidthForRange = (maximum - minimum) / binCount;

        for (var index = 0; index < binEdges.Length; index++)
        {
            binEdges[index] = minimum + (index * binWidthForRange);
        }

        for (var index = 0; index < binCenters.Length; index++)
        {
            binCenters[index] = (binEdges[index] + binEdges[index + 1]) / 2d;
        }

        foreach (var value in values)
        {
            var binIndex = value >= maximum
                ? binCount - 1
                : (int)Math.Floor((value - minimum) / binWidthForRange);
            binIndex = Math.Clamp(binIndex, 0, binCount - 1);
            counts[binIndex]++;
        }

        return new WaveformHistogramData(orientation, binEdges, binCenters, counts);
    }

    private static WaveformHistogramStatistics BuildStatistics(
        IReadOnlyList<double> values,
        WaveformHistogramData histogramData)
    {
        var orderedValues = values.OrderBy(value => value).ToArray();
        var minimum = orderedValues[0];
        var maximum = orderedValues[^1];
        var mean = values.Average();
        var median = orderedValues.Length % 2 == 0
            ? (orderedValues[(orderedValues.Length / 2) - 1] + orderedValues[orderedValues.Length / 2]) / 2d
            : orderedValues[orderedValues.Length / 2];

        var variance = values.Sum(value =>
        {
            var delta = value - mean;
            return delta * delta;
        }) / values.Count;

        var modeIndex = 0;
        var modeCount = histogramData.Counts[0];

        for (var index = 1; index < histogramData.Counts.Length; index++)
        {
            if (histogramData.Counts[index] > modeCount)
            {
                modeIndex = index;
                modeCount = histogramData.Counts[index];
            }
        }

        return new WaveformHistogramStatistics(
            minimum,
            maximum,
            mean,
            median,
            histogramData.BinCenters[modeIndex],
            variance,
            Math.Sqrt(variance),
            values.Count);
    }

    private static WaveformMeasurementResult CreateUnavailableResult() =>
        new(
            MeasurementId,
            "Histogram",
            isAvailable: false,
            numericValue: null,
            detailText: "No samples fall inside the requested range.");
}
