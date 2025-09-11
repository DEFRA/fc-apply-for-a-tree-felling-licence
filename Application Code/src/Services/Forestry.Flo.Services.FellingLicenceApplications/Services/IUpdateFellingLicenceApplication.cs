using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for a service that updates <see cref="FellingLicenceApplication"/>./>
/// </summary>
public interface IUpdateFellingLicenceApplication
{
    /// <summary>
    /// Adds a status history entry to a <see cref="FellingLicenceApplication"/>.
    /// </summary>
    /// <param name="userId">The id of the user commiting the change in status.</param>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="newStatus">The status to be added.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task AddStatusHistoryAsync(
        Guid userId,
        Guid applicationId,
        FellingLicenceStatus newStatus,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the date received value for an application and updates the citizen charter date accordingly.
    /// </summary>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="dateReceived">The date that the application was received.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the date received and citizen charter date have been successfully updated, or an error if unsuccessful.</returns>
    Task<Result> UpdateDateReceivedAsync(
        Guid applicationId,
        DateTime dateReceived,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates an application to return it to the applicant.
    /// </summary>
    /// <remarks>This may result in the application being in either <see cref="FellingLicenceStatus.ReturnedToApplicant"/> or
    /// <see cref="FellingLicenceStatus.WithApplicant"/> state, depending on the current state of the application.</remarks>
    /// <param name="request">A populated <see cref="ReturnToApplicantRequest"/> request model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of assigned staff member ids requiring a notification.</returns>
    Task<Result<List<Guid>>> ReturnToApplicantAsync(
        ReturnToApplicantRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Assigns an application to an internal user.
    /// </summary>
    /// <remarks>This may result in updating the status of the application, if the assignment is for the Admin Officer role and
    /// the application is currently in the Submitted state.</remarks>
    /// <param name="request">A populated <see cref="AssignToUserRequest"/> request model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="AssignToUserResponse"/> model, or <see cref="Result.Failure"/> if the assignment failed.</returns>
    Task<Result<AssignToUserResponse>> AssignToInternalUserAsync(
        AssignToUserRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds the required decision public register details to the application.
    /// </summary>
    /// <param name="applicationId">The id of the application add the details to.</param>
    /// <param name="publishedDateTime">The point in time that the application was published to the decision public register</param>
    /// <param name="expiryDateTime">The point in time that the application expires on the decision public register, and so would need to be removed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<Result> AddDecisionPublicRegisterDetailsAsync(
        Guid applicationId,
        DateTime publishedDateTime,
        DateTime expiryDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Set the decision public register entry for the application in the local system to removed.
    /// </summary>
    /// <param name="applicationId">The id of the application to which the decision public register details need to be set to expired.</param>
    /// <param name="removedDateTime">The point in time that the application was removed from the decision public register.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<Result> SetRemovalDateOnDecisionPublicRegisterEntryAsync(
        Guid applicationId,
        DateTime removedDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Set the consultation public register entry for the application in the local system to removed.
    /// </summary>
    /// <param name="applicationId">The id of the application to which the decision public register details need to be set to expired.</param>
    /// <param name="removedDateTime">The point in time that the application was removed from the decision public register.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<Result> SetRemovalDateOnConsultationPublicRegisterEntryAsync(
        Guid applicationId,
        DateTime removedDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the final action date for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="finalActionDate">The final action date to be set.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the final action date has been successfully updated, or an error if unsuccessful.</returns>
    Task<Result> UpdateFinalActionDateAsync(
        Guid applicationId,
        DateTime finalActionDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to revert an application from the Withdrawn state.
    /// </summary>
    /// <param name="performingUserId">The id of the user reverting the application.</param>
    /// <param name="applicationId">The id of the application to be reverted.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the application was successfully reverted, or an error if unsuccessful.</returns>
    Task<Result> TryRevertApplicationFromWithdrawnAsync(
        Guid performingUserId,
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to set the approver id of the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="approverId">The id of the approver to set on the application, or null to remove any existing one.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the update was successful.</returns>
    Task<Result> SetApplicationApproverAsync(
        Guid applicationId,
        Guid? approverId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the environmental impact assessment status for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="status">The environmental impact assessment status to set.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the environmental impact assessment status was successfully updated, or an error if unsuccessful.</returns>
    Task<Result> UpdateEnvironmentalImpactAssessmentStatusAsync(
        Guid applicationId,
        bool status,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the environmental impact assessment record for a specified application.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="eiaRecord">The environmental impact assessment record containing updated values.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the environmental impact assessment record was successfully updated, or an error if unsuccessful.</returns>
    Task<Result> UpdateEnvironmentalImpactAssessmentAsync(
        Guid applicationId,
        EnvironmentalImpactAssessmentRecord eiaRecord,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the environmental impact assessment record for a specified application as an Admin Officer.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="eiaRecord">The environmental impact assessment record containing updated values for the Admin Officer.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the environmental impact assessment record was successfully updated, or an error if unsuccessful.</returns>
    Task<Result> UpdateEnvironmentalImpactAssessmentAsAdminOfficerAsync(
        Guid applicationId,
        EnvironmentalImpactAssessmentAdminOfficerRecord eiaRecord,
        CancellationToken cancellationToken);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, containing the database transaction.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}
