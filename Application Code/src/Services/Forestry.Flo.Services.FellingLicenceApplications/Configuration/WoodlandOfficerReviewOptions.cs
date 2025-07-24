using Forestry.Flo.Services.Gis.Interfaces;

namespace Forestry.Flo.Services.FellingLicenceApplications.Configuration;

/// <summary>
/// Configuration settings class for the woodland officer review tasks.
/// </summary>
public class WoodlandOfficerReviewOptions : DevelopmentConfigOptions
{
    /// <summary>
    /// The default length of time an application should be on the consultation
    /// public register.
    /// </summary>
    public TimeSpan PublicRegisterPeriod { get; set; }

    /// <summary>
    /// The default case type code to provide to the ESRI API when publishing a case to the
    /// consultation public register.
    /// </summary>
    public string DefaultCaseTypeOnPublishToPublicRegister { get; set; } = "02";

    /// <summary>
    /// The default case status code to provide to the ESRI API when publishing a case to the
    /// mobile app layers.
    /// </summary>
    public string DefaultCaseStatusOnPublishForMobileApps { get; set; } = "02";

    /// <summary>
    /// A flag indicating to use a development implementation of the <see cref="IPublicRegister"/>
    /// rather than publishing to a real ESRI endpoint - for development.
    /// </summary>
    public bool UseDevMobileAppsLayer { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to use the new confirmed felling restocking feature.
    /// </summary>
    public bool UseNewConfirmedFellingRestocking { get; set; } = false;

}