namespace Forestry.Flo.Internal.Web.Infrastructure;
public class PublicRegisterExpiryOptions
{
    /// <summary>
    /// Gets and sets the default DPR period, in days.
    /// </summary>
    public int DecisionPublicRegisterPeriod { get; set; } = 28;

    /// <summary>
    /// The time prior to the public register period end date that notifications should start being sent to assigned FC staff members.
    /// </summary>
    public TimeSpan ThresholdBeforePublicRegisterPeriodEnd { get; set; }
}