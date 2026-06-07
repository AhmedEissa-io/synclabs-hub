using Newtonsoft.Json;

namespace SyncLabsHub.Core.Models;

/// <summary>A Supabase auth session (GoTrue tokens + identity).</summary>
public sealed class AuthSession
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>True a minute before the real expiry, so we refresh proactively.</summary>
    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc.AddMinutes(-1);
}

/// <summary>An owned tool, derived from a confirmed/active row in subscription_orders.</summary>
public sealed class Entitlement
{
    public string ToolId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string LicenseType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsActive =>
        (Status == "active" || Status == "confirmed") &&
        (ExpiresAtUtc == null || ExpiresAtUtc > DateTime.UtcNow);

    public int? DaysRemaining =>
        ExpiresAtUtc is null ? null : (int)Math.Ceiling((ExpiresAtUtc.Value - DateTime.UtcNow).TotalDays);
}

public sealed class UserProfile
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsAdmin { get; set; }
}

/// <summary>
/// The on-disk shape of auth.json — the contract the Hub writes and every tool reads.
/// </summary>
public sealed class CachedSession
{
    public int SchemaVersion { get; set; } = 1;
    public AuthSession Session { get; set; } = new();
    public UserProfile Profile { get; set; } = new();
    public List<Entitlement> Entitlements { get; set; } = new();
    public DateTime CachedAtUtc { get; set; } = DateTime.UtcNow;
}

// ── Admin models ─────────────────────────────────────────────────────────────

/// <summary>
/// One user as seen by an admin — includes all their subscription_orders rows.
/// Populated by SupabaseAdminClient; requires admin-level SELECT RLS on:
///   profiles, user_roles, subscription_orders.
/// </summary>
public sealed class AdminUserSummary
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public bool IsAdmin { get; set; }
    public List<AdminOrderRow> Orders { get; set; } = new List<AdminOrderRow>();
}

/// <summary>A subscription_orders row as seen by an admin (includes user_id and all statuses).</summary>
public sealed class AdminOrderRow
{
    public string UserId { get; set; } = "";
    public string ToolId { get; set; } = "";
    public string ToolName { get; set; } = "";
    public string LicenseType { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? ExpiresAtUtc { get; set; }

    public bool IsActive =>
        (Status == "active" || Status == "confirmed") &&
        (ExpiresAtUtc == null || ExpiresAtUtc > DateTime.UtcNow);
}

public enum ToolKind
{
    RevitPlugin,
    RhinoPlugin,
    DesktopApp
}

/// <summary>A product in the catalog (mirrors the website's tools list).</summary>
public sealed class ToolCatalogEntry
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Tagline { get; init; } = "";
    public string[] Platforms { get; init; } = Array.Empty<string>();
    public ToolKind Kind { get; init; }

    /// <summary>WPF-UI SymbolRegular name, resolved at the UI layer (falls back gracefully).</summary>
    public string Glyph { get; init; } = "Apps24";

    public string KindLabel => Kind switch
    {
        ToolKind.RevitPlugin => "Revit Plugin",
        ToolKind.RhinoPlugin => "Rhino Plugin",
        _ => "Desktop App"
    };
}
