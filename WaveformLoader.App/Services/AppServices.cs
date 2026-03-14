using WaveformLoader.Core.Interfaces;

namespace WaveformLoader.App.Services;

public sealed class AppServices
{
    public AppServices(IHdf5InspectorService inspectorService, IWaveformLoader waveformLoader)
    {
        InspectorService = inspectorService;
        WaveformLoader = waveformLoader;
    }

    public IHdf5InspectorService InspectorService { get; }

    public IWaveformLoader WaveformLoader { get; }
}
