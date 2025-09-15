namespace Forestry.Flo.External.Web.Infrastructure;

public class EiaOptions
{
    /// <summary>
    /// A unique key used to identify the configuration section for EIA options.
    /// </summary>
    public static string ConfigurationKey => "EiaOptions";

    /// <summary>
    /// Gets or sets the external URI for Environmental Impact Assessment (EIA) guidance.
    /// </summary>
    public string EiaApplicationExternalUri { get; set; } = "https://www.gov.uk/guidance/environmental-impact-assessment";
}