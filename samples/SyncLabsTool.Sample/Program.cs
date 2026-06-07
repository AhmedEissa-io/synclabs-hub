using SyncLabsHub.Core;
using SyncLabsHub.Core.Services;

// A stand-in for a real SyncLabs desktop tool / Revit plugin command.
// On startup it validates the license through the SAME Core library and the
// shared %AppData%\SyncLabs\auth.json that the Hub writes on sign-in.
//
// Exit codes:  0 = access granted   1 = denied (no license)   2 = not signed in

var toolId = args.Length > 0 ? args[0] : "parameter-sync";
var toolName = ToolCatalog.ById(toolId)?.Name ?? toolId;

Console.WriteLine($"SyncLabs Tool — {toolName}");
Console.WriteLine(new string('-', 44));

var license = new LicenseService();
var session = await license.TryRestoreAsync(CancellationToken.None);

if (session is null)
{
    Console.WriteLine("Not signed in.");
    Console.WriteLine("Open SyncLabs Hub, sign in, then relaunch this tool.");
    return 2;
}

Console.WriteLine($"Signed in as {session.Profile.Email}"
    + (license.IsOfflineSession ? "   (offline — using cached license)" : ""));

var entitlement = license.GetEntitlement(toolId);
if (entitlement?.IsActive == true)
{
    var kind = entitlement.LicenseType == Entitlements.TrialLicenseType
        ? "TRIAL"
        : entitlement.LicenseType.ToUpperInvariant();
    var expiry = entitlement.DaysRemaining is int d ? $"{d} day(s) left" : "no expiry";
    Console.WriteLine($"ACCESS GRANTED — {kind}, {expiry}.");
    Console.WriteLine($"Launching {toolName}…");
    return 0;
}

Console.WriteLine($"ACCESS DENIED — no active license for {toolName}.");
Console.WriteLine($"Unlock it: {SyncLabsConfig.ProductUrl(toolId)}");
return 1;
