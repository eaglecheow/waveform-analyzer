using WaveformLoader.Core.Models;
using WaveformLoader.Core.Services;

namespace WaveformLoader.Tests;

public sealed class PureHdf5InspectorServiceTests
{
    [Fact]
    public async Task InspectAsync_BuildsDatasetTreeAndMetadata()
    {
        var filePath = TestHdf5Factory.CreateWaveformFile();
        var service = new PureHdf5InspectorService();

        var summary = await service.InspectAsync(filePath);

        Assert.Equal(filePath, summary.FilePath);
        Assert.Equal(5, summary.DatasetCount);
        Assert.Contains(summary.Root.Attributes, attribute => attribute.Name == "instrument");

        var yNode = FindNode(summary.Root, "/waveforms/y");
        Assert.NotNull(yNode);
        Assert.True(yNode!.IsPlottable);
        Assert.Equal("float64", yNode.ElementType);
        Assert.Equal(new long[] { 4 }, yNode.Dimensions);
        Assert.Contains(yNode.Attributes, attribute => attribute.Name == "unit" && !string.IsNullOrWhiteSpace(attribute.DisplayValue));

        var matrixNode = FindNode(summary.Root, "/waveforms/matrix");
        Assert.NotNull(matrixNode);
        Assert.False(matrixNode!.IsPlottable);
    }

    private static Hdf5NodeInfo? FindNode(Hdf5NodeInfo node, string path)
    {
        if (node.Path == path)
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            var match = FindNode(child, path);

            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
