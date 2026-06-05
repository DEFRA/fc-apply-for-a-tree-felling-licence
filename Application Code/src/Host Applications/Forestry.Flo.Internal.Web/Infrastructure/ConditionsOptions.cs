namespace Forestry.Flo.Internal.Web.Infrastructure;

/// <summary>
/// Options class for configuring the conditions deemed acceptance time span. This time span represents the
/// period after which conditions are considered accepted if not explicitly rejected by the applicant. It
/// is used to tell the applicant in the conditions notification when they should respond by if they do
/// not agree to the calculated conditions.
/// </summary>
public class ConditionsOptions
{
    /// <summary>
    /// A unique key used to identify the configuration section for conditions options.
    /// </summary>
    public static string ConfigurationKey => "ConditionsOptions";

    /// <summary>
    /// Gets and sets a timespan representing the period after which conditions are considered accepted
    /// if not explicitly rejected by the applicant.
    /// </summary>
    public TimeSpan ConditionsDeemedAcceptanceTimeSpan { get; set; } = TimeSpan.FromDays(14);
}