using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using WaveformLoader.App.ViewModels;
using WaveformLoader.Core.Measurements;
using WaveformLoader.Core.Models;
using WaveformLoader.Core.Utilities;

namespace WaveformLoader.App.Views;

public partial class WaveformPlotWindow : Window
{
    private readonly WaveformSeries _series;
    private readonly WaveformPlotWindowViewModel _viewModel;
    private readonly AvaPlot _plotControl;
    private readonly AvaPlot _horizontalHistogramPlotControl;
    private readonly AvaPlot _verticalHistogramPlotControl;
    private readonly List<IPlottable> _measurementPlottables = [];
    private Signal? _signalPlot;
    private Scatter? _scatterPlot;
    private Crosshair? _crosshair;
    private bool _isRefreshingExplicitScatter;
    private bool _isInitialized;

    public WaveformPlotWindow()
        : this(new WaveformSeries("design", "/design/y", null, 1, [0.0], null, "Sample Index", "design-y", Array.Empty<string>()))
    {
    }

    public WaveformPlotWindow(WaveformSeries series)
    {
        _series = series;
        InitializeComponent();
        DataContext = _viewModel = new WaveformPlotWindowViewModel(series);
        Title = _viewModel.WindowTitle;
        _plotControl = this.FindControl<AvaPlot>("PlotControl")
            ?? throw new InvalidOperationException("Plot control was not found.");
        _horizontalHistogramPlotControl = this.FindControl<AvaPlot>("HorizontalHistogramPlotControl")
            ?? throw new InvalidOperationException("Horizontal histogram plot control was not found.");
        _verticalHistogramPlotControl = this.FindControl<AvaPlot>("VerticalHistogramPlotControl")
            ?? throw new InvalidOperationException("Vertical histogram plot control was not found.");
        _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;

        Opened += (_, _) =>
        {
            if (_isInitialized)
            {
                return;
            }

            ConfigurePlot();
            _isInitialized = true;
        };

        _plotControl.SizeChanged += (_, _) =>
        {
            if (!_isInitialized)
            {
                return;
            }

            if (!_series.UsesImplicitX)
            {
                RefreshExplicitScatterForCurrentLimits();
            }

            RenderHistogramPlots();
        };

        Closed += (_, _) => _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
    }

    private void ConfigurePlot()
    {
        _plotControl.Reset();
        _horizontalHistogramPlotControl.Reset();
        _verticalHistogramPlotControl.Reset();

        var plot = _plotControl.Plot;
        plot.Clear();
        plot.Title(System.IO.Path.GetFileName(_series.FilePath));
        plot.XLabel(_series.XAxisLabel);
        plot.YLabel(_series.YAxisLabel);
        plot.HideLegend();
        plot.RenderManager.AxisLimitsChanged += Plot_RenderManager_AxisLimitsChanged;

        _crosshair = plot.Add.Crosshair(0, 0);
        _crosshair.IsVisible = false;
        _crosshair.EnableAutoscale = false;
        _crosshair.LineColor = ScottPlot.Colors.OrangeRed;
        _crosshair.MarkerSize = 8;
        _crosshair.MarkerLineWidth = 2;

        if (_series.UsesImplicitX)
        {
            _signalPlot = plot.Add.Signal(_series.YValues, 1, ScottPlot.Colors.CornflowerBlue);
            _signalPlot.LineWidth = 1.5f;
            _signalPlot.MarkerSize = 0;
            _signalPlot.AlwaysUseLowDensityMode = true;
        }
        else
        {
            RefreshExplicitScatterForCurrentLimits(useFullExtent: true, refreshControl: false);
        }

        _viewModel.RefreshMeasurements();
        FitToFullExtent(refreshControl: false);
        SyncVisibleRangeWithPlot();
        RenderSelectedMeasurementMarkers(refreshControl: false);
        RenderHistogramPlots(refreshControl: false);
        RefreshPlotControls();
    }

    private void ResetButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _viewModel.SelectedPointText = "Click the plot to inspect the nearest sample.";
        ConfigurePlot();
    }

    private void FitButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        FitToFullExtent();

    private void OpenAnotherWaveformButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Owner is Window owner)
        {
            owner.Activate();
        }
    }

    private void PlotControl_OnTapped(object? sender, TappedEventArgs e)
    {
        var position = e.GetPosition(_plotControl);
        var pixel = new Pixel(position.X * Math.Max(_plotControl.DisplayScale, 1f), position.Y * Math.Max(_plotControl.DisplayScale, 1f));
        var coordinates = _plotControl.Plot.GetCoordinates(pixel, _plotControl.Plot.Axes.Bottom, _plotControl.Plot.Axes.Left);
        var selectedPoint = WaveformPointSelector.SelectNearestPoint(_series, coordinates.X);

        if (_crosshair is not null)
        {
            _crosshair.IsVisible = true;
            _crosshair.X = selectedPoint.X;
            _crosshair.Y = selectedPoint.Y;
        }

        _viewModel.SelectedPointText = $"Index: {selectedPoint.Index:N0}    X: {selectedPoint.X:G8}    Y: {selectedPoint.Y:G8}";
        _plotControl.Refresh();
    }

    private void Plot_RenderManager_AxisLimitsChanged(object? sender, RenderDetails e)
    {
        if (!_series.UsesImplicitX)
        {
            RefreshExplicitScatterForCurrentLimits();
        }

        SyncVisibleRangeWithPlot();
        RenderHistogramPlots();
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        if (e.PropertyName == nameof(WaveformPlotWindowViewModel.SelectedMeasurement))
        {
            RenderSelectedMeasurementMarkers();
            return;
        }

        if (e.PropertyName is nameof(WaveformPlotWindowViewModel.HistogramMeasurementItem) or
            nameof(WaveformPlotWindowViewModel.IsHistogramEnabled) or
            nameof(WaveformPlotWindowViewModel.HistogramOrientation) or
            nameof(WaveformPlotWindowViewModel.HistogramScope) or
            nameof(WaveformPlotWindowViewModel.IsHorizontalHistogramVisible) or
            nameof(WaveformPlotWindowViewModel.IsVerticalHistogramVisible))
        {
            RenderHistogramPlots();
        }
    }

    private void FitToFullExtent(bool refreshControl = true)
    {
        var xMin = _series.UsesImplicitX ? 0 : _series.XValues!.Min();
        var xMax = _series.UsesImplicitX ? Math.Max(1, _series.SampleCount - 1) : _series.XValues!.Max();
        var yMin = _series.YValues.Min();
        var yMax = _series.YValues.Max();

        if (Math.Abs(yMax - yMin) < double.Epsilon)
        {
            var padding = Math.Abs(yMin) < 1 ? 1 : Math.Abs(yMin) * 0.05;
            yMin -= padding;
            yMax += padding;
        }

        _plotControl.Plot.Axes.SetLimits(xMin, xMax, yMin, yMax);
        SyncVisibleRangeWithPlot();

        if (!_series.UsesImplicitX)
        {
            RefreshExplicitScatterForCurrentLimits(refreshControl: refreshControl);
        }
        else if (refreshControl)
        {
            _plotControl.Refresh();
        }

        RenderHistogramPlots(refreshControl);
    }

    private void RenderSelectedMeasurementMarkers(bool refreshControl = true)
    {
        var plot = _plotControl.Plot;

        foreach (var plottable in _measurementPlottables)
        {
            plot.Remove(plottable);
        }

        _measurementPlottables.Clear();

        if (_viewModel.SelectedMeasurement is not { } measurement)
        {
            if (refreshControl)
            {
                _plotControl.Refresh();
            }

            return;
        }

        foreach (var marker in measurement.Markers)
        {
            var style = GetMeasurementLineStyle(marker);
            var line = plot.Add.HorizontalLine(marker.Y, 2f, style.Color, ScottPlot.LinePattern.Solid);
            line.EnableAutoscale = false;
            line.LineColor = style.Color;
            line.LineWidth = 2;
            line.LabelText = marker.Label;
            line.LabelAlignment = style.Alignment;
            line.ManualLabelAlignment = style.Alignment;
            line.LabelBackgroundColor = ScottPlot.Colors.White;
            line.LabelBorderColor = style.Color;
            line.LabelBorderWidth = 1;
            line.LabelPadding = 4;
            line.LabelBold = true;
            line.LabelFontName = "Consolas";
            line.LabelFontSize = 12;
            line.LabelFontColor = style.Color;
            line.LabelOffsetX = -8;
            line.LabelOffsetY = style.OffsetY;
            _measurementPlottables.Add(line);
        }

        if (refreshControl)
        {
            _plotControl.Refresh();
        }
    }

    private void RenderHistogramPlots(bool refreshControl = true)
    {
        RenderHorizontalHistogramPlot(refreshControl);
        RenderVerticalHistogramPlot(refreshControl);
    }

    private void RenderHorizontalHistogramPlot(bool refreshControl)
    {
        var plot = _horizontalHistogramPlotControl.Plot;
        plot.Clear();
        plot.HideLegend();
        plot.XLabel("Count");
        plot.YLabel(_series.YAxisLabel);

        if (_viewModel.IsHorizontalHistogramVisible && _viewModel.HistogramMeasurementItem?.HistogramData is { } histogramData)
        {
            var barPlot = plot.Add.Bars(CreateHistogramBars(histogramData));
            barPlot.Horizontal = true;
            barPlot.Color = ScottPlot.Colors.CornflowerBlue;

            var mainLimits = _plotControl.Plot.Axes.GetLimits();
            var maxCount = Math.Max(1, histogramData.Counts.Max());
            plot.Axes.SetLimits(0, maxCount * 1.05, mainLimits.Bottom, mainLimits.Top);
        }

        if (refreshControl)
        {
            _horizontalHistogramPlotControl.Refresh();
        }
    }

    private void RenderVerticalHistogramPlot(bool refreshControl)
    {
        var plot = _verticalHistogramPlotControl.Plot;
        plot.Clear();
        plot.HideLegend();
        plot.XLabel(_series.XAxisLabel);
        plot.YLabel("Count");

        if (_viewModel.IsVerticalHistogramVisible && _viewModel.HistogramMeasurementItem?.HistogramData is { } histogramData)
        {
            var barPlot = plot.Add.Bars(CreateHistogramBars(histogramData));
            barPlot.Horizontal = false;
            barPlot.Color = ScottPlot.Colors.CornflowerBlue;

            var mainLimits = _plotControl.Plot.Axes.GetLimits();
            var maxCount = Math.Max(1, histogramData.Counts.Max());
            plot.Axes.SetLimits(mainLimits.Left, mainLimits.Right, 0, maxCount * 1.05);
        }

        if (refreshControl)
        {
            _verticalHistogramPlotControl.Refresh();
        }
    }

    private void SyncVisibleRangeWithPlot()
    {
        var limits = _plotControl.Plot.Axes.GetLimits();
        var minimum = Math.Min(limits.Left, limits.Right);
        var maximum = Math.Max(limits.Left, limits.Right);
        _viewModel.UpdateVisibleRange(new AxisRange(minimum, maximum));
    }

    private void RefreshExplicitScatterForCurrentLimits(bool useFullExtent = false, bool refreshControl = true)
    {
        if (_series.XValues is null || _isRefreshingExplicitScatter)
        {
            return;
        }

        try
        {
            _isRefreshingExplicitScatter = true;

            var plot = _plotControl.Plot;
            var limits = useFullExtent
                ? new AxisRange(_series.XValues.Min(), _series.XValues.Max())
                : new AxisRange(plot.Axes.GetLimits().Left, plot.Axes.GetLimits().Right);
            var pixelWidth = Math.Max(200, (int)Math.Round(Math.Max(_plotControl.Bounds.Width, 200) * Math.Max(_plotControl.DisplayScale, 1f)));
            var reduced = WaveformDownsampler.DownsampleForVisibleRange(_series.XValues, _series.YValues, limits, pixelWidth);

            if (reduced.XValues.Length == 0)
            {
                reduced = WaveformDownsampler.DownsampleForVisibleRange(
                    _series.XValues,
                    _series.YValues,
                    new AxisRange(_series.XValues.Min(), _series.XValues.Max()),
                    pixelWidth);
            }

            if (_scatterPlot is not null)
            {
                plot.Remove(_scatterPlot);
            }

            _scatterPlot = plot.Add.Scatter(reduced.XValues, reduced.YValues, ScottPlot.Colors.CornflowerBlue);
            _scatterPlot.LineWidth = 1.25f;
            _scatterPlot.MarkerSize = 0;
            _scatterPlot.Smooth = false;

            if (!useFullExtent)
            {
                var current = plot.Axes.GetLimits();
                plot.RenderManager.DisableAxisLimitsChangedEventOnNextRender = true;
                plot.Axes.SetLimits(current);
            }

            if (refreshControl)
            {
                _plotControl.Refresh();
            }
        }
        finally
        {
            _isRefreshingExplicitScatter = false;
        }
    }

    private void RefreshPlotControls()
    {
        _plotControl.Refresh();
        _horizontalHistogramPlotControl.Refresh();
        _verticalHistogramPlotControl.Refresh();
    }

    private static ScottPlot.Bar[] CreateHistogramBars(WaveformHistogramData histogramData)
    {
        var bars = new ScottPlot.Bar[histogramData.Counts.Length];

        for (var index = 0; index < histogramData.Counts.Length; index++)
        {
            bars[index] = new ScottPlot.Bar
            {
                Position = histogramData.BinCenters[index],
                Value = histogramData.Counts[index],
                ValueBase = 0,
                Size = Math.Abs(histogramData.BinEdges[index + 1] - histogramData.BinEdges[index])
            };
        }

        return bars;
    }

    private static (ScottPlot.Color Color, Alignment Alignment, float OffsetY) GetMeasurementLineStyle(WaveformMeasurementMarker marker) =>
        marker.Kind switch
        {
            WaveformMeasurementMarkerKind.Maximum => (ScottPlot.Colors.OrangeRed, Alignment.UpperRight, -6f),
            WaveformMeasurementMarkerKind.Minimum => (ScottPlot.Colors.DodgerBlue, Alignment.LowerRight, 6f),
            _ => (ScottPlot.Colors.DarkOrange, Alignment.UpperRight, 0f)
        };
}
