using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for a service used by the external app that updates a <see cref="FellingLicenceApplication"/>./>
/// </summary>
public interface IUpdateFellingLicenceApplicationForExternalUsers
{
    /// <summary>
    /// Submits the felling licence application with the provided id.
    /// </summary>
    /// <remarks>This may result in the application being in either <see cref="FellingLicenceStatus.Submitted"/> or
    /// <see cref="FellingLicenceStatus.WoodlandOfficerReview"/> state, depending on the current state of the application.</remarks>
    /// <param name="applicationId">The id of the application to submit.</param>
    /// <param name="userAccessModel">A populated <see cref="UserAccessModel"/> representing the permissions of the performing user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="SubmitFellingLicenceApplicationResponse"/> model if successful,
    /// otherwise <see cref="Result.Failure"/>.</returns>
    Task<Result<SubmitFellingLicenceApplicationResponse>> SubmitFellingLicenceApplicationAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds the given submitted FLA property detail to the application with the given id.
    /// //TODO this should take a Model as input, not the entity class
    /// </summary>
    /// <param name="propertyDetail">The property details to add to the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating the success or otherwise of adding the details.</returns>
    Task<Result> AddSubmittedFellingLicenceApplicationPropertyDetailAsync(
        SubmittedFlaPropertyDetail propertyDetail,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieves a Submitted FLA Property Compartment entity by ID.
    /// //TODO this method should be returning a model rather than an entity.
    /// </summary>
    /// <param name="compartmentId">The ID of the Submitted FLA Property Compartment to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<Result<SubmittedFlaPropertyCompartment>> GetSubmittedFlaPropertyCompartmentByIdAsync(
        Guid compartmentId, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to update the larch risk zones for a submitted FLA property compartment.
    /// </summary>
    /// <param name="compartmentId">The id of the compartment to update.</param>
    /// <param name="zone1">A flag to indicate the compartment is in risk zone 1.</param>
    /// <param name="zone2">A flag to indicate the compartment is in risk zone 2.</param>
    /// <param name="zone3">A flag to indicate the compartment is in risk zone 3.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the call was successful.</returns>
    Task<Result> UpdateSubmittedFlaPropertyCompartmentZonesAsync(
        Guid compartmentId, 
        bool zone1, 
        bool zone2, 
        bool zone3, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Converts the proposed felling and restocking details for the application with the given id to confirmed details.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userAccessModel">A <see cref="UserAccessModel"/> representing the user performing the process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the call was successful.</returns>
    Task<Result> ConvertProposedFellingAndRestockingToConfirmedAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Converts the proposed compartment designations for the application with the given id to submitted designations.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="userAccessModel">A <see cref="UserAccessModel"/> representing the user performing the process.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the call was successful.</returns>
    Task<Result> ConvertProposedCompartmentDesignationsToSubmittedAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the ten-year licence status for a specified application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="userAccess">The auth for the user performing the update.</param>
    /// <param name="isForTenYearLicence">A flag indicating whether the application is for a ten-year licence.</param>
    /// <param name="woodlandManagementPlanReference">The reference for the WMP related to the application, if applicable.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if the update was successful.</returns>
    Task<Result> UpdateTenYearLicenceStatusAsync(
        Guid applicationId,
        UserAccessModel userAccess,
        bool isForTenYearLicence,
        string? woodlandManagementPlanReference,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks the ten-year licence step as complete for the specified application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="userAccess">The auth for the user performing the update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if the update was successful.</returns>
    Task<Result> CompleteTenYearLicenceStepAsync(
        Guid applicationId,
        UserAccessModel userAccess,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the Agent Authority Form (AAF) step status for the specified application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="userAccess">The auth for the user performing the update.</param>
    /// <param name="aafStepStatus">The status to set for the AAF step (true = complete, false = incomplete).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating if the update was successful.</returns>
    Task<Result> UpdateAafStepAsync(
        Guid applicationId,
        UserAccessModel userAccess,
        bool aafStepStatus,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the PAWS designations data for a compartment in the specified application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update</param>
    /// <param name="userAccess">The auth for the user performing the update.</param>
    /// <param name="pawsDesignationsData">A model of the PAWS designations data to store.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<Result> UpdateApplicationPawsDesignationsDataAsync(
        Guid applicationId,
        UserAccessModel userAccess,
        PawsCompartmentDesignationsModel pawsDesignationsData,
        CancellationToken cancellationToken);
}