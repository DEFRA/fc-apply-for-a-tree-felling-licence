using Forestry.Flo.Services.Gis.Interfaces;

namespace Forestry.Flo.Services.FellingLicenceApplications.Configuration;

/// <summary>
/// Configuration settings class for the development implementation of external services.
/// </summary>
public class DevelopmentConfigOptions
{
    /// <summary>
    /// A flag indicating to use a development implementation of the <see cref="IPublicRegister"/>
    /// rather than publishing to a real ESRI endpoint - for development.
    /// </summary>
    public bool UseDevPublicRegister { get; set; } = false;
}