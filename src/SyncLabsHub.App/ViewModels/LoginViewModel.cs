using SyncLabsHub.App.Mvvm;
using SyncLabsHub.Core.Services;

namespace SyncLabsHub.App.ViewModels;

public sealed class LoginViewModel : ObservableObject
{
    private string _email = "";
    private string _error = "";
    private bool _isBusy;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    /// <summary>Raised on a successful sign-in so the window can open the main shell.</summary>
    public event Action? Succeeded;

    public async Task LoginAsync(string password)
    {
        Error = "";

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
        {
            Error = "Enter your email and password.";
            return;
        }

        IsBusy = true;
        try
        {
            await App.License.LoginAsync(Email.Trim(), password, CancellationToken.None);
            Succeeded?.Invoke();
        }
        catch (SyncLabsAuthException ex)
        {
            Error = ex.Message;
        }
        catch (Exception)
        {
            Error = "Couldn't reach SyncLabs. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
