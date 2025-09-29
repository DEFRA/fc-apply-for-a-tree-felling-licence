namespace Forestry.Flo.External.Web.Infrastructure;

/// <summary>
/// Represents configuration options for the internal user site.
/// </summary>
public class InternalUserSiteOptions
{
    /// <summary>
    /// A unique key used to identify the configuration section for these options.
    /// </summary>
    public static string ConfigurationKey => "InternalUserSite";

    /// <summary>
    /// The base URL of the internal user site.
    /// </summary>
    public required string BaseUrl { get; set; }
}