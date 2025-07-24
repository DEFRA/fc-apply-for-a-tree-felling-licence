using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Gis.Models.Esri.Configuration;
public class BaseEsriAccessSettingConfig : BaseEsriServiceConfig
{
    /// <summary>
    /// States if the End point is a public endpoint
    /// </summary>
    public bool IsPublic { get; set; } = false;

    public bool NeedsToken { get; set; }
}

/// <summary>
/// The basic settings that all services have.
/// </summary>
public class BaseEsriServiceConfig
{
    /// <summary>
    /// The distance operation is performed on a geometry service resource. It reports the 2D Euclidean or geodesic distance between the two geometries. At 10.1 and later, this operation calls simplify on the input geometry1 and geometry2 values when the geodesic parameter is true. You can provide arguments to the distance operation as query parameters defined in the following parameters table.
    /// <see cref="https://developers.arcgis.com/rest/services-reference/enterprise/distance.htm"/>
    /// </summary>
    public string Path { get; set; } = null!;
}


public class BaseConfigAGOL<T>
{
    /// <summary>
    /// The BaseUrl resource only returns the version of the containing portal.
    /// Esri referer to it as the "ROOT".
    /// <see cref="https://developers.arcgis.com/rest/users-groups-and-items/root.htm"/>
    /// </summary>
    [Required]
    public string BaseUrl { get; set; } = null!;

    /// <summary>
    /// English Country Code
    /// </summary>
    [Required]
    public string CountryCode { get; set; } = "E92000001";

    /// <summary>
    /// Contains all the settings to be able to generate a token for the system.
    /// </summary>
    public T GenerateTokenService { get; set; }

    /// <summary>
    /// The Map API Key
    /// </summary>
    public string ApiKey { get; set; } = null!;

    /// <summary>
    /// All the Layers that we use in the system.
    /// </summary>
    public List<FeatureLayerConfig>? LayerServices { get; set; } = null!;

    /// <summary>
    /// Contains all the settings to call the services under the Geometry service
    /// </summary>
    public GeometryServiceSettings GeometryService { get; set; } = null!;

}