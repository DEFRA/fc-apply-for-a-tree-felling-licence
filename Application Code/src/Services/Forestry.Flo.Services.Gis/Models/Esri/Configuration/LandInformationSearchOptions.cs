using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Gis.Models.Esri.Configuration;

/// <summary>
/// Configuration class relevant to the Land Information Search
/// </summary>
public class LandInformationSearchOptions
{
    /// <summary>
    /// The endpoint as specified by Forester/esri for the Land Information Search.
    /// <para>
    /// This is used during construction of the url for deep-link access into the LIS
    /// when the constraint check is ran.
    /// </para>
    /// </summary>
    [Required]
    public string DeepLinkUrlAndPath { get; set; }

    /// <summary>
    /// Parameter used as part of the query string in the deeplink.
    /// <para>
    /// The value of this configuration item is applied to the query param named "config" in the external LIS deep-link.
    /// </para>
    ///<para>
    /// The value of this configuration item is applied to the query param named "lisconfig" in the internal LIS deep-link.
    /// </para>
    /// </summary>
    public string LisConfig { get; set; }

    /// <summary>
    /// Full URL to the AGOL
    /// </summary>
    public string BaseUrl { get; set; } = null!;

    /// <summary>
    /// Path to the Feature service to push Case ID attributes and Submitted compartment geometries
    /// </summary>
    public string FeaturePath { get; set; } = null!;

    /// <summary>
    /// Get Token Url
    /// </summary>
    public string TokenUrl { get; set; } = null!;

    /// <summary>
    /// Path to the get token method
    /// </summary>
    public string TokenPath { get; set; } = null!;

    /// <summary>
    /// The Client Id for accessing the API
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// The Client Secret for the API
    /// </summary>
    public string ClientSecret { get; set; } = null!;
}