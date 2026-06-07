namespace SyncLabsHub.Core;

/// <summary>
/// Central configuration shared by the Hub and every tool that links the Core library.
/// The anon key is the public, RLS-protected Supabase key — safe to ship in a desktop client.
/// </summary>
public static class SyncLabsConfig
{
    public const string SupabaseUrl = "https://xkqptqdyyqfjylqhrsjd.supabase.co";

    public const string SupabaseAnonKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9." +
        "eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InhrcXB0cWR5eXFmanlscWhyc2pkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzAwNjU5ODUsImV4cCI6MjA4NTY0MTk4NX0." +
        "Hw7qAE5tgxtgT0FaFbR6J2CWwo-3ZHMjx2V0AFqILzM";

    /// <summary>Public marketing/checkout site.</summary>
    public const string WebsiteUrl = "https://synclabs.lovable.app";

    /// <summary>Product detail page for a tool (shows plans + checkout).</summary>
    public static string ProductUrl(string toolId) => $"{WebsiteUrl}/products/{toolId}";

    /// <summary>Direct checkout for a tool.</summary>
    public static string CheckoutUrl(string toolId) => $"{WebsiteUrl}/checkout/{toolId}";

    /// <summary>Flat plan pricing (EGP), mirrored from the website's subscription sidebar.</summary>
    public const int IndividualPriceEgp = 500;
    public const int TeamSeatPriceEgp = 300;

    /// <summary>How long a cached license stays valid with no connectivity.</summary>
    public const int OfflineGraceDays = 14;

    /// <summary>
    /// Velopack release feed (a URL or UNC folder of published releases). Empty = updates
    /// disabled. Set this to where `vpk pack` uploads releases (e.g. a static host or GitHub).
    /// </summary>
    public const string UpdateFeedUrl = "";

    /// <summary>%AppData%\SyncLabs — the shared contract location read by every tool.</summary>
    public static string DataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SyncLabs");

    public static string AuthFilePath => Path.Combine(DataFolder, "auth.json");

    public static string InstalledFilePath => Path.Combine(DataFolder, "installed.json");
}
