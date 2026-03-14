using WaveformLoader.Core.Models;

namespace WaveformLoader.Core.Interfaces;

public interface IHdf5InspectorService
{
    Task<Hdf5FileSummary> InspectAsync(string filePath, CancellationToken cancellationToken = default);
}
