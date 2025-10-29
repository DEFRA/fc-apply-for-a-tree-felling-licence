using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;
using Forestry.Flo.Services.InternalUsers.Models;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for admin officer use cases related to Environmental Impact Assessment (EIA) for felling licence applications.
/// </summary>
public interface IEnvironmentalImpactAssessmentAdminOfficerUseCase
{
    /// <summary>
    /// Retrieves the Environmental Impact Assessment (EIA) details for a specified felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result{EnvironmentalImpactAssessmentModel}"/> containing the EIA model if successful,
    /// or a failure result with an error message if the application or EIA cannot be retrieved.
    /// </returns>
    Task<Result<EnvironmentalImpactAssessmentModel>> GetEnvironmentalImpactAssessmentAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a summary model for the specified felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result{FellingLicenceApplicationSummaryModel}"/> containing the summary model if successful.
    /// </returns>
    Task<Result<FellingLicenceApplicationSummaryModel>> GetSummaryModel(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Confirms whether the attached EIA forms are correct for the specified application.
    /// Updates the Environmental Impact Assessment record as an Admin Officer.
    /// If the forms are not correct, sends a notification to the applicant and rolls back the transaction on failure.
    /// </summary>
    /// <param name="viewModel">The view model containing details about the EIA forms and their correctness.</param>
    /// <param name="performingUserId">The ID of the user performing the confirmation.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the confirmation and notification process.
    /// </returns>
    Task<Result> ConfirmAttachedEiaFormsAreCorrectAsync(
        EiaWithFormsPresentViewModel viewModel,
        Guid performingUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Confirms whether the EIA forms have been received for the specified application.
    /// Updates the Environmental Impact Assessment record as an Admin Officer and audits the operation.
    /// If the forms have not been received, triggers a notification and handles transaction rollback on failure.
    /// </summary>
    /// <param name="viewModel">The view model containing EIA form receipt details.</param>
    /// <param name="performingUserId">The ID of the user performing the confirmation.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure of the confirmation and notification process.
    /// </returns>
    Task<Result> ConfirmEiaFormsHaveBeenReceivedAsync(
        EiaWithFormsAbsentViewModel viewModel,
        Guid performingUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves user accounts by their IDs.
    /// </summary>
    /// <param name="ids">A list of user account IDs.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result{List{UserAccountModel}}"/> containing the user accounts if successful.
    /// </returns>
    Task<Result<List<UserAccountModel>>> RetrieveUserAccountsByIdsAsync(
        List<Guid> ids,
        CancellationToken cancellationToken);
}