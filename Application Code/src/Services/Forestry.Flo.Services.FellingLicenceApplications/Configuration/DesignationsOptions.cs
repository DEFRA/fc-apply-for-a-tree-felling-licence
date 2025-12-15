namespace Forestry.Flo.Services.FellingLicenceApplications.Configuration;

/// <summary>
/// Configuration settings for compartment designations.
/// </summary>
public class DesignationsOptions
{
    public static string ConfigurationKey => "Designations";

    /// <summary>
    /// Gets and sets the list of zone names that are of interest on the PAWS mapping layers.
    /// </summary>
    public List<string> PawsZoneNames { get; set; } = new()
    {
        "ARW", //"Ancient replanted woodland",
        "IAWPP" //"Infilled ancient wood pasture"
    };
}