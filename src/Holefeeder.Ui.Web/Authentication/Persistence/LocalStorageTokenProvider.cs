using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

using Holefeeder.Ui.Common.Authentication;

using Microsoft.JSInterop;

namespace Holefeeder.Ui.Web.Authentication.Persistence;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal class LocalStorageTokenProvider(IJSRuntime jsRuntime) : ITokenProvider
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
            var idToken = await GetItem(IdTokenKey);
            var accessToken = await GetItem(AccessTokenKey);
            var refreshToken = await GetItem(RefreshTokenKey);
            var expiresAtSerialized = await GetItem(ExpiresAtKey);

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
            var serialized = await GetItem(ClaimsKey);
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
        await SetItem(IdTokenKey, tokens.IdToken);
        await SetItem(AccessTokenKey, tokens.AccessToken);
        await SetItem(RefreshTokenKey, tokens.RefreshToken);
        await SetItem(ExpiresAtKey, tokens.Expires?.ToString(CultureInfo.InvariantCulture));
    }

    public async Task SetClaimsAsync(IEnumerable<(string Name, string Value)> claims)
    {
        var serializedClaims =
            JsonSerializer.Serialize(claims.Select(x => new ClaimPair { Name = x.Name, Value = x.Value }));
        await SetItem(ClaimsKey, serializedClaims);
    }

    public async Task ClearAllAsync() => await Clear();

    private async Task<string?> GetItem(string key) =>
        await jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

    private async Task SetItem(string key, string? value) =>
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);

    private async Task RemoveItem(string key) => await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);

    private async Task Clear() => await jsRuntime.InvokeVoidAsync("localStorage.clear");
}
