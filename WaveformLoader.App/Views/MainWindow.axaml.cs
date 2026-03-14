using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using WaveformLoader.App.Models;
using WaveformLoader.App.ViewModels;
using WaveformLoader.Core.Services;

namespace WaveformLoader.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly TreeView _structureTreeView;

    public MainWindow()
        : this(new MainWindowViewModel(new PureHdf5InspectorService(), new PureHdf5WaveformLoader()))
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        _structureTreeView = this.FindControl<TreeView>("StructureTreeView")
            ?? throw new InvalidOperationException("Structure tree view was not found.");

        _viewModel.PlotRequested += ViewModel_OnPlotRequested;
        Closed += (_, _) => _viewModel.PlotRequested -= ViewModel_OnPlotRequested;
    }

    private async void OpenFileButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open HDF5 Waveform",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("HDF5 Files")
                {
                    Patterns = ["*.h5", "*.hdf5"]
                }
            ]
        });

        var file = files.FirstOrDefault();
        var filePath = file?.TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        await _viewModel.LoadFileAsync(filePath);
    }

    private async void PlotButton_OnClick(object? sender, RoutedEventArgs e) =>
        await _viewModel.PlotSelectedWaveformAsync();

    private void StructureTreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_structureTreeView.SelectedItem is Hdf5NodeViewModel node)
        {
            _viewModel.SelectedNode = node;
        }
    }

    private void UseSelectedAsYButton_OnClick(object? sender, RoutedEventArgs e) =>
        _viewModel.UseSelectedAsY();

    private void UseSelectedAsXButton_OnClick(object? sender, RoutedEventArgs e) =>
        _viewModel.UseSelectedAsX();

    private void ViewModel_OnPlotRequested(object? sender, WaveformPlotRequestedEventArgs e)
    {
        var plotWindow = new WaveformPlotWindow(e.Series)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        plotWindow.Show(this);
    }
}
