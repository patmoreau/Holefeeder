using System.Diagnostics.CodeAnalysis;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Holefeeder.Ui.Common.Authentication;

[SuppressMessage("Security", "CA5404:Do not disable token validation checks")]
internal static class JwtService
{
    internal static async Task<JsonWebToken?> DecodeToken(this string token)
    {
        var handler = new JsonWebTokenHandler();
        // Use ValidateToken method to read the token, since JsonWebTokenHandler does not have a direct ReadToken method like JwtSecurityTokenHandler.
        // Assuming validation is not the main concern here, we'll focus on decoding only.
        // Note: You might need to provide a TokenValidationParameters object, even if it's with minimal setup, to use ValidateToken.
        // For simplicity, this example will not perform actual validation.
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = false,
            ValidateLifetime = false,
            SignatureValidator = (jwtEncodedString, _) => new JsonWebToken(jwtEncodedString)
        };
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            var validationResult = await handler.ValidateTokenAsync(token, tokenValidationParameters);
            if (validationResult.IsValid && validationResult.SecurityToken is JsonWebToken jwt)
            {
                return jwt;
            }
        }
        catch
        {
            // Handle or log the error as needed.
            // For this example, we'll just return null to indicate the token could not be decoded.
        }
#pragma warning restore CA1031 // Do not catch general exception types
        return null;
    }
}
