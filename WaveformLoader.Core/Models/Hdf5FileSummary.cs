namespace WaveformLoader.Core.Models;

public sealed record Hdf5FileSummary(
    string FilePath,
    Hdf5NodeInfo Root,
    int DatasetCount,
    IReadOnlyList<string> Warnings);
