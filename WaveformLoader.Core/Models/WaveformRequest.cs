namespace WaveformLoader.Core.Models;

public sealed record WaveformRequest(
    string FilePath,
    string YDatasetPath,
    string? XDatasetPath);
