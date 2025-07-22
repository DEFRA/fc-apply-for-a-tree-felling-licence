namespace Forestry.Flo.Internal.Web.Infrastructure;

public class VoluntaryWithdrawalNotificationOptions
{
    /// <summary>
    /// The time after the created date for the 'with applicant' status that the Voluntary withdrawal notification should be sent to applicant.
    /// </summary>
    public TimeSpan ThresholdAfterWithApplicantStatusDate { get; set; }

    /// <summary>
    /// The time after the created date for the 'with applicant' status that the Voluntary withdrawal notification should be sent to applicant.
    /// </summary>
    public TimeSpan ThresholdAutomaticWithdrawal { get; set; }
}