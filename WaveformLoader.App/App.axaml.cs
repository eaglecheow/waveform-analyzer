using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using WaveformLoader.App.Services;
using WaveformLoader.App.ViewModels;
using WaveformLoader.App.Views;
using WaveformLoader.Core.Services;

namespace WaveformLoader.App;

public partial class App : Application
{
    private static readonly AppServices Services = new(
        inspectorService: new PureHdf5InspectorService(),
        waveformLoader: new PureHdf5WaveformLoader());

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(new MainWindowViewModel(Services.InspectorService, Services.WaveformLoader));
        }

        base.OnFrameworkInitializationCompleted();
    }
}
