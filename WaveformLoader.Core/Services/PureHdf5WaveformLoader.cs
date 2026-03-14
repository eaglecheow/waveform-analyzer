using PureHDF;
using WaveformLoader.Core.Exceptions;
using WaveformLoader.Core.Interfaces;
using WaveformLoader.Core.Models;
using WaveformLoader.Core.Utilities;

namespace WaveformLoader.Core.Services;

public sealed class PureHdf5WaveformLoader : IWaveformLoader
{
    private const int ExplicitXYWarningThreshold = 500_000;
    private const int ExplicitXYHardLimit = 5_000_000;

    public Task<WaveformSeries> LoadAsync(WaveformRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.YDatasetPath);

        return Task.Run(() => LoadCore(request), cancellationToken);
    }

    private static WaveformSeries LoadCore(WaveformRequest request)
    {
        try
        {
            var file = H5File.OpenRead(request.FilePath);
            var yDataset = file.Dataset(request.YDatasetPath);
            var yDimensions = PureHdfValueReader.GetDimensions(yDataset.Space);

            WaveformValidation.Ensure1DAndNonEmpty(request.YDatasetPath, yDimensions, PureHdfValueReader.IsNumeric(yDataset.Type));

            var yValues = PureHdfValueReader.ReadNumericDataset(yDataset);
            double[]? xValues = null;
            var warnings = new List<string>();
            var xAxisLabel = "Sample Index";

            if (!string.IsNullOrWhiteSpace(request.XDatasetPath))
            {
                if (string.Equals(request.XDatasetPath, request.YDatasetPath, StringComparison.Ordinal))
                {
                    throw new WaveformLoadException("The X and Y datasets must be different.");
                }

                var xDataset = file.Dataset(request.XDatasetPath);
                var xDimensions = PureHdfValueReader.GetDimensions(xDataset.Space);

                WaveformValidation.Ensure1DAndNonEmpty(request.XDatasetPath, xDimensions, PureHdfValueReader.IsNumeric(xDataset.Type));
                WaveformValidation.EnsureSameLength(request.XDatasetPath, request.YDatasetPath, checked((int)xDimensions[0]), checked((int)yDimensions[0]));

                if (yValues.Length > ExplicitXYHardLimit)
                {
                    throw new WaveformLoadException(
                        $"The selected X/Y datasets contain {yValues.Length:N0} samples, which exceeds the explicit X/Y limit of {ExplicitXYHardLimit:N0} samples in v1.");
                }

                if (yValues.Length > ExplicitXYWarningThreshold)
                {
                    warnings.Add(
                        $"Large explicit X/Y datasets are fully loaded into memory in v1. Rendering will downsample on-screen, but loading {yValues.Length:N0} points may take longer.");
                }

                xValues = PureHdfValueReader.ReadNumericDataset(xDataset);
                xAxisLabel = GetAxisLabel(request.XDatasetPath);
            }

            return new WaveformSeries(
                FilePath: request.FilePath,
                YDatasetPath: request.YDatasetPath,
                XDatasetPath: request.XDatasetPath,
                SampleCount: yValues.Length,
                YValues: yValues,
                XValues: xValues,
                XAxisLabel: xAxisLabel,
                YAxisLabel: GetAxisLabel(request.YDatasetPath),
                Warnings: warnings);
        }
        catch (WaveformLoadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WaveformLoadException($"Unable to load the selected waveform: {ex.Message}");
        }
    }

    private static string GetAxisLabel(string datasetPath)
    {
        var lastSegment = datasetPath.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return string.IsNullOrWhiteSpace(lastSegment) ? datasetPath : lastSegment;
    }
}
