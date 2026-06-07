using System.Collections.ObjectModel;
using System.Windows;
using SyncLabsHub.App.Services;
using SyncLabsHub.Core;
using SyncLabsHub.Core.Models;
using SyncLabsHub.Core.Services;

namespace SyncLabsHub.App.ViewModels;

public sealed class MyToolsViewModel : SectionViewModel
{
    private readonly List<ToolTileViewModel> _all;
    private string _search = "";

    public override string Name => "My Tools";
    public override string NavGlyph => "Apps24";

    public ObservableCollection<ToolTileViewModel> Licensed { get; } = new();
    public ObservableCollection<ToolTileViewModel> Available { get; } = new();

    public MyToolsViewModel(LicenseService license)
    {
        _all = ToolCatalog.All
            .Select(e => new ToolTileViewModel(e, license.GetEntitlement(e.Id), OnPrimary))
            .ToList();
        Apply();
    }

    public string SearchText
    {
        get => _search;
        set { if (SetProperty(ref _search, value)) Apply(); }
    }

    public string Subtitle => $"{_all.Count(t => t.IsLicensed)} of {_all.Count} tools licensed";
    public bool HasLicensed => Licensed.Count > 0;
    public bool HasAvailable => Available.Count > 0;
    public bool HasNoResults => Licensed.Count == 0 && Available.Count == 0;

    private void Apply()
    {
        var q = _search.Trim();

        bool Match(ToolTileViewModel t) =>
            q.Length == 0 ||
            t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.Entry.Id.Contains(q, StringComparison.OrdinalIgnoreCase);

        Licensed.Clear();
        foreach (var t in _all.Where(t => t.IsLicensed && Match(t))) Licensed.Add(t);

        Available.Clear();
        foreach (var t in _all.Where(t => !t.IsLicensed && Match(t))) Available.Add(t);

        OnPropertyChanged(nameof(HasLicensed));
        OnPropertyChanged(nameof(HasAvailable));
        OnPropertyChanged(nameof(HasNoResults));
        OnPropertyChanged(nameof(Subtitle));
    }

    private void OnPrimary(ToolTileViewModel tile)
    {
        if (tile.IsLocked)
        {
            Launcher.OpenUrl(SyncLabsConfig.ProductUrl(tile.Entry.Id));
            return;
        }

        if (tile.Entry.Kind == ToolKind.DesktopApp)
        {
            Launcher.LaunchTool(tile.Entry.Id, tile.Entry.Name);
            return;
        }

        var host = tile.Entry.Platforms.FirstOrDefault() ?? "its host application";
        MessageBox.Show(
            $"{tile.Entry.Name} runs inside {host}.\n\nOpen {host} and use the SyncLabs panel — your license is active and will be validated automatically.",
            "SyncLabs Hub", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
