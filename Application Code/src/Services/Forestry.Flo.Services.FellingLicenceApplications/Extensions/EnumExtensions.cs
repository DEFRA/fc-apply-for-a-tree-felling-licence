using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Extensions;

public static class EnumExtensions
{
    public static string GetStatusStyleName(this InternalReviewStepStatus status)
    {
        return status switch
        {
            InternalReviewStepStatus.NotStarted or InternalReviewStepStatus.CannotStartYet or InternalReviewStepStatus.NotRequired => "govuk-tag--grey",
            InternalReviewStepStatus.InProgress => "govuk-tag--blue",
            InternalReviewStepStatus.Failed => "govuk-tag--red",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets a formatted date string for the current date plus the recommended duration.
    /// </summary>
    /// <param name="duration">The recommended licence duration to calculate an expiry date with.</param>
    /// <param name="clock">An <see cref="IClock"/> to get the current date.</param>
    /// <returns>A formatted date string for the expiry date calculated from the input.</returns>
    public static string GetExpiryDateForRecommendedDuration(
        this RecommendedLicenceDuration? duration,
        IClock clock)
    {
        if (duration is null or RecommendedLicenceDuration.None)
        {
            return "To be confirmed";
        }

        var now = clock.GetCurrentInstant().ToDateTimeUtc();

        return now.AddYears((int)duration).CreateFormattedDate();
    }
}