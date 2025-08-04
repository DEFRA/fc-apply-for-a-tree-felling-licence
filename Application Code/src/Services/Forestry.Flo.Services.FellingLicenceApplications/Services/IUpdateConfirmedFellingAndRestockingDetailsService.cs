using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using Microsoft.EntityFrameworkCore.Storage;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Defines the contract for a service that populates and amends confirmed felling and restocking details.
/// </summary>
public interface IUpdateConfirmedFellingAndRestockingDetailsService
{
    /// <summary>
    /// Begins a transaction for updating confirmed felling and restocking details.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Converts proposed felling and restocking details into confirmed felling and restocking details.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user importing the details.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether confirmed felling and restocking details have been updated.</returns>
    Task<Result> ConvertProposedFellingAndRestockingToConfirmedAsync(
        Guid applicationId, 
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reverts amendments made to a deleted confirmed felling detail by reimporting the proposed felling details.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="proposedFellingDetailsId">The id of the proposed felling details to reimport.</param>
    /// <param name="userId">The id of the user reverting the amendments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the deleted confirmed felling detail amendments have been reverted.</returns>
    Task<Result> RevertConfirmedFellingDetailAmendmentsAsync(
        Guid applicationId,
        Guid proposedFellingDetailsId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves amendments made to confirmed felling and restocking details.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user amending the details.</param>
    /// <param name="confirmedFellingAndRestockingDetailModels">A collection of <see cref="ConfirmedFellingAndRestockingDetailModel"/> containing felling and restocking details for compartments.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether confirmed felling and restocking details have been updated.</returns>
    Task<Result> SaveChangesToConfirmedFellingAndRestockingAsync(
        Guid applicationId,
        Guid userId,
        IList<ConfirmedFellingAndRestockingDetailModel> confirmedFellingAndRestockingDetailModels,
        CancellationToken cancellationToken);

    /// <summary>
    /// Saves amendments made to confirmed felling details.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user amending the details.</param>
    /// <param name="newFelling">A <see cref="IndividualConfirmedFellingRestockingDetailModel"/> containing felling and restocking details for a compartment.</param>
    /// <param name="speciesModel">A dictionary of <see cref="SpeciesModel"/> keyed by species code, representing the species included in the confirmed felling details.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether confirmed felling details have been updated.</returns>
    Task<Result> SaveChangesToConfirmedFellingDetailsAsync(
        Guid applicationId,
        Guid userId,
        IndividualConfirmedFellingRestockingDetailModel newFelling,
        Dictionary<string, SpeciesModel> speciesModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds new confirmed felling and restocking details to an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userId">The id of the user amending the details.</param>
    /// <param name="confirmedFellingDetailsModel">A <see cref="NewConfirmedFellingDetailWithCompartmentId"/> containing new felling and restocking details for a compartment.</param>
    /// <param name="speciesModel">A dictionary of <see cref="SpeciesModel"/> keyed by species code, representing the species included in the confirmed felling details.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether confirmed felling details have been updated.</returns>
    Task<Result> AddNewConfirmedFellingDetailsAsync(
        Guid applicationId,
        Guid userId,
        NewConfirmedFellingDetailWithCompartmentId confirmedFellingDetailsModel,
        Dictionary<string, SpeciesModel> speciesModel,
        CancellationToken cancellationToken);

    Task<Result> SaveChangesToConfirmedRestockingDetailsAsync(
        Guid applicationId,
        Guid userId,
        IndividualConfirmedRestockingDetailModel confirmedRestockingDetailsModel,
        Dictionary<string, SpeciesModel> speciesModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves confirmed felling and restocking details for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether confirmed felling and restocking details have been retrieved containing a populated <see cref="CombinedConfirmedFellingAndRestockingDetailRecord"/>.</returns>
    Task<Result<CombinedConfirmedFellingAndRestockingDetailRecord>> RetrieveConfirmedFellingAndRestockingDetailModelAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a confirmed felling detail from an application.
    /// This will also remove all associated restocking details.
    /// </summary>
    /// <param name="applicationId">The id of the application containing the confirmed felling detail.</param>
    /// <param name="confirmedFellingDetailId">The id of the confirmed felling detail to delete.</param>
    /// <param name="userId">The id of the user performing the deletion.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the confirmed felling detail has been deleted.</returns>
    Task<Result> DeleteConfirmedFellingDetailAsync(
        Guid applicationId,
        Guid confirmedFellingDetailId,
        Guid userId,
        CancellationToken cancellationToken);
}