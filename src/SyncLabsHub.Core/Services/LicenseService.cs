using SyncLabsHub.Core.Models;

namespace SyncLabsHub.Core.Services;

/// <summary>
/// The single entry point the Hub (and tools) use for licensing: login, silent restore
/// with offline grace, refresh, and per-tool entitlement checks.
/// </summary>
public sealed class LicenseService
{
    private readonly SupabaseAuthClient _auth = new();
    private readonly SupabaseDataClient _data = new();
    private readonly TokenStore _store = new();

    public CachedSession? Current { get; private set; }

    /// <summary>True when running on a cached session because the network was unreachable.</summary>
    public bool IsOfflineSession { get; private set; }

    public async Task<CachedSession> LoginAsync(string email, string password, CancellationToken ct)
    {
        var session = await _auth.LoginAsync(email, password, ct).ConfigureAwait(false);
        var cached = await BuildAsync(session, ct).ConfigureAwait(false);
        IsOfflineSession = false;
        Current = cached;
        _store.Save(cached);
        return cached;
    }

    /// <summary>
    /// Attempts to resume the last session without prompting. Refreshes online when possible;
    /// otherwise falls back to the cached entitlements within the offline grace window.
    /// Returns null when the user must sign in again.
    /// </summary>
    public async Task<CachedSession?> TryRestoreAsync(CancellationToken ct)
    {
        var cached = _store.Load();
        if (cached is null || string.IsNullOrEmpty(cached.Session.RefreshToken))
            return null;

        try
        {
            var session = cached.Session.IsExpired
                ? await _auth.RefreshAsync(cached.Session.RefreshToken, ct).ConfigureAwait(false)
                : cached.Session;

            var fresh = await BuildAsync(session, ct).ConfigureAwait(false);
            IsOfflineSession = false;
            Current = fresh;
            _store.Save(fresh);
            return fresh;
        }
        catch
        {
            if ((DateTime.UtcNow - cached.CachedAtUtc).TotalDays <= SyncLabsConfig.OfflineGraceDays)
            {
                IsOfflineSession = true;
                Current = cached;
                return cached;
            }
            return null;
        }
    }

    public async Task RefreshEntitlementsAsync(CancellationToken ct)
    {
        if (Current is null) return;
        try
        {
            var session = Current.Session.IsExpired
                ? await _auth.RefreshAsync(Current.Session.RefreshToken, ct).ConfigureAwait(false)
                : Current.Session;

            var fresh = await BuildAsync(session, ct).ConfigureAwait(false);
            IsOfflineSession = false;
            Current = fresh;
            _store.Save(fresh);
        }
        catch
        {
            IsOfflineSession = true;
        }
    }

    private async Task<CachedSession> BuildAsync(AuthSession session, CancellationToken ct)
    {
        var profile = await _data.GetProfileAsync(session.AccessToken, session.Email, ct).ConfigureAwait(false);
        var paid = await _data.GetEntitlementsAsync(session.AccessToken, ct).ConfigureAwait(false);
        var trials = await _data.GetTrialsAsync(session.AccessToken, ct).ConfigureAwait(false);
        return new CachedSession
        {
            Session = session,
            Profile = profile,
            Entitlements = Entitlements.Merge(paid, trials),
            CachedAtUtc = DateTime.UtcNow
        };
    }

    public void Logout()
    {
        _store.Clear();
        Current = null;
        IsOfflineSession = false;
    }

    public Entitlement? GetEntitlement(string toolId) =>
        Current?.Entitlements.FirstOrDefault(e => string.Equals(e.ToolId, toolId, StringComparison.OrdinalIgnoreCase));

    public bool HasActive(string toolId) => GetEntitlement(toolId)?.IsActive == true;
}
