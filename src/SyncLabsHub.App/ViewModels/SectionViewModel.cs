using SyncLabsHub.App.Mvvm;

namespace SyncLabsHub.App.ViewModels;

/// <summary>Base for every navigable page shown in the content area.</summary>
public abstract class SectionViewModel : ObservableObject
{
    public abstract string Name { get; }

    /// <summary>WPF-UI SymbolRegular name; resolved by StringToSymbolConverter.</summary>
    public abstract string NavGlyph { get; }
}
