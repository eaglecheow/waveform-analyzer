namespace WaveformLoader.App.Models;

public sealed class DatasetOptionViewModel
{
    public DatasetOptionViewModel(string displayName, string? path)
    {
        DisplayName = displayName;
        Path = path;
    }

    public string DisplayName { get; }

    public string? Path { get; }

    public bool UsesImplicitIndex => Path is null;

    public override string ToString() => DisplayName;
}
