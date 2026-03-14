namespace WaveformLoader.Core.Models;

public sealed record WaveformSeries(
    string FilePath,
    string YDatasetPath,
    string? XDatasetPath,
    int SampleCount,
    double[] YValues,
    double[]? XValues,
    string XAxisLabel,
    string YAxisLabel,
    IReadOnlyList<string> Warnings)
{
    public bool UsesImplicitX => XValues is null;

    public double GetXValue(int index) => XValues is null ? index : XValues[index];
}
