using System.Collections.ObjectModel;
using System.Windows.Input;
using SyncLabsHub.App.Mvvm;
using SyncLabsHub.App.Services;
using SyncLabsHub.Core;
using SyncLabsHub.Core.Models;
using SyncLabsHub.Core.Services;

namespace SyncLabsHub.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly LicenseService _license;
    private readonly UpdateService _updates = new();
    private SectionViewModel? _selectedSection;
    private string _statusMessage = "Ready";
    private string _connectivityText;
    private string? _availableUpdate;

    public MainViewModel(CachedSession session)
    {
        _license = App.License;

        CurrentUser = string.IsNullOrWhiteSpace(session.Profile.FullName)
            ? session.Profile.Email
            : session.Profile.FullName;
        Email = session.Profile.Email;
        IsAdmin = session.Profile.IsAdmin;
        RoleLabel = IsAdmin ? "ADMIN" : "PRO";
        _connectivityText = _license.IsOfflineSession ? "Offline (cached)" : "Online";

        SettingsSection = new SettingsViewModel(_license);
        BuildSections();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
        ApplyUpdateCommand = new AsyncRelayCommand(() => _updates.ApplyAsync());
        OpenSettingsCommand = new RelayCommand(() => SelectedSection = SettingsSection);
        OpenWebsiteCommand = new RelayCommand(() => Launcher.OpenUrl(SyncLabsConfig.WebsiteUrl));
        LogoutCommand = new RelayCommand(() =>
        {
            _license.Logout();
            LogoutRequested?.Invoke();
        });

        _ = CheckForUpdatesAsync();
    }

    /// <summary>Non-null when a newer version is available; drives the "Update ready" banner.</summary>
    public string? AvailableUpdate
    {
        get => _availableUpdate;
        private set => SetProperty(ref _availableUpdate, value);
    }

    private async Task CheckForUpdatesAsync()
    {
        var version = await _updates.CheckAsync();
        AvailableUpdate = version;
        if (version is not null)
            StatusMessage = $"Update available: v{version}";
    }

    public ObservableCollection<SectionViewModel> Sections { get; } = new();
    public SettingsViewModel SettingsSection { get; }

    public SectionViewModel? SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    public string CurrentUser { get; }
    public string Email { get; }
    public bool IsAdmin { get; }
    public string RoleLabel { get; }
    public string ShellVersion => "1.0.0";

    public string ConnectivityText
    {
        get => _connectivityText;
        private set => SetProperty(ref _connectivityText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string UserBadgeTooltip => $"{CurrentUser}\n{Email}";

    public ICommand RefreshCommand { get; }
    public ICommand CheckForUpdatesCommand { get; }
    public ICommand ApplyUpdateCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenWebsiteCommand { get; }
    public ICommand LogoutCommand { get; }

    /// <summary>Raised after logout so the window can swap to the login screen.</summary>
    public event Action? LogoutRequested;

    private void BuildSections()
    {
        var keepIndex = SelectedSection is null ? 0 : Sections.IndexOf(SelectedSection);

        Sections.Clear();
        Sections.Add(new MyToolsViewModel(_license));
        Sections.Add(new StoreViewModel(_license));
        Sections.Add(new LicensesViewModel(_license));
        Sections.Add(new UpdatesViewModel());
        if (IsAdmin)
            Sections.Add(new AdminViewModel(_license));

        SelectedSection = keepIndex >= 0 && keepIndex < Sections.Count
            ? Sections[keepIndex]
            : Sections[0];
    }

    private async Task RefreshAsync()
    {
        StatusMessage = "Refreshing licenses…";
        await _license.RefreshEntitlementsAsync(CancellationToken.None);
        ConnectivityText = _license.IsOfflineSession ? "Offline (cached)" : "Online";
        BuildSections();
        StatusMessage = "Up to date";
    }
}
