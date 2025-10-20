using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for operations related to confirmed felling and restocking details
/// in the woodland officer review process.
/// </summary>
public interface IConfirmedFellingAndRestockingDetailsUseCase
{
    /// <summary>
    /// Retrieves the set of confirmed felling and restocking details for a given application,
    /// along with the proposed details and activity feed items.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve the data for.</param>
    /// <param name="user">The user viewing the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="pageName">The name of the page viewing the data, for the breadcrumbs.</param>
    /// <returns>A <see cref="Result{ConfirmedFellingRestockingDetailsModel}"/> representing the application data.</returns>
    Task<Result<ConfirmedFellingRestockingDetailsModel>> GetConfirmedFellingAndRestockingDetailsAsync(
        Guid applicationId,
        InternalUser user,
        CancellationToken cancellationToken,
        string? pageName = null);

    /// <summary>
    /// Saves amendments made to confirmed felling details for a specific compartment.
    /// </summary>
    /// <param name="model">The view model containing amended confirmed felling details.</param>
    /// <param name="user">The internal user performing the save operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> SaveConfirmedFellingDetailsAsync(
        AmendConfirmedFellingDetailsViewModel model,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new confirmed felling details to a specific compartment.
    /// </summary>
    /// <param name="model">The view model containing the new confirmed felling details.</param>
    /// <param name="user">The internal user performing the save operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> SaveConfirmedFellingDetailsAsync(
        AddNewConfirmedFellingDetailsViewModel model,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of selectable compartments for a given felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{SelectFellingCompartmentModel}"/> containing selectable compartments.</returns>
    Task<Result<SelectFellingCompartmentModel>> GetSelectableFellingCompartmentsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reverts amendments made to a confirmed felling detail for a specific application and proposed felling details.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="proposedFellingDetailsId">The unique identifier of the proposed felling details to revert.</param>
    /// <param name="user">The internal user performing the revert operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the revert operation.</returns>
    Task<Result> RevertConfirmedFellingDetailAmendmentsAsync(
        Guid applicationId,
        Guid proposedFellingDetailsId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a confirmed felling detail from an application, including all associated restocking details.
    /// </summary>
    /// <param name="applicationId">The id of the application containing the confirmed felling detail.</param>
    /// <param name="confirmedFellingDetailId">The id of the confirmed felling detail to delete.</param>
    /// <param name="user">The internal user performing the deletion.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the confirmed felling detail has been deleted.</returns>
    Task<Result> DeleteConfirmedFellingDetailAsync(
        Guid applicationId,
        Guid confirmedFellingDetailId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a confirmed restocking detail from an application.
    /// </summary>
    /// <param name="applicationId">The id of the application containing the confirmed restocking detail.</param>
    /// <param name="confirmedRestockingDetailId">The id of the confirmed restocking detail to delete.</param>
    /// <param name="user">The internal user performing the deletion.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the confirmed restocking detail has been deleted.</returns>
    Task<Result> DeleteConfirmedRestockingDetailAsync(
        Guid applicationId,
        Guid confirmedRestockingDetailId,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves an amended restocking details model back to the data store.
    /// </summary>
    /// <param name="model">A <see cref="AmendConfirmedRestockingDetailsViewModel"/> model of the restocking data to save.</param>
    /// <param name="user">The user amending the data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the data saved successfully.</returns>
    Task<Result> SaveConfirmedRestockingDetailsAsync(
        AmendConfirmedRestockingDetailsViewModel model,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sends amendments to the applicant and triggers notification email.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="user">The internal user sending amendments.</param>
    /// <param name="amendmentsReason">Optional reason for amendments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> SendAmendmentsToApplicant(
        Guid applicationId,
        InternalUser user,
        string? amendmentsReason,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks further amendments as complete for a given amendment review.
    /// Publishes audit events for completion and failure, and logs the result.
    /// </summary>
    /// <param name="user">The internal user completing the amendments.</param>
    /// <param name="amendmentReviewId">The unique identifier of the amendment review to complete.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the completion operation.</returns>
    Task<Result> MakeFurtherAmendments(
        InternalUser user,
        Guid amendmentReviewId,
        CancellationToken cancellationToken);
}