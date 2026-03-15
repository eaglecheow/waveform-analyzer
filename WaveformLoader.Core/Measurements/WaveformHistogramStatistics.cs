namespace WaveformLoader.Core.Measurements;

public sealed record WaveformHistogramStatistics(
    double Min,
    double Max,
    double Mean,
    double Median,
    double Mode,
    double Variance,
    double StandardDeviation,
    int SampleCount);
