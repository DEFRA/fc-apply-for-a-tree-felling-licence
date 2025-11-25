using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Infrastructure;

/// <summary>
/// Provides extension methods for the <see cref="FellingLicenceStatus"/> enum.
/// </summary>
public static class FellingLicenceStatusExtensions
{
    /// <summary>
    /// Gets the GOV.UK tag style class name associated with the specified <see cref="FellingLicenceStatus"/>.
    /// </summary>
    /// <param name="status">The felling licence status.</param>
    /// <returns>
    /// A string representing the GOV.UK tag style class name for the given status.
    /// </returns>
    /// <remarks>
    /// Full list of GOV.UK tag colors available at
    /// https://design-system.service.gov.uk/components/tag/
    /// </remarks>
    public static string GetStatusStyleName(this FellingLicenceStatus status)
    {
        // Full list of gov tag colours available https://design-system.service.gov.uk/components/tag/

        return status switch
        {
            FellingLicenceStatus.Received => "govuk-tag--pink",
            FellingLicenceStatus.Submitted => "govuk-tag--blue",
            FellingLicenceStatus.AdminOfficerReview => "govuk-tag--pink",
            FellingLicenceStatus.WithApplicant => "govuk-tag--turquoise",
            FellingLicenceStatus.ReturnedToApplicant => "govuk-tag--turquoise",
            FellingLicenceStatus.WoodlandOfficerReview => "govuk-tag--yellow",
            FellingLicenceStatus.SentForApproval => "govuk-tag--purple",
            FellingLicenceStatus.Approved => "govuk-tag--green",
            FellingLicenceStatus.Refused => "govuk-tag--red",
            FellingLicenceStatus.Withdrawn => "govuk-tag--orange",
            FellingLicenceStatus.ReferredToLocalAuthority => "govuk-tag--mid-grey",
            FellingLicenceStatus.ApprovedInError => "govuk-tag--red",
            _ => "govuk-tag--grey"
        };
    }
}