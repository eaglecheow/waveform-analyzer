namespace WaveformLoader.Core.Models;

public sealed record DownsampledWaveform(
    double[] XValues,
    double[] YValues);
