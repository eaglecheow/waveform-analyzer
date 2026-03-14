using System.Globalization;
using PureHDF;
using WaveformLoader.Core.Exceptions;

namespace WaveformLoader.Core.Services;

internal static class PureHdfValueReader
{
    public static bool IsNumeric(IH5DataType dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);
        return dataType.Class is H5DataTypeClass.FixedPoint or H5DataTypeClass.FloatingPoint;
    }

    public static long[] GetDimensions(IH5Dataspace dataspace)
    {
        ArgumentNullException.ThrowIfNull(dataspace);
        return dataspace.Dimensions.Select(dimension => checked((long)dimension)).ToArray();
    }

    public static string DescribeDataType(IH5DataType dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);

        if (IsStringLike(dataType))
        {
            return "string";
        }

        return dataType.Class switch
        {
            H5DataTypeClass.FixedPoint => $"{(dataType.FixedPoint?.IsSigned == false ? "u" : string.Empty)}int{dataType.Size * 8}",
            H5DataTypeClass.FloatingPoint => $"float{dataType.Size * 8}",
            H5DataTypeClass.Compound => "compound",
            H5DataTypeClass.Enumerated => "enum",
            H5DataTypeClass.Array => "array",
            H5DataTypeClass.VariableLength => "variablelength",
            _ => dataType.Class.ToString().ToLowerInvariant()
        };
    }

    public static double[] ReadNumericDataset(IH5Dataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        return dataset.Type.Class switch
        {
            H5DataTypeClass.FixedPoint => ReadFixedPointDataset(dataset),
            H5DataTypeClass.FloatingPoint => ReadFloatingPointDataset(dataset),
            _ => throw new WaveformLoadException($"Dataset '{dataset.Name}' is not numeric and cannot be plotted.")
        };
    }

    public static string FormatAttributePreview(IH5Attribute attribute, int maxItems = 8)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        var dimensions = GetDimensions(attribute.Space);
        var itemCount = GetItemCount(dimensions);
        var isScalar = dimensions.Length == 0;
        var typeDescription = DescribeDataType(attribute.Type);

        try
        {
            if (itemCount > maxItems)
            {
                return $"{typeDescription} [{FormatDimensions(dimensions)}]";
            }

            if (IsStringLike(attribute.Type))
            {
                return isScalar
                    ? attribute.Read<string>()
                    : FormatSequence(attribute.Read<string[]>());
            }

            return attribute.Type.Class switch
            {
                H5DataTypeClass.FixedPoint => isScalar
                    ? ReadFixedPointScalar(attribute).ToString(CultureInfo.InvariantCulture)
                    : FormatSequence(ReadFixedPointArray(attribute)),
                H5DataTypeClass.FloatingPoint => isScalar
                    ? ReadFloatingPointScalar(attribute).ToString("G6", CultureInfo.InvariantCulture)
                    : FormatSequence(ReadFloatingPointArray(attribute).Select(value => value.ToString("G6", CultureInfo.InvariantCulture))),
                _ => $"{typeDescription} [{FormatDimensions(dimensions)}]"
            };
        }
        catch
        {
            return $"{typeDescription} [{FormatDimensions(dimensions)}]";
        }
    }

    public static string FormatDimensions(IReadOnlyList<long> dimensions) =>
        dimensions.Count == 0 ? "scalar" : string.Join(" x ", dimensions);

    private static bool IsStringLike(IH5DataType dataType) =>
        dataType.Class == H5DataTypeClass.String ||
        (dataType.Class == H5DataTypeClass.VariableLength && dataType.VariableLength?.BaseType.Class == H5DataTypeClass.String);

    private static int GetItemCount(IReadOnlyList<long> dimensions)
    {
        if (dimensions.Count == 0)
        {
            return 1;
        }

        long total = 1;

        foreach (var dimension in dimensions)
        {
            total *= dimension;
        }

        return checked((int)total);
    }

    private static string FormatSequence<T>(IEnumerable<T> values) => $"[{string.Join(", ", values)}]";

    private static double[] ReadFixedPointDataset(IH5Dataset dataset)
    {
        var isSigned = dataset.Type.FixedPoint?.IsSigned != false;

        return dataset.Type.Size switch
        {
            1 when isSigned => dataset.Read<sbyte[]>().Select(value => (double)value).ToArray(),
            1 => dataset.Read<byte[]>().Select(value => (double)value).ToArray(),
            2 when isSigned => dataset.Read<short[]>().Select(value => (double)value).ToArray(),
            2 => dataset.Read<ushort[]>().Select(value => (double)value).ToArray(),
            4 when isSigned => dataset.Read<int[]>().Select(value => (double)value).ToArray(),
            4 => dataset.Read<uint[]>().Select(value => (double)value).ToArray(),
            8 when isSigned => dataset.Read<long[]>().Select(value => (double)value).ToArray(),
            8 => dataset.Read<ulong[]>().Select(value => (double)value).ToArray(),
            _ => throw new WaveformLoadException($"Dataset '{dataset.Name}' uses unsupported integer width {dataset.Type.Size * 8} bits.")
        };
    }

    private static double[] ReadFloatingPointDataset(IH5Dataset dataset) =>
        dataset.Type.Size switch
        {
            2 => dataset.Read<Half[]>().Select(value => (double)value).ToArray(),
            4 => dataset.Read<float[]>().Select(value => (double)value).ToArray(),
            8 => dataset.Read<double[]>(),
            _ => throw new WaveformLoadException($"Dataset '{dataset.Name}' uses unsupported floating-point width {dataset.Type.Size * 8} bits.")
        };

    private static IEnumerable<string> ReadFixedPointArray(IH5Attribute attribute)
    {
        var isSigned = attribute.Type.FixedPoint?.IsSigned != false;

        return attribute.Type.Size switch
        {
            1 when isSigned => attribute.Read<sbyte[]>().Select(value => value.ToString(CultureInfo.InvariantCulture)),
            1 => attribute.Read<byte[]>().Select(value => value.ToString(CultureInfo.InvariantCulture)),
            2 when isSigned => attribute.Read<short[]>().Select(value => value.ToString(CultureInfo.InvariantCulture)),
            2 => attribute.Read<ushort[]>().Select(value => value.ToString(CultureInfo.InvariantCulture)),
            4 when isSigned => attribute.Read<int[]>().Select(value => value.ToString(CultureInfo.InvariantCulture)),
            4 => attribute.Read<uint[]>().Select(value => value.ToString(CultureInfo.InvariantCulture)),
            8 when isSigned => attribute.Read<long[]>().Select(value => value.ToString(CultureInfo.InvariantCulture)),
            8 => attribute.Read<ulong[]>().Select(value => value.ToString(CultureInfo.InvariantCulture)),
            _ => throw new WaveformLoadException($"Attribute '{attribute.Name}' uses unsupported integer width {attribute.Type.Size * 8} bits.")
        };
    }

    private static double[] ReadFloatingPointArray(IH5Attribute attribute) =>
        attribute.Type.Size switch
        {
            2 => attribute.Read<Half[]>().Select(value => (double)value).ToArray(),
            4 => attribute.Read<float[]>().Select(value => (double)value).ToArray(),
            8 => attribute.Read<double[]>(),
            _ => throw new WaveformLoadException($"Attribute '{attribute.Name}' uses unsupported floating-point width {attribute.Type.Size * 8} bits.")
        };

    private static double ReadFixedPointScalar(IH5Attribute attribute)
    {
        var isSigned = attribute.Type.FixedPoint?.IsSigned != false;

        return attribute.Type.Size switch
        {
            1 when isSigned => attribute.Read<sbyte>(),
            1 => attribute.Read<byte>(),
            2 when isSigned => attribute.Read<short>(),
            2 => attribute.Read<ushort>(),
            4 when isSigned => attribute.Read<int>(),
            4 => attribute.Read<uint>(),
            8 when isSigned => attribute.Read<long>(),
            8 => attribute.Read<ulong>(),
            _ => throw new WaveformLoadException($"Attribute '{attribute.Name}' uses unsupported integer width {attribute.Type.Size * 8} bits.")
        };
    }

    private static double ReadFloatingPointScalar(IH5Attribute attribute) =>
        attribute.Type.Size switch
        {
            2 => (double)attribute.Read<Half>(),
            4 => attribute.Read<float>(),
            8 => attribute.Read<double>(),
            _ => throw new WaveformLoadException($"Attribute '{attribute.Name}' uses unsupported floating-point width {attribute.Type.Size * 8} bits.")
        };
}
