using System.Net.Http;
using Newtonsoft.Json.Linq;
using SyncLabsHub.Core.Models;

namespace SyncLabsHub.Core.Services;

/// <summary>Reads entitlements, trials, profile and role from Supabase PostgREST (RLS-scoped to the caller).</summary>
public sealed class SupabaseDataClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri(SyncLabsConfig.SupabaseUrl),
        Timeout = TimeSpan.FromSeconds(20)
    };

    private static HttpRequestMessage Build(string path, string accessToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.TryAddWithoutValidation("apikey", SyncLabsConfig.SupabaseAnonKey);
        req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);
        return req;
    }

    public async Task<List<Entitlement>> GetEntitlementsAsync(string accessToken, CancellationToken ct)
    {
        const string path =
            "/rest/v1/subscription_orders" +
            "?select=tool_id,tool_name,license_type,status,expires_at" +
            "&status=in.(active,confirmed)" +
            "&order=expires_at.desc.nullslast";

        using var req = Build(path, accessToken);
        using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return new List<Entitlement>();

        var arr = JArray.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
        var rows = arr.Select(r => new Entitlement
        {
            ToolId = (string?)r["tool_id"] ?? "",
            ToolName = (string?)r["tool_name"] ?? "",
            LicenseType = (string?)r["license_type"] ?? "",
            Status = (string?)r["status"] ?? "",
            ExpiresAtUtc = ((DateTimeOffset?)r["expires_at"])?.UtcDateTime
        });

        // One row per tool — keep the furthest-out expiry (best entitlement).
        return rows
            .GroupBy(e => e.ToolId)
            .Select(g => g.OrderByDescending(x => x.ExpiresAtUtc ?? DateTime.MaxValue).First())
            .ToList();
    }

    /// <summary>Active, unconverted free trials → entitlements (status "active" while valid).</summary>
    public async Task<List<Entitlement>> GetTrialsAsync(string accessToken, CancellationToken ct)
    {
        const string path =
            "/rest/v1/free_trials" +
            "?select=tool_id,tool_name,expires_at,is_active,converted_to_paid" +
            "&is_active=eq.true&converted_to_paid=eq.false";

        using var req = Build(path, accessToken);
        using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return new List<Entitlement>();

        var arr = JArray.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
        return arr.Select(r => Entitlements.FromTrial(
                (string?)r["tool_id"] ?? "",
                (string?)r["tool_name"] ?? "",
                ((DateTimeOffset?)r["expires_at"])?.UtcDateTime,
                (bool?)r["is_active"] ?? false,
                (bool?)r["converted_to_paid"] ?? false))
            .ToList();
    }

    public async Task<UserProfile> GetProfileAsync(string accessToken, string fallbackEmail, CancellationToken ct)
    {
        var profile = new UserProfile { Email = fallbackEmail, FullName = DeriveName(fallbackEmail) };

        try
        {
            using var req = Build("/rest/v1/profiles?select=full_name,email&limit=1", accessToken);
            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var arr = JArray.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
                if (arr.Count > 0)
                {
                    var fn = (string?)arr[0]["full_name"];
                    if (!string.IsNullOrWhiteSpace(fn)) profile.FullName = fn!;
                    profile.Email = (string?)arr[0]["email"] ?? profile.Email;
                }
            }
        }
        catch { /* profile is best-effort */ }

        try
        {
            using var req = Build("/rest/v1/user_roles?select=role", accessToken);
            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var arr = JArray.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
                profile.IsAdmin = arr.Any(r => (string?)r["role"] == "admin");
            }
        }
        catch { /* role is best-effort */ }

        return profile;
    }

    private static string DeriveName(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "User";
        var at = email.IndexOf('@');
        var local = at > 0 ? email.Substring(0, at) : email;
        if (local.Length == 0) return "User";
        return char.ToUpper(local[0]) + (local.Length > 1 ? local.Substring(1) : "");
    }
}
