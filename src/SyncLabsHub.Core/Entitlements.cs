using SyncLabsHub.Core.Models;

namespace SyncLabsHub.Core;

/// <summary>Pure helpers for turning trial rows into entitlements and merging sources.</summary>
public static class Entitlements
{
    public const string TrialLicenseType = "trial";

    /// <summary>Maps a free_trials row to an Entitlement (status "active" only when usable).</summary>
    public static Entitlement FromTrial(string toolId, string toolName, DateTime? expiresUtc, bool isActive, bool convertedToPaid)
        => new Entitlement
        {
            ToolId = toolId,
            ToolName = toolName,
            LicenseType = TrialLicenseType,
            Status = (isActive && !convertedToPaid) ? "active" : "expired",
            ExpiresAtUtc = expiresUtc
        };

    /// <summary>
    /// Combines paid orders with trials, keeping the single best entitlement per tool:
    /// active beats inactive, paid beats trial, then the furthest expiry wins.
    /// </summary>
    public static List<Entitlement> Merge(IEnumerable<Entitlement> paid, IEnumerable<Entitlement> trials)
        => paid.Concat(trials)
            .GroupBy(e => e.ToolId)
            .Select(g => g
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.LicenseType != TrialLicenseType)
                .ThenByDescending(x => x.ExpiresAtUtc ?? DateTime.MaxValue)
                .First())
            .ToList();
}
