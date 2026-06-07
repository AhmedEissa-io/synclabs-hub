using System.Windows;
using System.Windows.Input;
using SyncLabsHub.App.ViewModels;
using Wpf.Ui.Controls;

namespace SyncLabsHub.App.Views;

public partial class LoginWindow : FluentWindow
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            vm.Succeeded += OnSucceeded;
        EmailInput.Focus();
    }

    private void OnSucceeded()
    {
        App.ShowMain();
        Close();
    }

    private async void OnSignInClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
            await vm.LoginAsync(PasswordInput.Password);
    }

    private async void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
            await vm.LoginAsync(PasswordInput.Password);
    }
}
