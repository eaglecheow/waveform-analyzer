using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Avalonia;
using ScottPlot.Plottables;
using WaveformLoader.App.ViewModels;
using WaveformLoader.Core.Models;
using WaveformLoader.Core.Utilities;

namespace WaveformLoader.App.Views;

public partial class WaveformPlotWindow : Window
{
    private readonly WaveformSeries _series;
    private readonly WaveformPlotWindowViewModel _viewModel;
    private readonly AvaPlot _plotControl;
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
            if (_isInitialized && !_series.UsesImplicitX)
            {
                RefreshExplicitScatterForCurrentLimits();
            }
        };
    }

    private void ConfigurePlot()
    {
        _plotControl.Reset();
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

        FitToFullExtent();
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
    }

    private void FitToFullExtent()
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

        if (!_series.UsesImplicitX)
        {
            RefreshExplicitScatterForCurrentLimits();
        }
        else
        {
            _plotControl.Refresh();
        }
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
}
