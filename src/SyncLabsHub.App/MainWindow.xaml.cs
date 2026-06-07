using System.Windows;
using SyncLabsHub.App.ViewModels;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace SyncLabsHub.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.LogoutRequested += OnLogoutRequested;
    }

    private void OnLogoutRequested()
    {
        App.ShowLogin();
        Close();
    }

    private void OnThemeToggleClick(object sender, RoutedEventArgs e)
    {
        var isDark = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;
        var next = isDark ? ApplicationTheme.Light : ApplicationTheme.Dark;
        ApplicationThemeManager.Apply(next);
        ThemeIcon.Symbol = isDark ? SymbolRegular.WeatherMoon24 : SymbolRegular.WeatherSunny24;
    }
}
