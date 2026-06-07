using System.Collections.ObjectModel;
using System.Windows.Media;
using SyncLabsHub.App.Mvvm;
using SyncLabsHub.Core;
using SyncLabsHub.Core.Models;
using SyncLabsHub.Core.Services;

namespace SyncLabsHub.App.ViewModels;

public sealed class LicensesViewModel : SectionViewModel
{
    public override string Name => "My Licenses";
    public override string NavGlyph => "Receipt24";

    public ObservableCollection<LicenseRowViewModel> Rows { get; } = new();

    public LicensesViewModel(LicenseService license)
    {
        var entitlements = license.Current?.Entitlements ?? new List<Entitlement>();
        foreach (var e in entitlements.OrderBy(x => x.ExpiresAtUtc ?? DateTime.MaxValue))
            Rows.Add(new LicenseRowViewModel(e));
    }

    public bool HasLicenses => Rows.Count > 0;
    public string Subtitle => Rows.Count == 0
        ? "You don't own any licenses yet"
        : $"{Rows.Count} active license{(Rows.Count == 1 ? "" : "s")}";
}

public sealed class LicenseRowViewModel : ObservableObject
{
    private static readonly Brush Green = Freeze(Color.FromRgb(0x4A, 0xDE, 0x80));
    private static readonly Brush Amber = Freeze(Color.FromRgb(0xF5, 0x9E, 0x0B));

    private readonly Entitlement _entitlement;

    public LicenseRowViewModel(Entitlement entitlement) => _entitlement = entitlement;

    public string ToolName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_entitlement.ToolName)) return _entitlement.ToolName;
            return ToolCatalog.ById(_entitlement.ToolId)?.Name ?? _entitlement.ToolId;
        }
    }

    public string LicenseType => Capitalize(_entitlement.LicenseType);
    public string StatusText => Capitalize(_entitlement.Status);

    public string ExpiryText => _entitlement.ExpiresAtUtc is { } exp
        ? exp.ToLocalTime().ToString("dd MMM yyyy")
        : "No expiry";

    public string RemainingText => _entitlement.DaysRemaining is int d
        ? (d <= 0 ? "Expired" : $"{d} day{(d == 1 ? "" : "s")} left")
        : "Perpetual";

    public Brush AccentBrush =>
        _entitlement.DaysRemaining is int d && d <= 7 ? Amber : Green;

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? "—" : char.ToUpper(s[0]) + (s.Length > 1 ? s[1..] : "");

    private static Brush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
