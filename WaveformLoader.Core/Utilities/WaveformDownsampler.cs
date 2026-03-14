using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Utilities;

public static class WaveformDownsampler
{
    public static DownsampledWaveform DownsampleForVisibleRange(
        double[] xValues,
        double[] yValues,
        AxisRange visibleRange,
        int pixelWidth)
    {
        ArgumentNullException.ThrowIfNull(xValues);
        ArgumentNullException.ThrowIfNull(yValues);

        if (xValues.Length != yValues.Length)
        {
            throw new ArgumentException("X and Y arrays must have the same length.");
        }

        if (xValues.Length == 0)
        {
            return new DownsampledWaveform([], []);
        }

        if (pixelWidth <= 0 || xValues.Length <= pixelWidth * 2 || !visibleRange.IsValid)
        {
            return new DownsampledWaveform(xValues.ToArray(), yValues.ToArray());
        }

        var bucketCount = Math.Max(1, pixelWidth);
        var bucketWidth = (visibleRange.Max - visibleRange.Min) / bucketCount;

        if (bucketWidth <= 0 || double.IsInfinity(bucketWidth))
        {
            return new DownsampledWaveform(xValues.ToArray(), yValues.ToArray());
        }

        var reducedX = new List<double>(bucketCount * 2);
        var reducedY = new List<double>(bucketCount * 2);

        for (var bucketIndex = 0; bucketIndex < bucketCount; bucketIndex++)
        {
            var bucketMinX = visibleRange.Min + (bucketIndex * bucketWidth);
            var bucketMaxX = bucketIndex == bucketCount - 1
                ? visibleRange.Max
                : bucketMinX + bucketWidth;

            var hasPoint = false;
            var minY = double.PositiveInfinity;
            var maxY = double.NegativeInfinity;
            var minX = 0d;
            var maxX = 0d;
            var minSourceIndex = -1;
            var maxSourceIndex = -1;

            for (var sampleIndex = 0; sampleIndex < xValues.Length; sampleIndex++)
            {
                var sampleX = xValues[sampleIndex];

                if (sampleX < bucketMinX || sampleX > bucketMaxX)
                {
                    continue;
                }

                hasPoint = true;
                var sampleY = yValues[sampleIndex];

                if (sampleY < minY)
                {
                    minY = sampleY;
                    minX = sampleX;
                    minSourceIndex = sampleIndex;
                }

                if (sampleY > maxY)
                {
                    maxY = sampleY;
                    maxX = sampleX;
                    maxSourceIndex = sampleIndex;
                }
            }

            if (!hasPoint)
            {
                continue;
            }

            if (minSourceIndex == maxSourceIndex)
            {
                reducedX.Add(minX);
                reducedY.Add(minY);
                continue;
            }

            if (minSourceIndex < maxSourceIndex)
            {
                reducedX.Add(minX);
                reducedY.Add(minY);
                reducedX.Add(maxX);
                reducedY.Add(maxY);
            }
            else
            {
                reducedX.Add(maxX);
                reducedY.Add(maxY);
                reducedX.Add(minX);
                reducedY.Add(minY);
            }
        }

        return reducedX.Count == 0
            ? new DownsampledWaveform([], [])
            : new DownsampledWaveform(reducedX.ToArray(), reducedY.ToArray());
    }
}
