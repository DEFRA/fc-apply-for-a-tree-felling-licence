using System.Security.Cryptography;
using Forestry.Flo.Services.Common.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Forestry.Flo.External.Web.Controllers.Api;

[AllowAnonymous]
public class JwksController(ILogger<JwksController> logger) : Controller
{
    [HttpGet]
    [Route("jwks.json")]
    public ActionResult GetJwks(
        [FromServices] IOptions<GovUkOneLoginOptions> options)
    {
        var keys = new List<PublicJwk>();

        foreach (var keyConfig in options.Value.SigningKeys)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(keyConfig.PrivateKeyPem);

            var parameters = rsa.ExportParameters(false);
            var kid = GenerateKid(parameters);

            keys.Add(new PublicJwk(
                Kty: "RSA",
                E: Base64UrlEncoder.Encode(parameters.Exponent),
                Use: "sig",
                Kid: kid,
                N: Base64UrlEncoder.Encode(parameters.Modulus)
            ));
        }

        return Json(new { keys });
    }

    public static string GenerateKid(RSAParameters parameters)
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

    /// <summary>
    /// Represents a public JSON Web Key (JWK) used for signature verification.
    /// </summary>
    /// <param name="Kty">The key type (e.g., "RSA").</param>
    /// <param name="E">The base64url-encoded exponent value.</param>
    /// <param name="Use">The intended use of the key (e.g., "sig" for signature).</param>
    /// <param name="Kid">The key identifier, used to uniquely identify the key.</param>
    /// <param name="N">The base64url-encoded modulus value.</param>
    public record PublicJwk(
        string Kty,
        string E,
        string Use,
        string Kid,
        string N
    );
}
