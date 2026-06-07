using Newtonsoft.Json;
using SyncLabsHub.Core;
using SyncLabsHub.Core.Models;
using Xunit;

namespace SyncLabsHub.Core.Tests;

public class EntitlementTests
{
    [Theory]
    [InlineData("active", 10, true)]
    [InlineData("confirmed", 10, true)]
    [InlineData("active", -1, false)]      // expired
    [InlineData("cancelled", 10, false)]   // wrong status
    [InlineData("pending", 10, false)]
    public void IsActive_respects_status_and_expiry(string status, int daysFromNow, bool expected)
    {
        var e = new Entitlement
        {
            ToolId = "t",
            Status = status,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(daysFromNow)
        };
        Assert.Equal(expected, e.IsActive);
    }

    [Fact]
    public void IsActive_true_when_active_and_no_expiry()
    {
        var e = new Entitlement { ToolId = "t", Status = "active", ExpiresAtUtc = null };
        Assert.True(e.IsActive);
    }

    [Fact]
    public void DaysRemaining_is_null_for_perpetual_and_positive_for_future()
    {
        Assert.Null(new Entitlement { ExpiresAtUtc = null }.DaysRemaining);

        var d = new Entitlement { ExpiresAtUtc = DateTime.UtcNow.AddDays(5).AddHours(1) }.DaysRemaining;
        Assert.NotNull(d);
        Assert.InRange(d!.Value, 5, 6);
    }
}

public class AuthSessionTests
{
    [Fact]
    public void IsExpired_true_within_the_one_minute_buffer()
    {
        Assert.True(new AuthSession { ExpiresAtUtc = DateTime.UtcNow.AddSeconds(30) }.IsExpired);
        Assert.True(new AuthSession { ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5) }.IsExpired);
    }

    [Fact]
    public void IsExpired_false_when_comfortably_in_future()
    {
        Assert.False(new AuthSession { ExpiresAtUtc = DateTime.UtcNow.AddHours(1) }.IsExpired);
    }
}

public class TrialTests
{
    [Fact]
    public void FromTrial_active_when_live_and_not_converted()
    {
        var e = Entitlements.FromTrial("t", "Tool", DateTime.UtcNow.AddDays(3), isActive: true, convertedToPaid: false);
        Assert.Equal(Entitlements.TrialLicenseType, e.LicenseType);
        Assert.True(e.IsActive);
    }

    [Theory]
    [InlineData(false, false)] // inactive
    [InlineData(true, true)]   // converted to paid
    public void FromTrial_not_active_when_inactive_or_converted(bool isActive, bool converted)
    {
        var e = Entitlements.FromTrial("t", "Tool", DateTime.UtcNow.AddDays(3), isActive, converted);
        Assert.False(e.IsActive);
    }
}

public class MergeTests
{
    private static Entitlement Paid(string tool, int days, string status = "active") =>
        new() { ToolId = tool, LicenseType = "individual", Status = status, ExpiresAtUtc = DateTime.UtcNow.AddDays(days) };

    private static Entitlement Trial(string tool, int days) =>
        Entitlements.FromTrial(tool, tool, DateTime.UtcNow.AddDays(days), true, false);

    [Fact]
    public void Paid_beats_trial_when_both_active()
    {
        var merged = Entitlements.Merge(new[] { Paid("dbp", 30) }, new[] { Trial("dbp", 5) });
        var only = Assert.Single(merged);
        Assert.NotEqual(Entitlements.TrialLicenseType, only.LicenseType);
    }

    [Fact]
    public void Active_trial_wins_over_expired_paid()
    {
        var merged = Entitlements.Merge(new[] { Paid("dbp", -1) }, new[] { Trial("dbp", 5) });
        var only = Assert.Single(merged);
        Assert.Equal(Entitlements.TrialLicenseType, only.LicenseType);
        Assert.True(only.IsActive);
    }

    [Fact]
    public void Keeps_one_row_per_tool_and_furthest_expiry()
    {
        var merged = Entitlements.Merge(new[] { Paid("dbp", 10), Paid("dbp", 90) }, Array.Empty<Entitlement>());
        var only = Assert.Single(merged);
        Assert.InRange(only.DaysRemaining!.Value, 89, 90);
    }

    [Fact]
    public void Distinct_tools_are_all_kept()
    {
        var merged = Entitlements.Merge(new[] { Paid("a", 10) }, new[] { Trial("b", 5) });
        Assert.Equal(2, merged.Count);
    }
}

public class CachedSessionContractTests
{
    [Fact]
    public void Round_trips_through_json_preserving_the_on_disk_contract()
    {
        var original = new CachedSession
        {
            Session = new AuthSession
            {
                AccessToken = "acc", RefreshToken = "ref", UserId = "u1",
                Email = "a@b.com", ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
            },
            Profile = new UserProfile { FullName = "Ahmed", Email = "a@b.com", IsAdmin = true },
            Entitlements =
            {
                new Entitlement { ToolId = "parameter-sync", ToolName = "Parameter Sync",
                    LicenseType = "individual", Status = "active",
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(20) }
            },
            CachedAtUtc = DateTime.UtcNow
        };

        var json = JsonConvert.SerializeObject(original, Formatting.Indented);
        var restored = JsonConvert.DeserializeObject<CachedSession>(json)!;

        Assert.Equal("ref", restored.Session.RefreshToken);
        Assert.True(restored.Profile.IsAdmin);
        var e = Assert.Single(restored.Entitlements);
        Assert.Equal("parameter-sync", e.ToolId);
        Assert.True(e.IsActive);
    }
}
