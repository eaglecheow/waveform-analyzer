using System.Collections.ObjectModel;
using WaveformLoader.App.Models;
using WaveformLoader.Core.Measurements;
using WaveformLoader.Core.Models;

namespace WaveformLoader.App.ViewModels;

public sealed class WaveformPlotWindowViewModel : ViewModelBase
{
    private readonly IReadOnlyList<IWaveformMeasurement> _measurementCatalog;
    private readonly WaveformSeries _series;
    private MeasurementItemViewModel? _selectedMeasurement;
    private string _selectedPointText = "Click the plot to inspect the nearest sample.";
    private string _statusMessage;

    public WaveformPlotWindowViewModel(WaveformSeries series, IReadOnlyList<IWaveformMeasurement>? measurementCatalog = null)
    {
        _series = series ?? throw new ArgumentNullException(nameof(series));
        _measurementCatalog = measurementCatalog ?? WaveformMeasurementCatalog.Default;
        WindowTitle = $"Waveform Plot - {Path.GetFileName(series.FilePath)}";
        SeriesSummary = $"Samples: {series.SampleCount:N0} | Y: {series.YDatasetPath} | X: {(series.XDatasetPath ?? "sample index")}";
        _statusMessage = series.Warnings.Count == 0
            ? "Zoom with the mouse wheel, pan by dragging, and single-click to inspect a point."
            : string.Join(Environment.NewLine, series.Warnings);
        Measurements = new ObservableCollection<MeasurementItemViewModel>();
        RefreshMeasurements();
    }

    public string WindowTitle { get; }

    public string SeriesSummary { get; }

    public ObservableCollection<MeasurementItemViewModel> Measurements { get; }

    public MeasurementItemViewModel? SelectedMeasurement
    {
        get => _selectedMeasurement;
        set => SetProperty(ref _selectedMeasurement, value);
    }

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

    public void RefreshMeasurements(AxisRange? range = null)
    {
        var previousSelectionId = SelectedMeasurement?.Id;
        var context = new WaveformMeasurementContext(_series, range);
        var items = _measurementCatalog
            .Select(measurement => new MeasurementItemViewModel(measurement.Evaluate(context)))
            .ToArray();

        Measurements.Clear();

        foreach (var item in items)
        {
            Measurements.Add(item);
        }

        SelectedMeasurement = items.FirstOrDefault(item => string.Equals(item.Id, previousSelectionId, StringComparison.Ordinal))
            ?? items.FirstOrDefault(item => item.IsAvailable)
            ?? items.FirstOrDefault();
    }
}
