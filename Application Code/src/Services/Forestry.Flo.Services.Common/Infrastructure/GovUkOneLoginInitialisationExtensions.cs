using System.Security.Cryptography;
using GovUk.OneLogin.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;

namespace Forestry.Flo.Services.Common.Infrastructure;

/// <summary>
/// Provides extension methods for initializing GOV.UK One Login authentication options.
/// </summary>
public static class GovUkOneLoginInitialisationExtensions
{
    /// <summary>
    /// Configures the <see cref="OneLoginOptions"/> for GOV.UK One Login authentication using the specified <see cref="GovUkOneLoginOptions"/>.
    /// </summary>
    /// <param name="options">The <see cref="OneLoginOptions"/> to configure.</param>
    /// <param name="govUkOptions">The <see cref="GovUkOneLoginOptions"/> containing configuration values.</param>
    /// <param name="logMethod">A method to log messages, defaults to <see cref="Console.WriteLine()"/>.</param>
    /// <remarks>
    /// This method sets up the authentication scheme, environment, client ID, callback path, signing credentials,
    /// vectors of trust, cookie name prefixes, and event handlers for redirect and token validation.
    /// </remarks>
    public static void InitialiseGovUkOneLogin(this OneLoginOptions options, GovUkOneLoginOptions govUkOptions, Action<string>? logMethod = null)
    {
        logMethod ??= Console.WriteLine;

        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.Environment = govUkOptions.Environment;
        options.ClientId = govUkOptions.ClientId;
        options.CallbackPath = govUkOptions.CallbackPath;

        var currentKeyPem = govUkOptions.SigningKeys.Last().PrivateKeyPem;

        using (var rsa = RSA.Create())
        {
            rsa.ImportFromPem(currentKeyPem);
            var parameters = rsa.ExportParameters(includePrivateParameters: true);

            options.ClientAuthenticationCredentials = new SigningCredentials(
                new RsaSecurityKey(parameters)
                {
                    KeyId = GenerateKid(parameters)
                },
                SecurityAlgorithms.RsaSha256);
        }

        // Configure vectors of trust.
        // See the One Login docs for the various options to use here.
        options.VectorsOfTrust = govUkOptions.VectorsOfTrust;

        // Override the cookie name prefixes (optional)
        options.CorrelationCookie.Name = govUkOptions.CorrelationCookieName;
        options.NonceCookie.Name = govUkOptions.NonceCookieName;

        options.Events.OnRedirectToIdentityProvider += context =>
        {
            var token = context.HttpContext.Request.Query.TryGetValue("token", out var tokenValue);
            if (token)
            {
                logMethod("Sign up Token: " + tokenValue);
                context.ProtocolMessage.State = tokenValue;
            }

            logMethod("OIDC Redirect URL: " + context.ProtocolMessage.BuildRedirectUrl());
            return Task.CompletedTask;
        };

        options.Events.OnTokenValidated += context =>
        {
            if (context.ProtocolMessage.State is not null && context.Principal is not null)
            {
                context.Principal.AddIdentity(new System.Security.Claims.ClaimsIdentity([
                    new System.Security.Claims.Claim("token", context.ProtocolMessage.State)
                ]));
            }

            logMethod("Token validated successfully. State: " + context.ProtocolMessage.State);

            return Task.CompletedTask;
        };
    }

    private static string GenerateKid(RSAParameters parameters)
    {
        if (parameters.Modulus is null || parameters.Exponent is null)
        {
            throw new InvalidOperationException("RSA parameters must include modulus and exponent to generate a key ID.");
        }

        using var sha256 = SHA256.Create();
        var keyBytes = parameters.Modulus.Concat(parameters.Exponent).ToArray();
        var hash = sha256.ComputeHash(keyBytes);
        return Base64UrlEncoder.Encode(hash);
    }
}