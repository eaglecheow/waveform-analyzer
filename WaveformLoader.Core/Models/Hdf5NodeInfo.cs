namespace WaveformLoader.Core.Models;

public sealed record Hdf5NodeInfo(
    string Path,
    string DisplayName,
    Hdf5NodeKind NodeKind,
    string? ElementType,
    IReadOnlyList<long> Dimensions,
    IReadOnlyList<Hdf5AttributeInfo> Attributes,
    IReadOnlyList<Hdf5NodeInfo> Children,
    bool IsNumeric,
    bool IsPlottable)
{
    public int Rank => Dimensions.Count;

    public string ShapeDisplay => Dimensions.Count == 0
        ? "-"
        : string.Join(" x ", Dimensions);
}
