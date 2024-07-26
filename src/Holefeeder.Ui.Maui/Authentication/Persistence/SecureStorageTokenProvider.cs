using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

using Holefeeder.Ui.Common.Authentication;

namespace Holefeeder.Ui.Maui.Authentication.Persistence;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class SecureStorageTokenProvider : ITokenProvider
{
    private const string IdTokenKey = "MauiAuth_IdTokens";
    private const string AccessTokenKey = "MauiAuth_AccessToken";
    private const string RefreshTokenKey = "MauiAuth_RefreshToken";
    private const string ExpiresAtKey = "MauiAuth_ExpiresAt";
    private const string ClaimsKey = "MauiAuth_Claims";

    public async Task<Tokens?> GetTokensAsync()
    {
        try
        {
            var idToken = await SecureStorage.Default.GetAsync(IdTokenKey);
            var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            var expiresAtSerialized = await SecureStorage.Default.GetAsync(ExpiresAtKey);

            DateTimeOffset? expiresAt = expiresAtSerialized is not null
                ? DateTimeOffset.Parse(expiresAtSerialized, CultureInfo.InvariantCulture)
                : null;

            var tokens = new Tokens(idToken, accessToken, refreshToken, expiresAt);

            return tokens;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<(string Name, string Value)>> GetClaimsAsync()
    {
        try
        {
            var serialized = await SecureStorage.Default.GetAsync(ClaimsKey);
            if (serialized is null)
            {
                return [];
            }

            var deserialized = JsonSerializer.Deserialize<IEnumerable<ClaimPair>>(serialized);

            return deserialized?.Select(x => (x.Name, x.Value)) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task SetTokensAsync(Tokens tokens)
    {
        await SecureStorage.Default.SetAsync(IdTokenKey, tokens.IdToken ?? string.Empty);
        await SecureStorage.Default.SetAsync(AccessTokenKey, tokens.AccessToken ?? string.Empty);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, tokens.RefreshToken ?? string.Empty);
        await SecureStorage.Default.SetAsync(ExpiresAtKey, tokens.Expires?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
    }

    public async Task SetClaimsAsync(IEnumerable<(string Name, string Value)> claims)
    {
        var serializedClaims = JsonSerializer.Serialize(claims.Select(x => new ClaimPair { Name = x.Name, Value = x.Value }));
        await SecureStorage.Default.SetAsync(ClaimsKey, serializedClaims);
    }

    public Task ClearAllAsync()
    {
        SecureStorage.Default.RemoveAll();
        return Task.CompletedTask;
    }
}
