namespace Forestry.Flo.Internal.Web.Infrastructure;

public class ApplicationExtensionOptions
{
    /// <summary>
    /// The length of an extension if the final action date is surpassed without the application being processed.
    /// </summary>
    public TimeSpan ExtensionLength { get; set; }

    /// <summary>
    /// The time prior to the final action date that notifications should start being sent to assigned FC staff members.
    /// </summary>
    public TimeSpan ThresholdBeforeFinalActionDate { get; set; }
}