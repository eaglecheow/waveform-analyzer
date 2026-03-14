using System.Collections.ObjectModel;
using WaveformLoader.App.Models;
using WaveformLoader.Core.Interfaces;
using WaveformLoader.Core.Models;

namespace WaveformLoader.App.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IHdf5InspectorService _inspectorService;
    private readonly IWaveformLoader _waveformLoader;
    private Hdf5NodeViewModel? _selectedNode;
    private DatasetOptionViewModel? _selectedYDataset;
    private DatasetOptionViewModel? _selectedXDataset;
    private string _loadedFilePath = "No file selected";
    private string _statusMessage = "Open an .h5 or .hdf5 file to inspect and plot a waveform.";
    private string _warningMessage = string.Empty;
    private string _summaryText = "No file loaded.";
    private bool _isBusy;
    private IReadOnlyList<string> _fileWarnings = Array.Empty<string>();

    public MainWindowViewModel(IHdf5InspectorService inspectorService, IWaveformLoader waveformLoader)
    {
        _inspectorService = inspectorService;
        _waveformLoader = waveformLoader;
        RootNodes = new ObservableCollection<Hdf5NodeViewModel>();
        PlottableDatasets = new ObservableCollection<DatasetOptionViewModel>();
        XAxisOptions = new ObservableCollection<DatasetOptionViewModel>();
        RebuildXAxisOptions();
    }

    public event EventHandler<WaveformPlotRequestedEventArgs>? PlotRequested;

    public ObservableCollection<Hdf5NodeViewModel> RootNodes { get; }

    public ObservableCollection<DatasetOptionViewModel> PlottableDatasets { get; }

    public ObservableCollection<DatasetOptionViewModel> XAxisOptions { get; }

    public Hdf5NodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                OnPropertyChanged(nameof(CanUseSelectedAsY));
                OnPropertyChanged(nameof(CanUseSelectedAsX));
            }
        }
    }

    public DatasetOptionViewModel? SelectedYDataset
    {
        get => _selectedYDataset;
        set
        {
            if (SetProperty(ref _selectedYDataset, value))
            {
                RebuildXAxisOptions();
                OnPropertyChanged(nameof(CanPlot));
                OnPropertyChanged(nameof(CanUseSelectedAsX));
            }
        }
    }

    public DatasetOptionViewModel? SelectedXDataset
    {
        get => _selectedXDataset;
        set => SetProperty(ref _selectedXDataset, value);
    }

    public string LoadedFilePath
    {
        get => _loadedFilePath;
        private set => SetProperty(ref _loadedFilePath, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string WarningMessage
    {
        get => _warningMessage;
        private set
        {
            if (SetProperty(ref _warningMessage, value))
            {
                OnPropertyChanged(nameof(HasWarnings));
            }
        }
    }

    public bool HasWarnings => !string.IsNullOrWhiteSpace(WarningMessage);

    public string SummaryText
    {
        get => _summaryText;
        private set => SetProperty(ref _summaryText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanPlot));
            }
        }
    }

    public bool CanPlot => !IsBusy && SelectedYDataset?.Path is not null && !string.Equals(LoadedFilePath, "No file selected", StringComparison.Ordinal);

    public bool CanUseSelectedAsY => SelectedNode?.IsPlottable == true;

    public bool CanUseSelectedAsX => SelectedNode?.IsPlottable == true && SelectedNode.Path != SelectedYDataset?.Path;

    public async Task LoadFileAsync(string filePath)
    {
        IsBusy = true;
        StatusMessage = $"Inspecting '{Path.GetFileName(filePath)}'...";

        try
        {
            var summary = await _inspectorService.InspectAsync(filePath);
            LoadedFilePath = filePath;
            _fileWarnings = summary.Warnings;

            RootNodes.Clear();
            var rootNode = new Hdf5NodeViewModel(summary.Root);
            RootNodes.Add(rootNode);
            SelectedNode = rootNode;

            PlottableDatasets.Clear();

            foreach (var node in EnumeratePlottableNodes(summary.Root))
            {
                PlottableDatasets.Add(new DatasetOptionViewModel($"{node.Path} ({node.ElementType}, {node.ShapeDisplay})", node.Path));
            }

            SelectedYDataset = PlottableDatasets.FirstOrDefault();
            SummaryText = $"Datasets: {summary.DatasetCount} | Plottable 1D numeric datasets: {PlottableDatasets.Count}";
            StatusMessage = PlottableDatasets.Count == 0
                ? "The file loaded successfully, but it does not contain any plottable numeric 1D datasets."
                : "Select a waveform dataset and open it in the plot window.";
            WarningMessage = BuildWarningMessage(_fileWarnings, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            LoadedFilePath = filePath;
            RootNodes.Clear();
            PlottableDatasets.Clear();
            SelectedNode = null;
            SelectedYDataset = null;
            SummaryText = "Unable to inspect this file.";
            WarningMessage = string.Empty;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void UseSelectedAsY()
    {
        if (!CanUseSelectedAsY || SelectedNode is null)
        {
            return;
        }

        SelectedYDataset = PlottableDatasets.FirstOrDefault(option => option.Path == SelectedNode.Path);
    }

    public void UseSelectedAsX()
    {
        if (!CanUseSelectedAsX || SelectedNode is null)
        {
            return;
        }

        SelectedXDataset = XAxisOptions.FirstOrDefault(option => option.Path == SelectedNode.Path) ?? XAxisOptions.FirstOrDefault();
    }

    public async Task PlotSelectedWaveformAsync()
    {
        if (!CanPlot || SelectedYDataset?.Path is null)
        {
            StatusMessage = "Select a numeric 1D dataset for Y before plotting.";
            return;
        }

        IsBusy = true;
        StatusMessage = $"Loading '{SelectedYDataset.Path}' for plotting...";

        try
        {
            var series = await _waveformLoader.LoadAsync(new WaveformRequest(LoadedFilePath, SelectedYDataset.Path, SelectedXDataset?.Path));
            WarningMessage = BuildWarningMessage(_fileWarnings, series.Warnings);
            StatusMessage = $"Loaded {series.SampleCount:N0} samples. The waveform is ready in a separate window.";
            PlotRequested?.Invoke(this, new WaveformPlotRequestedEventArgs(series));
        }
        catch (Exception ex)
        {
            WarningMessage = BuildWarningMessage(_fileWarnings, Array.Empty<string>());
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RebuildXAxisOptions()
    {
        var previousSelection = SelectedXDataset?.Path;

        XAxisOptions.Clear();
        XAxisOptions.Add(new DatasetOptionViewModel("Use sample index", null));

        foreach (var option in PlottableDatasets.Where(option => option.Path != SelectedYDataset?.Path))
        {
            XAxisOptions.Add(option);
        }

        SelectedXDataset = XAxisOptions.FirstOrDefault(option => option.Path == previousSelection) ?? XAxisOptions.FirstOrDefault();
    }

    private static IEnumerable<Hdf5NodeInfo> EnumeratePlottableNodes(Hdf5NodeInfo root)
    {
        if (root.IsPlottable)
        {
            yield return root;
        }

        foreach (var child in root.Children)
        {
            foreach (var nested in EnumeratePlottableNodes(child))
            {
                yield return nested;
            }
        }
    }

    private static string BuildWarningMessage(IEnumerable<string> fileWarnings, IEnumerable<string> plotWarnings)
    {
        var warnings = fileWarnings.Concat(plotWarnings).Where(message => !string.IsNullOrWhiteSpace(message)).ToArray();
        return warnings.Length == 0 ? string.Empty : string.Join(Environment.NewLine, warnings);
    }
}
