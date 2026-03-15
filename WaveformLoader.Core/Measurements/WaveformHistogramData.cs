namespace WaveformLoader.Core.Measurements;

public sealed record WaveformHistogramData
{
    public WaveformHistogramData(
        WaveformHistogramOrientation orientation,
        double[] binEdges,
        double[] binCenters,
        int[] counts)
    {
        if (binEdges is null)
        {
            throw new ArgumentNullException(nameof(binEdges));
        }

        if (binCenters is null)
        {
            throw new ArgumentNullException(nameof(binCenters));
        }

        if (counts is null)
        {
            throw new ArgumentNullException(nameof(counts));
        }

        if (binEdges.Length != counts.Length + 1)
        {
            throw new ArgumentException("Histogram bin edges must contain exactly one more value than the bin counts.", nameof(binEdges));
        }

        if (binCenters.Length != counts.Length)
        {
            throw new ArgumentException("Histogram bin centers must contain one value per histogram bin.", nameof(binCenters));
        }

        Orientation = orientation;
        BinEdges = binEdges;
        BinCenters = binCenters;
        Counts = counts;
    }

    public WaveformHistogramOrientation Orientation { get; }

    public double[] BinEdges { get; }

    public double[] BinCenters { get; }

    public int[] Counts { get; }
}
