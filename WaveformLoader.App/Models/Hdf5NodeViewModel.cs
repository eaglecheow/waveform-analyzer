using WaveformLoader.Core.Models;

namespace WaveformLoader.App.Models;

public sealed class Hdf5NodeViewModel
{
    public Hdf5NodeViewModel(Hdf5NodeInfo model)
    {
        Model = model;
        Children = model.Children.Select(child => new Hdf5NodeViewModel(child)).ToArray();
    }

    public Hdf5NodeInfo Model { get; }

    public string DisplayName => Model.DisplayName;

    public string Path => Model.Path;

    public string KindDisplay => Model.NodeKind.ToString();

    public string ElementType => Model.ElementType ?? "-";

    public string ShapeDisplay => Model.ShapeDisplay;

    public bool IsPlottable => Model.IsPlottable;

    public bool IsDataset => Model.NodeKind == Hdf5NodeKind.Dataset;

    public IReadOnlyList<Hdf5NodeViewModel> Children { get; }

    public IReadOnlyList<Hdf5AttributeInfo> Attributes => Model.Attributes;

    public string PlotStatusText => Model.NodeKind switch
    {
        Hdf5NodeKind.Dataset when Model.IsPlottable => "Numeric 1D dataset. Ready for plotting.",
        Hdf5NodeKind.Dataset => "Visible in the inspector, but not plottable in v1.",
        Hdf5NodeKind.Group => "Container group.",
        _ => "Root file node."
    };

    public string NodeSummary => Model.NodeKind switch
    {
        Hdf5NodeKind.Dataset => $"{ElementType} | {ShapeDisplay}",
        Hdf5NodeKind.Group => $"{Children.Count} item(s)",
        _ => $"{Children.Count} top-level item(s)"
    };
}
