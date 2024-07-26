using Auth0.OidcClient;

using Holefeeder.Ui.Common.Authentication;

namespace Holefeeder.Ui.Maui.Authentication;

internal class AuthenticationMessageHandler(Auth0Client authClient, TokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenService.GetBearerTokenAsync();

        if (token is not null && await tokenService.ShouldRefreshTokenAsync())
        {
            var refreshToken = await tokenService.GetRefreshTokenAsync();
            if (refreshToken != null)
            {
                var result = await authClient.RefreshTokenAsync(refreshToken, cancellationToken);
                if (!result.IsError)
                {
                    token = await tokenService.GetBearerTokenAsync();
                }
            }
            else
            {
                await authClient.LogoutAsync(cancellationToken: cancellationToken);
            }
        }

        if (token is not null)
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
