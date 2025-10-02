namespace Forestry.Flo.Services.Common.Infrastructure;

/// <summary>
/// Represents configuration options for authentication.
/// The purpose of this is to be able to toggle between identity providers, in case of issues during transition.
/// </summary>
public class AuthenticationOptions
{
    /// <summary>
    /// A unique key used to identify the configuration section for authentication options.
    /// </summary>
    public static string ConfigurationKey => "AuthenticationOptions";

    /// <summary>
    /// Gets or sets the authentication provider to use.
    /// </summary>
    public AuthenticationProvider Provider { get; set; } = AuthenticationProvider.OneLogin;
}

public enum AuthenticationProvider
{
    /// <summary>
    /// Use GOV.UK One Login as the authentication provider.
    /// </summary>
    OneLogin,
    /// <summary>
    /// Use Azure Active Directory as the authentication provider.
    /// </summary>
    Azure
}