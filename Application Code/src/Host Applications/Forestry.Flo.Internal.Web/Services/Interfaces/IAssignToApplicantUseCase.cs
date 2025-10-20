using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for assigning a felling licence application back to an external applicant.
/// </summary>
public interface IAssignToApplicantUseCase
{
    /// <summary>
    /// Retrieves a list of valid external applicants for assignment for a given application.
    /// </summary>
    /// <param name="internalUser">The internal user performing the assignment.</param>
    /// <param name="applicationId">The ID of the felling licence application.</param>
    /// <param name="returnUrl">The URL to return to after assignment.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the model for assigning back to an applicant, or a failure.</returns>
    Task<Result<AssignBackToApplicantModel>> GetValidExternalApplicantsForAssignmentAsync(
        InternalUser internalUser,
        Guid applicationId,
        string returnUrl,
        CancellationToken cancellationToken);

    /// <summary>
    /// Assigns a felling licence application to an external applicant and sends notifications.
    /// </summary>
    /// <param name="applicationId">The ID of the application to assign.</param>
    /// <param name="internalUser">The internal user performing the assignment.</param>
    /// <param name="applicantId">The ID of the external applicant to assign to.</param>
    /// <param name="returnReason">The reason for returning the application.</param>
    /// <param name="viewFLAUrl">The URL to view the application.</param>
    /// <param name="amendmentSections">Sections requiring attention.</param>
    /// <param name="amendmentCompartments">Compartments requiring attention.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AssignApplicationToApplicantAsync(
        Guid applicationId,
        InternalUser internalUser,
        Guid applicantId,
        string returnReason,
        string viewFLAUrl,
        Dictionary<FellingLicenceApplicationSection, bool> amendmentSections,
        Dictionary<Guid, bool> amendmentCompartments,
        CancellationToken cancellationToken);
}