using System.Security.Claims;

using Microsoft.Extensions.Options;

namespace Holefeeder.Ui.Common.Authentication;

public class TokenService(ITokenProvider tokenProvider, IOptions<AuthenticationSettings> settings)
{
    private readonly AuthenticationSettings _settings = settings.Value;

    public async Task<string?> GetBearerTokenAsync()
    {
        var tokens = await tokenProvider.GetTokensAsync();

        var token = _settings.UseIdTokenForHttpAuthentication
            ? tokens?.IdToken
            : tokens?.AccessToken;

        return token;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        var tokens = await tokenProvider.GetTokensAsync();

        return tokens?.RefreshToken;
    }

    public async Task<ClaimsPrincipal> GetClaimsAsync()
    {
        var claimPairs = await tokenProvider.GetClaimsAsync();

        var claims = claimPairs.Select(claimPair => new Claim(claimPair.Name, claimPair.Value)).ToList();

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Count > 0 ? "none" : null));

        return claimsPrincipal;
    }

    public async Task<bool> ShouldRefreshTokenAsync()
    {
        var tokens = await tokenProvider.GetTokensAsync();

        if (tokens?.IdToken == null || tokens.RefreshToken == null)
        {
            return false;
        }

        var decodedToken = await tokens.IdToken.DecodeToken();

        return decodedToken != null && ShouldRefreshToken(decodedToken.ValidTo);
    }

    private bool ShouldRefreshToken(DateTime expiresAt) =>
        DateTime.UtcNow.AddMinutes(_settings.RefreshExpiryClockSkewInMinutes) >= expiresAt;
}
