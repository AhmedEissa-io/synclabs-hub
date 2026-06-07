namespace SyncLabsHub.App.ViewModels;

public sealed class UpdatesViewModel : SectionViewModel
{
    public override string Name => "Updates";
    public override string NavGlyph => "ArrowDownload24";

    public string Headline => "You're up to date";
    public string Detail => "All installed SyncLabs tools are on their latest versions. " +
                            "New releases will appear here automatically.";
}
