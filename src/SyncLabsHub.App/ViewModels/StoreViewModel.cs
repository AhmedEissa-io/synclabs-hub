using System.Collections.ObjectModel;
using SyncLabsHub.App.Services;
using SyncLabsHub.Core;
using SyncLabsHub.Core.Services;

namespace SyncLabsHub.App.ViewModels;

public sealed class StoreViewModel : SectionViewModel
{
    private readonly List<ToolTileViewModel> _all;
    private string _search = "";

    public override string Name => "Store";
    public override string NavGlyph => "Cart24";

    public ObservableCollection<ToolTileViewModel> Tiles { get; } = new();

    public StoreViewModel(LicenseService license)
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

    public string Subtitle => $"{_all.Count} tools available";
    public bool HasNoMatches => Tiles.Count == 0;

    private void Apply()
    {
        var q = _search.Trim();
        Tiles.Clear();
        foreach (var t in _all.Where(t =>
                     q.Length == 0 ||
                     t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     t.Entry.Id.Contains(q, StringComparison.OrdinalIgnoreCase)))
            Tiles.Add(t);

        OnPropertyChanged(nameof(HasNoMatches));
    }

    private void OnPrimary(ToolTileViewModel tile)
    {
        // From the store, every card routes to its product page.
        Launcher.OpenUrl(SyncLabsConfig.ProductUrl(tile.Entry.Id));
    }
}
