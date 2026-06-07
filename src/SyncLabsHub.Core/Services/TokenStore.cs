using Newtonsoft.Json;
using SyncLabsHub.Core.Models;

namespace SyncLabsHub.Core.Services;

/// <summary>Reads/writes the shared auth.json under %AppData%\SyncLabs.</summary>
public sealed class TokenStore
{
    public CachedSession? Load()
    {
        try
        {
            if (!File.Exists(SyncLabsConfig.AuthFilePath))
                return null;
            var json = File.ReadAllText(SyncLabsConfig.AuthFilePath);
            return JsonConvert.DeserializeObject<CachedSession>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(CachedSession session)
    {
        try
        {
            Directory.CreateDirectory(SyncLabsConfig.DataFolder);
            var json = JsonConvert.SerializeObject(session, Formatting.Indented);
            File.WriteAllText(SyncLabsConfig.AuthFilePath, json);
        }
        catch
        {
            // best-effort; the session still works in-memory for this run
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(SyncLabsConfig.AuthFilePath))
                File.Delete(SyncLabsConfig.AuthFilePath);
        }
        catch
        {
            // ignore
        }
    }
}
