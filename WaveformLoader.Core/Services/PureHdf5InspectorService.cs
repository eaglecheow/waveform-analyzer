using PureHDF;
using WaveformLoader.Core.Interfaces;
using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Services;

public sealed class PureHdf5InspectorService : IHdf5InspectorService
{
    public Task<Hdf5FileSummary> InspectAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = H5File.OpenRead(filePath);
            var warnings = new List<string>();
            var datasetCount = 0;
            var childNodes = new List<Hdf5NodeInfo>();

            foreach (var child in file.Children())
            {
                childNodes.Add(BuildNode(child, CombinePath("/", child.Name), ref datasetCount, warnings));
            }

            var children = childNodes
                .OrderBy(node => node.NodeKind)
                .ThenBy(node => node.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (datasetCount == 0)
            {
                warnings.Add("The file does not contain any datasets.");
            }

            var root = new Hdf5NodeInfo(
                Path: "/",
                DisplayName: Path.GetFileName(filePath),
                NodeKind: Hdf5NodeKind.File,
                ElementType: null,
                Dimensions: Array.Empty<long>(),
                Attributes: BuildAttributes(file),
                Children: children,
                IsNumeric: false,
                IsPlottable: false);

            return new Hdf5FileSummary(filePath, root, datasetCount, warnings);
        }, cancellationToken);
    }

    private static Hdf5NodeInfo BuildNode(IH5Object obj, string path, ref int datasetCount, List<string> warnings)
    {
        return obj switch
        {
            IH5Group group => BuildGroup(group, path, ref datasetCount, warnings),
            IH5Dataset dataset => BuildDataset(dataset, path, ref datasetCount),
            _ => BuildUnsupportedNode(obj, path, warnings)
        };
    }

    private static Hdf5NodeInfo BuildGroup(IH5Group group, string path, ref int datasetCount, List<string> warnings)
    {
        var childNodes = new List<Hdf5NodeInfo>();

        foreach (var child in group.Children())
        {
            childNodes.Add(BuildNode(child, CombinePath(path, child.Name), ref datasetCount, warnings));
        }

        var children = childNodes
            .OrderBy(node => node.NodeKind)
            .ThenBy(node => node.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new Hdf5NodeInfo(
            Path: path,
            DisplayName: group.Name,
            NodeKind: Hdf5NodeKind.Group,
            ElementType: null,
            Dimensions: Array.Empty<long>(),
            Attributes: BuildAttributes(group),
            Children: children,
            IsNumeric: false,
            IsPlottable: false);
    }

    private static Hdf5NodeInfo BuildDataset(IH5Dataset dataset, string path, ref int datasetCount)
    {
        datasetCount++;

        var dimensions = PureHdfValueReader.GetDimensions(dataset.Space);
        var isNumeric = PureHdfValueReader.IsNumeric(dataset.Type);

        return new Hdf5NodeInfo(
            Path: path,
            DisplayName: dataset.Name,
            NodeKind: Hdf5NodeKind.Dataset,
            ElementType: PureHdfValueReader.DescribeDataType(dataset.Type),
            Dimensions: dimensions,
            Attributes: BuildAttributes(dataset),
            Children: Array.Empty<Hdf5NodeInfo>(),
            IsNumeric: isNumeric,
            IsPlottable: isNumeric && dimensions.Length == 1 && dimensions[0] > 0);
    }

    private static Hdf5NodeInfo BuildUnsupportedNode(IH5Object obj, string path, List<string> warnings)
    {
        warnings.Add($"Skipped unsupported HDF5 object '{path}' of runtime type '{obj.GetType().Name}'.");

        return new Hdf5NodeInfo(
            Path: path,
            DisplayName: obj.Name,
            NodeKind: Hdf5NodeKind.Group,
            ElementType: null,
            Dimensions: Array.Empty<long>(),
            Attributes: BuildAttributes(obj),
            Children: Array.Empty<Hdf5NodeInfo>(),
            IsNumeric: false,
            IsPlottable: false);
    }

    private static IReadOnlyList<Hdf5AttributeInfo> BuildAttributes(IH5Object obj) =>
        obj.Attributes()
            .Select(attribute => new Hdf5AttributeInfo(attribute.Name, PureHdfValueReader.FormatAttributePreview(attribute)))
            .OrderBy(attribute => attribute.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string CombinePath(string parentPath, string childName) =>
        parentPath == "/" ? $"/{childName}" : $"{parentPath.TrimEnd('/')}/{childName}";
}
