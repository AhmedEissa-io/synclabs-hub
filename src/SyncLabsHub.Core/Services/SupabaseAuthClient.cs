using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using SyncLabsHub.Core.Models;

namespace SyncLabsHub.Core.Services;

public sealed class SyncLabsAuthException : Exception
{
    public SyncLabsAuthException(string message) : base(message) { }
}

/// <summary>Talks to Supabase GoTrue (/auth/v1) for email-password login and token refresh.</summary>
public sealed class SupabaseAuthClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri(SyncLabsConfig.SupabaseUrl),
        Timeout = TimeSpan.FromSeconds(20)
    };

    public Task<AuthSession> LoginAsync(string email, string password, CancellationToken ct)
        => PostTokenAsync("/auth/v1/token?grant_type=password",
            new JObject { ["email"] = email, ["password"] = password }, ct);

    public Task<AuthSession> RefreshAsync(string refreshToken, CancellationToken ct)
        => PostTokenAsync("/auth/v1/token?grant_type=refresh_token",
            new JObject { ["refresh_token"] = refreshToken }, ct);

    private static async Task<AuthSession> PostTokenAsync(string path, JObject body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, path);
        req.Headers.TryAddWithoutValidation("apikey", SyncLabsConfig.SupabaseAnonKey);
        req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

        using var resp = await Http.SendAsync(req, ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            throw new SyncLabsAuthException(ParseError(text, resp.StatusCode));

        return ParseSession(text);
    }

    private static AuthSession ParseSession(string json)
    {
        var o = JObject.Parse(json);
        var expiresIn = (int?)o["expires_in"] ?? 3600;
        var user = o["user"] as JObject;
        return new AuthSession
        {
            AccessToken = (string?)o["access_token"] ?? "",
            RefreshToken = (string?)o["refresh_token"] ?? "",
            UserId = (string?)user?["id"] ?? "",
            Email = (string?)user?["email"] ?? "",
            ExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn)
        };
    }

    private static string ParseError(string json, HttpStatusCode code)
    {
        try
        {
            var o = JObject.Parse(json);
            var msg = (string?)o["error_description"]
                      ?? (string?)o["msg"]
                      ?? (string?)o["error"]
                      ?? (string?)o["message"];
            if (!string.IsNullOrWhiteSpace(msg))
                return msg!;
        }
        catch
        {
            // fall through to generic message
        }

        return code == HttpStatusCode.BadRequest
            ? "Invalid email or password."
            : $"Sign-in failed ({(int)code}).";
    }
}
