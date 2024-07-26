using System.Net.Http.Headers;

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Holefeeder.Ui.Web.Authentication;

#pragma warning disable CA1848

internal class AuthenticationMessageHandler(
    IAccessTokenProvider accessTokenProvider,
    ILogger<AuthenticationMessageHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var result = await accessTokenProvider.RequestAccessToken();

        switch (result.Status)
        {
            case AccessTokenResultStatus.RequiresRedirect:
                logger.LogWarning("Access token request requires redirect. Redirecting user to the login page");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    ReasonPhrase = "Requires redirect to login page"
                };
            case AccessTokenResultStatus.Success when result.TryGetToken(out var token):
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
                break;
            default:
                logger.LogError("Failed to acquire access token with status: {Status}", result.Status);
                // Optionally, return an appropriate error response
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    ReasonPhrase = "Access token acquisition failed"
                };
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
#pragma warning restore CA1848
