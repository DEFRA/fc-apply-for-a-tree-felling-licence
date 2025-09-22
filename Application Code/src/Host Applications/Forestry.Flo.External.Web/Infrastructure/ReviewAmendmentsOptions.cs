namespace Forestry.Flo.External.Web.Infrastructure;

public class ReviewAmendmentsOptions
{
    /// <summary>
    /// A unique key used to identify the configuration section for these options.
    /// </summary>
    public static string ConfigurationKey => "ReviewAmendmentsOptions";

    /// <summary>
    /// The number of days since amendments were sent to the applicant after which
    /// the application will be automatically withdrawn if no response has been received.
    /// </summary>
    public int ApplicationWithdrawalDays { get; set; } = 28;
}