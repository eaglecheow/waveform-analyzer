using WaveformLoader.Core.Models;

namespace WaveformLoader.App.ViewModels;

public sealed class WaveformPlotWindowViewModel : ViewModelBase
{
    private string _selectedPointText = "Click the plot to inspect the nearest sample.";
    private string _statusMessage;

    public WaveformPlotWindowViewModel(WaveformSeries series)
    {
        WindowTitle = $"Waveform Plot - {Path.GetFileName(series.FilePath)}";
        SeriesSummary = $"Samples: {series.SampleCount:N0} | Y: {series.YDatasetPath} | X: {(series.XDatasetPath ?? "sample index")}";
        _statusMessage = series.Warnings.Count == 0
            ? "Zoom with the mouse wheel, pan by dragging, and single-click to inspect a point."
            : string.Join(Environment.NewLine, series.Warnings);
    }

    public string WindowTitle { get; }

    public string SeriesSummary { get; }

    public string SelectedPointText
    {
        get => _selectedPointText;
        set => SetProperty(ref _selectedPointText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
}
