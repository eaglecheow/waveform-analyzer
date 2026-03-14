# Waveform Loader

A `.NET 10` cross-platform desktop viewer for HDF5 waveform files built with `Avalonia`, `PureHDF`, and `ScottPlot`.

## Features

- Open local `.h5` and `.hdf5` files with a desktop file picker.
- Inspect the HDF5 group/dataset tree and attribute metadata before loading waveform samples.
- Plot a selected numeric 1D dataset in a separate interactive window.
- Use sample index as X by default, or pair the Y dataset with a second numeric 1D X dataset of the same length.
- Zoom, pan, fit, reset, and click a point to inspect exact `X` and `Y` values.
- Handle large datasets efficiently with ScottPlot `Signal` rendering for implicit X and viewport downsampling for explicit X.

## Run

```powershell
$env:DOTNET_CLI_HOME = "$PWD\.dotnet"
dotnet run --project .\WaveformLoader.App\WaveformLoader.App.csproj
```

## Test

```powershell
dotnet test .\WaveformLoader.Tests\WaveformLoader.Tests.csproj
```

## Notes

- `Y` plotting in v1 supports numeric 1D datasets only.
- `X` datasets must also be numeric 1D and must have the same length as `Y`.
- Very large explicit `X/Y` pairs are loaded into memory in v1 and then downsampled for display.
- Linux and macOS users may need the native dependencies described by ScottPlot/SkiaSharp for fonts and rendering.
