namespace WaveformLoader.Core.Models;

public readonly record struct AxisRange(double Min, double Max)
{
    public bool IsValid =>
        !double.IsNaN(Min) &&
        !double.IsNaN(Max) &&
        !double.IsInfinity(Min) &&
        !double.IsInfinity(Max) &&
        Max > Min;
}
