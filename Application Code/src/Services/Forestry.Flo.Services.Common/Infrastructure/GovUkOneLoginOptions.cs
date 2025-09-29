using GovUk.OneLogin.AspNetCore;

namespace Forestry.Flo.Services.Common.Infrastructure;

/// <summary>
/// Represents configuration options for GOV.UK One Login integration.
/// </summary>
public class GovUkOneLoginOptions
{
    /// <summary>
    /// A unique key used to identify the configuration section for GOV.UK One Login options.
    /// </summary>
    public static string ConfigurationKey => "GovUkOneLoginOptions";

    /// <summary>
    /// Gets or sets the client ID used to identify the application with GOV.UK One Login.
    /// </summary>
    public required string ClientId { get; set; }

    /// <summary>
    /// Gets or sets the call back path where the user will be redirected after authentication.
    /// </summary>
    public required string CallbackPath { get; set; }

    /// <summary>
    /// Gets or sets the environment for GOV.UK One Login (e.g., Integration, Production).
    /// </summary>
    /// <remarks>
    /// See <see cref="OneLoginEnvironments"/> for available options.
    /// </remarks>
    public required string Environment { get; set; }

    /// <summary>
    /// A collection of signing key options used for signing authentication requests.
    /// </summary>
    public required List<SigningKeyOptions> SigningKeys { get; set; }

    /// <summary>
    /// Gets or sets the name of the correlation cookie used for GOV.UK One Login authentication.
    /// </summary>
    public string CorrelationCookieName { get; set; } = "flov2-onelogin-correlation.";

    /// <summary>
    /// Gets or sets the name of the nonce cookie used for GOV.UK One Login authentication.
    /// </summary>
    public string NonceCookieName { get; set; } = "flov2-onelogin-nonce.";

    /// <summary>
    /// Gets or sets the vectors of trust for GOV.UK One Login authentication.
    /// See the One Login docs for the various options to use here.
    /// </summary>
    public List<string> VectorsOfTrust { get; set; } = ["Cl"];
}

/// <summary>
/// Represents signing key options for GOV.UK One Login authentication.
/// </summary>
public class SigningKeyOptions
{
    /// <summary>
    /// Gets or sets the private key in PEM format used for signing authentication requests.
    /// </summary>
    public required string PrivateKeyPem { get; set; }
}