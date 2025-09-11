using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models.AdminOfficerReview;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that retrieves the current details of the admin
/// officer review for felling licence applications.
/// </summary>
public interface IGetAdminOfficerReview
{
    /// <summary>
    /// Retrieves the overall status of the admin officer review for an application, including
    /// the status of each task for the review.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve the admin officer review for.</param>
    /// <param name="isAgentApplication">A flag indicating whether or not an agent created this application.</param>
    /// <param name="isLarchApplication">A flag indicating whether or not this is a larch application.</param>
    /// <param name="isAssignedWoodlandOfficer">A flag indicating whether or not the application has been assigned to a woodland officer.</param>
    /// <param name="isEiaApplication">A flag indicating whether or not this is an EIA application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="AdminOfficerReviewStatusModel"/> instance.</returns>
    Task<AdminOfficerReviewStatusModel> GetAdminOfficerReviewStatusAsync(
        Guid applicationId,
        bool isAgentApplication,
        bool isLarchApplication,
        bool isAssignedWoodlandOfficer,
        bool isCBWApplication,
        bool isEiaApplication,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the CBW (Cricket Bat Willow) review status for a specific application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the application to retrieve the CBW review status for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A nullable boolean indicating the CBW review status:
    /// - true if requires WO review,
    /// - false if can be approved directly,
    /// - null if the review status is not yet determined or unavailable.
    /// </returns>
    Task<bool?> GetCBWReviewStatusAsync(Guid applicationId, CancellationToken cancellationToken);
}