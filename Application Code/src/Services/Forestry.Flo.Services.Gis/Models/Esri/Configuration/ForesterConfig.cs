namespace Forestry.Flo.Services.Gis.Models.Esri.Configuration;

/// <summary>
/// Contains all the settings for the Forester service.
/// </summary>
public class ForesterConfig: BaseConfigAGOL<OAuthServiceSettingsUser>
{
    /// <summary>
    /// If the service needs a token to access
    /// </summary>
    public Boolean NeedsToken { get; set; }

    /// <summary>
    /// Contains all the settings to be able to generate a token for the system.
    /// </summary>
    public OAuthServiceSettingsUser GenerateTokenService { get; set; } = null!;


    /// <summary>
    /// Contains all the settings to generate Files of the maps
    /// </summary>
    public UtilitiesServiceSettings UtilitiesService { get; set; } = null!;

}

public class UtilitiesServiceSettings : BaseEsriAccessSettingConfig
{
    /// <summary>
    /// Settings for the Export service
    /// </summary>
    public ExportServiceSettings ExportService { get; set; }

    public JobStatusServiceSettings JobStatusService { get; set; }
}


public class  JobStatusServiceSettings : BaseEsriServiceConfig{
    public StatusSettings Status { get; set; }
}

public class StatusSettings
{
    /// <summary>
    /// The states that are considered failed
    /// </summary>
    public string[] FailedStates { get; set; } = [];

    /// <summary>
    /// The states that are considered successful
    /// </summary>
    public string[] SuccessStates { get; set; } = [];

    /// <summary>
    ///  The states that are pending
    /// </summary>
    public string[] PendingStates { get; set; } = [];
}

public class TextOverrideDetails
{
    /// <summary>
    /// The copy right text to use
    /// </summary>
    public string Copyright { get; set; } = "";

    /// <summary>
    /// The Title to use if its felling
    /// </summary>
    public string FellingTitle { get; set; } = "";

    /// <summary>
    /// The Title to use if its felling
    /// </summary>
    public string RestockingTitle { get; set; } = "";
}

public class ExportServiceSettings : BaseEsriServiceConfig
{
    /// <summary>
    /// The default file to export
    /// Possible examples: 
    /// <list type="bullet">
    /// <item>PNG8</item>
    /// <item>PNG32</item>
    /// <item>JPG</item>
    /// <item>GIF</item>
    /// <item>PDF</item>
    /// <item>EPS</item>
    /// <item>SVG</item>
    /// <item>SVGZ</item>
    /// <item>AIX</item>
    /// <item>TIFF</item>
    /// </list>
    /// </summary>
    public string DefaultFormat { get; set; } = "PNG8";

    /// <summary>
    /// The URL to the basemap
    /// </summary>
    public string BaseMap { get; set; }

    /// <summary>
    /// The ID of the base map
    /// </summary>
    public string BaseMapID { get; set; }

    /// <summary>
    /// The Text to override with
    /// </summary>
    public TextOverrideDetails TextOverrides { get; set; } = new TextOverrideDetails();
}