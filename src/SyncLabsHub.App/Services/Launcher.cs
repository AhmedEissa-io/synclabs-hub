using System.Diagnostics;
using System.IO;
using System.Windows;
using Newtonsoft.Json.Linq;
using SyncLabsHub.Core;

namespace SyncLabsHub.App.Services;

/// <summary>Opens URLs and launches installed desktop tools recorded in installed.json.</summary>
public static class Launcher
{
    public static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* nothing we can do if the shell refuses */ }
    }

    public static void LaunchTool(string toolId, string toolName)
    {
        try
        {
            if (File.Exists(SyncLabsConfig.InstalledFilePath))
            {
                var installed = JObject.Parse(File.ReadAllText(SyncLabsConfig.InstalledFilePath));
                var exe = (string?)installed[toolId]?["exe_path"];
                if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
                {
                    Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
                    return;
                }
            }
        }
        catch { /* fall through to the not-installed message */ }

        MessageBox.Show(
            $"{toolName} isn't installed on this machine yet.\n\nInstall it from the Store, then launch it from here.",
            "SyncLabs Hub", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
