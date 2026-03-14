using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Interfaces;

public interface IWaveformLoader
{
    Task<WaveformSeries> LoadAsync(WaveformRequest request, CancellationToken cancellationToken = default);
}
