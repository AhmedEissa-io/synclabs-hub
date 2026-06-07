using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SyncLabsHub.Core.Models;

namespace SyncLabsHub.Core.Services;

/// <summary>
/// Admin-only Supabase calls. All methods use the signed-in admin's JWT, so they
/// only work when the Supabase database has appropriate admin-level RLS policies:
///
///   -- Read all profiles:
///   CREATE POLICY "Admin reads profiles" ON profiles FOR SELECT
///     USING (EXISTS(SELECT 1 FROM user_roles WHERE user_id = auth.uid() AND role = 'admin'));
///
///   -- Read all orders:
///   CREATE POLICY "Admin reads orders" ON subscription_orders FOR SELECT
///     USING (user_id = auth.uid() OR
///            EXISTS(SELECT 1 FROM user_roles WHERE user_id = auth.uid() AND role = 'admin'));
///
///   -- Update any order:
///   CREATE POLICY "Admin updates orders" ON subscription_orders FOR UPDATE
///     USING (EXISTS(SELECT 1 FROM user_roles WHERE user_id = auth.uid() AND role = 'admin'));
///
///   -- Insert order for any user:
///   CREATE POLICY "Admin inserts orders" ON subscription_orders FOR INSERT
///     WITH CHECK (EXISTS(SELECT 1 FROM user_roles WHERE user_id = auth.uid() AND role = 'admin'));
/// </summary>
public sealed class SupabaseAdminClient
{
    // Shared client — safe to share across instances (thread-safe).
    private static readonly HttpClient Http = new HttpClient
    {
        BaseAddress = new Uri(SyncLabsConfig.SupabaseUrl),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private static readonly HttpMethod PatchMethod = new HttpMethod("PATCH");

    // ── Request builders ──────────────────────────────────────────────────────

    private static HttpRequestMessage Get(string path, string token)
    {
        var r = new HttpRequestMessage(HttpMethod.Get, path);
        r.Headers.TryAddWithoutValidation("apikey", SyncLabsConfig.SupabaseAnonKey);
        r.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
        return r;
    }

    private static HttpRequestMessage Patch(string path, string token, object body)
    {
        var r = new HttpRequestMessage(PatchMethod, path);
        r.Headers.TryAddWithoutValidation("apikey", SyncLabsConfig.SupabaseAnonKey);
        r.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
        r.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        r.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        return r;
    }

    private static HttpRequestMessage Post(string path, string token, object body)
    {
        var r = new HttpRequestMessage(HttpMethod.Post, path);
        r.Headers.TryAddWithoutValidation("apikey", SyncLabsConfig.SupabaseAnonKey);
        r.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
        r.Headers.TryAddWithoutValidation("Prefer", "return=minimal");
        r.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        return r;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all users from the profiles table, their roles, and all subscription_orders.
    /// Throws <see cref="InvalidOperationException"/> if the admin RLS policies are missing
    /// (the profiles query will return a non-success status).
    /// </summary>
    public async Task<List<AdminUserSummary>> GetAllUsersAsync(string accessToken, CancellationToken ct)
    {
        var users = new List<AdminUserSummary>();

        // ── 1. Profiles ───────────────────────────────────────────────────────
        // NOTE: profiles.id = the table's own UUID; auth user id is in profiles.user_id
        using (var req = Get("/rest/v1/profiles?select=user_id,email,full_name&order=email.asc", accessToken))
        using (var resp = await Http.SendAsync(req, ct).ConfigureAwait(false))
        {
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new InvalidOperationException(
                    string.Format(
                        "Admin profiles query failed ({0}). " +
                        "Add admin-level SELECT RLS to the 'profiles' table. " +
                        "Server: {1}",
                        (int)resp.StatusCode, body));
            }

            var arr = JArray.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
            foreach (var r in arr)
            {
                users.Add(new AdminUserSummary
                {
                    UserId   = (string?)r["user_id"]   ?? "",
                    Email    = (string?)r["email"]     ?? "",
                    FullName = (string?)r["full_name"] ?? ""
                });
            }
        }

        if (users.Count == 0) return users;

        var userMap = new Dictionary<string, AdminUserSummary>();
        foreach (var u in users)
            if (!string.IsNullOrEmpty(u.UserId))
                userMap[u.UserId] = u;

        // ── 2. Roles — mark admins ────────────────────────────────────────────
        try
        {
            using var req = Get("/rest/v1/user_roles?select=user_id,role", accessToken);
            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var arr = JArray.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
                foreach (var r in arr)
                {
                    var uid  = (string?)r["user_id"] ?? "";
                    var role = (string?)r["role"]    ?? "";
                    if (role == "admin" && userMap.TryGetValue(uid, out var u))
                        u.IsAdmin = true;
                }
            }
        }
        catch { /* roles are best-effort */ }

        // ── 3. All subscription orders (admin sees across all users) ──────────
        try
        {
            const string ordersPath =
                "/rest/v1/subscription_orders" +
                "?select=user_id,tool_id,tool_name,license_type,status,expires_at" +
                "&order=user_id.asc";

            using var req = Get(ordersPath, accessToken);
            using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                var arr = JArray.Parse(await resp.Content.ReadAsStringAsync().ConfigureAwait(false));
                foreach (var r in arr)
                {
                    var uid = (string?)r["user_id"] ?? "";
                    if (!userMap.TryGetValue(uid, out var user)) continue;

                    user.Orders.Add(new AdminOrderRow
                    {
                        UserId      = uid,
                        ToolId      = (string?)r["tool_id"]      ?? "",
                        ToolName    = (string?)r["tool_name"]    ?? "",
                        LicenseType = (string?)r["license_type"] ?? "",
                        Status      = (string?)r["status"]       ?? "",
                        ExpiresAtUtc = ((DateTimeOffset?)r["expires_at"])?.UtcDateTime
                    });
                }
            }
        }
        catch { /* orders are best-effort */ }

        return users;
    }

    /// <summary>
    /// Locks an order by setting its status to "cancelled".
    /// Does NOT touch expires_at.
    /// </summary>
    public async Task<bool> LockToolAsync(
        string accessToken,
        string userId,
        string toolId,
        CancellationToken ct)
    {
        var path = string.Format(
            "/rest/v1/subscription_orders?user_id=eq.{0}&tool_id=eq.{1}",
            Uri.EscapeDataString(userId),
            Uri.EscapeDataString(toolId));

        using var req = Patch(path, accessToken, new { status = "cancelled" });
        using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// Restores an order to "active" and sets (or clears) its expires_at.
    /// Pass <paramref name="expiresAt"/> = null for perpetual access.
    /// </summary>
    public async Task<bool> UnlockToolAsync(
        string accessToken,
        string userId,
        string toolId,
        DateTime? expiresAt,
        CancellationToken ct)
    {
        var path = string.Format(
            "/rest/v1/subscription_orders?user_id=eq.{0}&tool_id=eq.{1}",
            Uri.EscapeDataString(userId),
            Uri.EscapeDataString(toolId));

        // Build patch body dynamically so expires_at is explicitly null-able
        var body = new JObject
        {
            ["status"]     = "active",
            ["expires_at"] = expiresAt.HasValue
                ? new JValue(expiresAt.Value.ToUniversalTime().ToString("o"))
                : JValue.CreateNull()
        };

        using var req = Patch(path, accessToken, body);
        using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// Grants a tool to a user by inserting a new active subscription_orders row.
    /// Satisfies all NOT NULL constraints: price_egp must be > 0 (DB CHECK),
    /// license_type must be one of: individual | teams | enterprise (DB CHECK).
    /// Pass <paramref name="expiresAt"/> = null for perpetual access.
    /// </summary>
    public async Task<bool> GrantToolAsync(
        string accessToken,
        string userId,
        string toolId,
        string toolName,
        string userDisplayName,
        string userEmail,
        DateTime? expiresAt,
        CancellationToken ct)
    {
        // Use JObject so expires_at can be explicitly null (anonymous type serializes null fine too,
        // but JObject makes the intent explicit and avoids Newtonsoft dropping null fields).
        var body = new JObject
        {
            ["user_id"]        = userId,
            ["tool_id"]        = toolId,
            ["tool_name"]      = toolName,
            ["license_type"]   = "individual",  // must match DB CHECK (individual|teams|enterprise)
            ["seats"]          = 1,
            ["price_egp"]      = 500,            // must be > 0 per DB CHECK; trigger early-exits for admins
            ["customer_name"]  = userDisplayName,
            ["customer_email"] = userEmail,
            ["customer_phone"] = "N/A",          // NOT NULL placeholder for admin grants
            ["payment_method"] = "admin",
            ["status"]         = "active",
            ["expires_at"]     = expiresAt.HasValue
                ? new JValue(expiresAt.Value.ToUniversalTime().ToString("o"))
                : JValue.CreateNull()
        };

        using var req = Post("/rest/v1/subscription_orders", accessToken, body);
        using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        return resp.IsSuccessStatusCode;
    }
}
