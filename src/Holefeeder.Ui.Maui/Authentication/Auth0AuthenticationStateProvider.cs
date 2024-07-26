using System.Security.Claims;

using Auth0.OidcClient;

using Holefeeder.Ui.Common.Authentication;

using Microsoft.AspNetCore.Components.Authorization;

namespace Holefeeder.Ui.Maui.Authentication;

internal class Auth0AuthenticationStateProvider : AuthenticationStateProvider, IAuthNavigationManager
{
    private readonly Auth0Client _auth0Client;
    private readonly Auth0Config _auth0Config;
    private readonly ITokenProvider _tokenProvider;
    private ClaimsPrincipal? _currentUser;

    public Auth0AuthenticationStateProvider(Auth0Client client, Auth0Config auth0Config, ITokenProvider tokenProvider)
    {
        _auth0Client = client;
        _auth0Config = auth0Config;
        _tokenProvider = tokenProvider;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_currentUser is null)
        {
            await Initialize();
        }

        return new AuthenticationState(_currentUser!);
    }

    public async Task LoginAsync()
    {
        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());
        var extraParameters = new Dictionary<string, string>();
        var audience = _auth0Config.Audience;

        if (!string.IsNullOrEmpty(audience))
        {
            extraParameters.Add("audience", audience);
        }

        var loginResult = await _auth0Client.LoginAsync(extraParameters);
        if (!loginResult.IsError)
        {
            await _tokenProvider.SetTokensAsync(new Tokens(
                loginResult.IdentityToken,
                loginResult.AccessToken,
                loginResult.RefreshToken,
                loginResult.AccessTokenExpiration));

            var claims = loginResult.User.Claims
                .Select(x => (x.Type, x.Value))
                .ToList();

            await _tokenProvider.SetClaimsAsync(claims);

            authenticatedUser = loginResult.User;
        }

        _currentUser = authenticatedUser;

        var state = new AuthenticationState(_currentUser);
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    public async Task LogoutAsync()
    {
        await _auth0Client.LogoutAsync();

        await _tokenProvider.ClearAllAsync();

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    private async Task Initialize()
    {
        var tokens = await _tokenProvider.GetTokensAsync();
        var claims = await _tokenProvider.GetClaimsAsync();

        if (tokens is not null && DateTime.Now < tokens.Expires)
        {
            _currentUser =
                new ClaimsPrincipal(new ClaimsIdentity(claims.Select(x => new Claim(x.Name, x.Value)), "none"));
        }
        else
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }
}
