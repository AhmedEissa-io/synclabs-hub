using System.Windows.Input;
using System.Windows.Media;
using SyncLabsHub.App.Mvvm;
using SyncLabsHub.Core;
using SyncLabsHub.Core.Models;
using Wpf.Ui.Controls;

namespace SyncLabsHub.App.ViewModels;

/// <summary>One tool card — combines a catalog entry with the user's entitlement (if any).</summary>
public sealed class ToolTileViewModel : ObservableObject
{
    private static readonly Brush ActiveBrush = Freeze(Color.FromRgb(0x4A, 0xDE, 0x80));
    private static readonly Brush ExpiringBrush = Freeze(Color.FromRgb(0xF5, 0x9E, 0x0B));
    private static readonly Brush LockedBrush = Freeze(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));

    public ToolCatalogEntry Entry { get; }
    public Entitlement? License { get; }

    public ToolTileViewModel(ToolCatalogEntry entry, Entitlement? license, Action<ToolTileViewModel> onPrimary)
    {
        Entry = entry;
        License = license;
        PrimaryCommand = new RelayCommand(() => onPrimary(this));
    }

    public ICommand PrimaryCommand { get; }

    public string Name => Entry.Name;
    public string Tagline => Entry.Tagline;
    public string KindLabel => Entry.KindLabel;
    public string Glyph => Entry.Glyph;

    public bool IsLicensed => License?.IsActive == true;
    public bool IsLocked => !IsLicensed;
    public bool IsTrial => License?.LicenseType == Entitlements.TrialLicenseType;
    public bool IsExpiringSoon => IsLicensed && License!.DaysRemaining is int d && d <= 7;
    public double TileOpacity => IsLocked ? 0.55 : 1.0;

    public Brush StatusBrush => IsLocked ? LockedBrush : (IsExpiringSoon ? ExpiringBrush : ActiveBrush);

    public string StatusText
    {
        get
        {
            if (IsLocked) return "Locked";
            if (License!.DaysRemaining is int d && d <= 7)
                return IsTrial ? $"Trial · {d}d left" : $"Expires in {d} day{(d == 1 ? "" : "s")}";
            return IsTrial ? "Trial" : "Active";
        }
    }

    public string PrimaryButtonText
    {
        get
        {
            if (IsLocked) return "Unlock";
            return Entry.Kind == ToolKind.DesktopApp ? "Launch" : "Installed";
        }
    }

    /// <summary>Show the starting price only on locked tiles, as a buy nudge.</summary>
    public bool ShowPrice => IsLocked;

    public string PriceText => $"From {SyncLabsConfig.IndividualPriceEgp} EGP / mo";

    /// <summary>Accent the button only for the actionable case (launch a licensed desktop app).</summary>
    public ControlAppearance PrimaryAppearance =>
        (IsLicensed && Entry.Kind == ToolKind.DesktopApp)
            ? ControlAppearance.Primary
            : ControlAppearance.Secondary;

    private static Brush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
