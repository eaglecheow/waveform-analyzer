using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Utilities;

public static class WaveformPointSelector
{
    public static SelectedPointInfo SelectNearestPoint(WaveformSeries series, double xValue)
    {
        ArgumentNullException.ThrowIfNull(series);

        if (series.SampleCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(series), "A waveform must contain at least one sample.");
        }

        var index = series.XValues is null
            ? GetNearestImplicitIndex(xValue, series.SampleCount)
            : GetNearestExplicitIndex(series.XValues, xValue);

        return new SelectedPointInfo(index, series.GetXValue(index), series.YValues[index]);
    }

    private static int GetNearestImplicitIndex(double xValue, int sampleCount)
    {
        var rounded = (int)Math.Round(xValue, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, sampleCount - 1);
    }

    private static int GetNearestExplicitIndex(double[] xValues, double xValue)
    {
        var monotonicDirection = GetMonotonicDirection(xValues);

        return monotonicDirection == 0
            ? FindNearestByScan(xValues, xValue)
            : FindNearestByBinarySearch(xValues, xValue, monotonicDirection > 0);
    }

    private static int GetMonotonicDirection(double[] values)
    {
        var direction = 0;

        for (var i = 1; i < values.Length; i++)
        {
            var delta = values[i] - values[i - 1];

            if (delta == 0)
            {
                continue;
            }

            var currentDirection = delta > 0 ? 1 : -1;

            if (direction == 0)
            {
                direction = currentDirection;
                continue;
            }

            if (direction != currentDirection)
            {
                return 0;
            }
        }

        return direction;
    }

    private static int FindNearestByBinarySearch(double[] values, double target, bool ascending)
    {
        var left = 0;
        var right = values.Length - 1;

        while (left <= right)
        {
            var middle = left + ((right - left) / 2);
            var comparison = values[middle].CompareTo(target);

            if (comparison == 0)
            {
                return middle;
            }

            if ((comparison < 0 && ascending) || (comparison > 0 && !ascending))
            {
                left = middle + 1;
            }
            else
            {
                right = middle - 1;
            }
        }

        if (left <= 0)
        {
            return 0;
        }

        if (left >= values.Length)
        {
            return values.Length - 1;
        }

        var lower = left - 1;
        var upper = left;

        return Math.Abs(values[lower] - target) <= Math.Abs(values[upper] - target)
            ? lower
            : upper;
    }

    private static int FindNearestByScan(double[] values, double target)
    {
        var bestIndex = 0;
        var bestDistance = Math.Abs(values[0] - target);

        for (var i = 1; i < values.Length; i++)
        {
            var distance = Math.Abs(values[i] - target);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
