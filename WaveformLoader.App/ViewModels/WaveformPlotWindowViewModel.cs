using System.Collections.ObjectModel;
using WaveformLoader.App.Models;
using WaveformLoader.Core.Measurements;
using WaveformLoader.Core.Models;

namespace WaveformLoader.App.ViewModels;

public sealed class WaveformPlotWindowViewModel : ViewModelBase
{
    private const int DefaultHistogramBinCount = 128;

    private readonly IReadOnlyList<IWaveformMeasurement> _measurementCatalog;
    private readonly WaveformSeries _series;
    private MeasurementItemViewModel? _histogramMeasurementItem;
    private MeasurementItemViewModel? _selectedMeasurement;
    private string _selectedPointText = "Click the plot to inspect the nearest sample.";
    private string _statusMessage;
    private bool _isHistogramEnabled;
    private WaveformHistogramOrientation _histogramOrientation = WaveformHistogramOrientation.Horizontal;
    private HistogramMeasurementScope _histogramScope = HistogramMeasurementScope.WholeWaveform;
    private int _histogramBinCount = DefaultHistogramBinCount;
    private AxisRange? _currentVisibleRange;

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
        HistogramResolutionOptions = [32, 64, 128, 256, 512];
        RefreshMeasurements();
    }

    public string WindowTitle { get; }

    public string SeriesSummary { get; }

    public ObservableCollection<MeasurementItemViewModel> Measurements { get; }

    public IReadOnlyList<int> HistogramResolutionOptions { get; }

    public MeasurementItemViewModel? HistogramMeasurementItem
    {
        get => _histogramMeasurementItem;
        private set => SetProperty(ref _histogramMeasurementItem, value);
    }

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

    public bool IsHistogramEnabled
    {
        get => _isHistogramEnabled;
        set
        {
            if (SetProperty(ref _isHistogramEnabled, value))
            {
                RaiseHistogramVisibilityPropertiesChanged();
            }
        }
    }

    public WaveformHistogramOrientation HistogramOrientation
    {
        get => _histogramOrientation;
        set
        {
            if (SetProperty(ref _histogramOrientation, value))
            {
                RaiseHistogramSelectionPropertiesChanged();
                RefreshMeasurements();
            }
        }
    }

    public HistogramMeasurementScope HistogramScope
    {
        get => _histogramScope;
        set
        {
            if (SetProperty(ref _histogramScope, value))
            {
                RaiseHistogramSelectionPropertiesChanged();
                RefreshMeasurements();
            }
        }
    }

    public int HistogramBinCount
    {
        get => _histogramBinCount;
        set
        {
            var normalizedValue = value > 0 ? value : DefaultHistogramBinCount;

            if (SetProperty(ref _histogramBinCount, normalizedValue))
            {
                RefreshMeasurements();
            }
        }
    }

    public bool IsHorizontalHistogramOrientation
    {
        get => HistogramOrientation == WaveformHistogramOrientation.Horizontal;
        set
        {
            if (value)
            {
                HistogramOrientation = WaveformHistogramOrientation.Horizontal;
            }
        }
    }

    public bool IsVerticalHistogramOrientation
    {
        get => HistogramOrientation == WaveformHistogramOrientation.Vertical;
        set
        {
            if (value)
            {
                HistogramOrientation = WaveformHistogramOrientation.Vertical;
            }
        }
    }

    public bool IsWholeWaveformHistogramScope
    {
        get => HistogramScope == HistogramMeasurementScope.WholeWaveform;
        set
        {
            if (value)
            {
                HistogramScope = HistogramMeasurementScope.WholeWaveform;
            }
        }
    }

    public bool IsVisibleRangeHistogramScope
    {
        get => HistogramScope == HistogramMeasurementScope.VisibleRange;
        set
        {
            if (value)
            {
                HistogramScope = HistogramMeasurementScope.VisibleRange;
            }
        }
    }

    public bool IsHorizontalHistogramVisible =>
        IsHistogramEnabled &&
        HistogramOrientation == WaveformHistogramOrientation.Horizontal &&
        HistogramMeasurementItem is { IsAvailable: true, HistogramData: not null };

    public bool IsVerticalHistogramVisible =>
        IsHistogramEnabled &&
        HistogramOrientation == WaveformHistogramOrientation.Vertical &&
        HistogramMeasurementItem is { IsAvailable: true, HistogramData: not null };

    public AxisRange? CurrentVisibleRange => _currentVisibleRange;

    public void UpdateVisibleRange(AxisRange? range)
    {
        if (Nullable.Equals(_currentVisibleRange, range))
        {
            return;
        }

        _currentVisibleRange = range;
        OnPropertyChanged(nameof(CurrentVisibleRange));

        if (HistogramScope == HistogramMeasurementScope.VisibleRange)
        {
            RefreshMeasurements();
        }
        else
        {
            RaiseHistogramVisibilityPropertiesChanged();
        }
    }

    public void RefreshMeasurements()
    {
        var previousSelectionId = SelectedMeasurement?.Id;
        var allItems = _measurementCatalog
            .Select(EvaluateMeasurement)
            .ToArray();
        var standardItems = allItems
            .Where(item => !string.Equals(item.Id, WaveformLoader.Core.Measurements.HistogramMeasurement.MeasurementId, StringComparison.Ordinal))
            .ToArray();

        HistogramMeasurementItem = allItems.FirstOrDefault(item => string.Equals(item.Id, WaveformLoader.Core.Measurements.HistogramMeasurement.MeasurementId, StringComparison.Ordinal));

        Measurements.Clear();

        foreach (var item in standardItems)
        {
            Measurements.Add(item);
        }

        SelectedMeasurement = standardItems.FirstOrDefault(item => string.Equals(item.Id, previousSelectionId, StringComparison.Ordinal))
            ?? standardItems.FirstOrDefault(item => item.IsAvailable)
            ?? standardItems.FirstOrDefault();

        RaiseHistogramSelectionPropertiesChanged();
        RaiseHistogramVisibilityPropertiesChanged();
    }

    private MeasurementItemViewModel EvaluateMeasurement(IWaveformMeasurement measurement)
    {
        var range = string.Equals(measurement.Id, WaveformLoader.Core.Measurements.HistogramMeasurement.MeasurementId, StringComparison.Ordinal) &&
                    HistogramScope == HistogramMeasurementScope.VisibleRange
            ? _currentVisibleRange
            : null;
        var context = new WaveformMeasurementContext(_series, range, HistogramOrientation, HistogramBinCount);
        return new MeasurementItemViewModel(measurement.Evaluate(context));
    }

    private void RaiseHistogramSelectionPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsHorizontalHistogramOrientation));
        OnPropertyChanged(nameof(IsVerticalHistogramOrientation));
        OnPropertyChanged(nameof(IsWholeWaveformHistogramScope));
        OnPropertyChanged(nameof(IsVisibleRangeHistogramScope));
    }

    private void RaiseHistogramVisibilityPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsHorizontalHistogramVisible));
        OnPropertyChanged(nameof(IsVerticalHistogramVisible));
    }
}
