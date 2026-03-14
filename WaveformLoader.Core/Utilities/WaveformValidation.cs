using WaveformLoader.Core.Exceptions;

namespace WaveformLoader.Core.Utilities;

public static class WaveformValidation
{
    public static void Ensure1DAndNonEmpty(string datasetPath, IReadOnlyList<long> dimensions, bool isNumeric)
    {
        if (!isNumeric)
        {
            throw new WaveformLoadException($"Dataset '{datasetPath}' is not numeric and cannot be plotted.");
        }

        if (dimensions.Count != 1)
        {
            throw new WaveformLoadException($"Dataset '{datasetPath}' must be one-dimensional to be plotted.");
        }

        if (dimensions[0] <= 0)
        {
            throw new WaveformLoadException($"Dataset '{datasetPath}' is empty and cannot be plotted.");
        }
    }

    public static void EnsureSameLength(string xDatasetPath, string yDatasetPath, int xLength, int yLength)
    {
        if (xLength != yLength)
        {
            throw new WaveformLoadException(
                $"Datasets '{xDatasetPath}' and '{yDatasetPath}' must have the same length. Found {xLength} and {yLength}.");
        }
    }
}
