namespace Forestry.Flo.Internal.Web.Infrastructure;
public class PublicRegisterExpiryOptions
{
    /// <summary>
    /// The time prior to the public register period end date that notifications should start being sent to assigned FC staff members.
    /// </summary>
    public TimeSpan ThresholdBeforePublicRegisterPeriodEnd { get; set; }
}