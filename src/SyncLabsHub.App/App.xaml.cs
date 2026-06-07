using System.Windows;
using SyncLabsHub.App.ViewModels;
using SyncLabsHub.App.Views;
using SyncLabsHub.Core.Services;
using Velopack;

namespace SyncLabsHub.App;

public partial class App : Application
{
    /// <summary>App-wide licensing service — the one source of truth for the session.</summary>
    public static LicenseService License { get; } = new();

    [STAThread]
    public static void Main()
    {
        // Velopack must process install/update hooks before any WPF UI is created.
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var restored = await License.TryRestoreAsync(CancellationToken.None);
        if (restored is not null)
            ShowMain();
        else
            ShowLogin();
    }

    public static void ShowLogin()
    {
        var vm = new LoginViewModel();
        var window = new LoginWindow { DataContext = vm };
        Current.MainWindow = window;
        window.Show();
    }

    public static void ShowMain()
    {
        var window = new MainWindow { DataContext = new MainViewModel(License.Current!) };
        Current.MainWindow = window;
        window.Show();
    }
}
