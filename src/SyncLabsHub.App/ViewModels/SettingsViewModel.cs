using SyncLabsHub.Core;
using SyncLabsHub.Core.Services;

namespace SyncLabsHub.App.ViewModels;

public sealed class SettingsViewModel : SectionViewModel
{
    public override string Name => "Settings";
    public override string NavGlyph => "Settings24";

    public SettingsViewModel(LicenseService license)
    {
        var session = license.Current;
        FullName = string.IsNullOrWhiteSpace(session?.Profile.FullName)
            ? (session?.Profile.Email ?? "—")
            : session!.Profile.FullName;
        Email = session?.Profile.Email ?? "—";
        RoleText = session?.Profile.IsAdmin == true ? "Administrator" : "Member";
        ConnectionText = license.IsOfflineSession ? "Offline — using cached license" : "Online";
        DataFolder = SyncLabsConfig.DataFolder;
    }

    public string FullName { get; }
    public string Email { get; }
    public string RoleText { get; }
    public string ConnectionText { get; }
    public string DataFolder { get; }
}
