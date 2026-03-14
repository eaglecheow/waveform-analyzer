using WaveformLoader.Core.Exceptions;
using WaveformLoader.Core.Models;
using WaveformLoader.Core.Services;

namespace WaveformLoader.Tests;

public sealed class PureHdf5WaveformLoaderTests
{
    [Fact]
    public async Task LoadAsync_LoadsImplicitAndExplicitWaveforms()
    {
        var filePath = TestHdf5Factory.CreateWaveformFile();
        var loader = new PureHdf5WaveformLoader();

        var implicitSeries = await loader.LoadAsync(new WaveformRequest(filePath, "/waveforms/y", null));
        var explicitSeries = await loader.LoadAsync(new WaveformRequest(filePath, "/waveforms/y", "/waveforms/x"));

        Assert.True(implicitSeries.UsesImplicitX);
        Assert.Equal(4, implicitSeries.SampleCount);
        Assert.Equal(new[] { 0.0, 1.25, -0.5, 0.25 }, implicitSeries.YValues);

        Assert.False(explicitSeries.UsesImplicitX);
        Assert.NotNull(explicitSeries.XValues);
        Assert.Equal(new[] { 0.0, 0.1, 0.2, 0.3 }, explicitSeries.XValues!);
        Assert.Equal("x", explicitSeries.XAxisLabel);
        Assert.Equal("y", explicitSeries.YAxisLabel);
    }

    [Fact]
    public async Task LoadAsync_RejectsUnsupportedOrMismatchedDatasets()
    {
        var filePath = TestHdf5Factory.CreateWaveformFile();
        var loader = new PureHdf5WaveformLoader();

        await Assert.ThrowsAsync<WaveformLoadException>(() =>
            loader.LoadAsync(new WaveformRequest(filePath, "/waveforms/notes", null)));

        await Assert.ThrowsAsync<WaveformLoadException>(() =>
            loader.LoadAsync(new WaveformRequest(filePath, "/waveforms/y", "/waveforms/xShort")));

        await Assert.ThrowsAsync<WaveformLoadException>(() =>
            loader.LoadAsync(new WaveformRequest(filePath, "/waveforms/matrix", null)));
    }
}
