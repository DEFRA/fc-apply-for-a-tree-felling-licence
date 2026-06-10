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

    /// <summary>
    /// Gets and sets the length of time the applicant has to respond before it is deemed that they have accepted the
    /// extension of the application final action date. This is used for display purposes in the notification email,
    /// and is not intended to be used for any calculations or logic in the system.
    /// </summary>
    public TimeSpan DeemedAcceptanceTimeSpan { get; set; }
}