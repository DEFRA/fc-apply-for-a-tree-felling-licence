using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

public interface IFellingLicenceApplicationExternalRepository : IFellingLicenceApplicationBaseRepository
{
    /// <summary>
    /// Adds a new felling licence application and saves it in the database.
    /// </summary>
    /// <param name="application">A felling licence application to add.</param>
    /// <param name="postFix">A string to append to the application reference.</param>
    /// <param name="startingOffset">An integer to use as the starting offset for the application reference.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The added felling licence application.</returns>
    Task<Result<FellingLicenceApplication, UserDbErrorReason>> CreateAndSaveAsync(
        FellingLicenceApplication application,
        string? postFix,
        int? startingOffset,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new felling licence application to the database but does not complete the transaction.
    /// </summary>
    /// <param name="application">A felling licence application to add.</param>
    ///     /// <param name="postFix">A string to append to the application reference.</param>
    /// <param name="startingOffset">An integer to use as the starting offset for the application reference.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The added felling licence application.</returns>
    Task<FellingLicenceApplication> AddAsync(
        FellingLicenceApplication application,
        string? postFix,
        int? startingOffset,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of felling licence applications for a given woodland owner.
    /// </summary>
    /// <param name="woodlandOwnerId">The id for the woodland owner to list the felling licence applications for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of felling licence applications.</returns>
    Task<IEnumerable<FellingLicenceApplication>> ListAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to retrieve a specific FLA identified by Id and woodland owner.
    /// </summary>
    /// <param name="applicationId">The Id of the FLA to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="FellingLicenceApplication"/> entity, if it was located with the given values, otherwise no value.</returns>
    Task<Maybe<FellingLicenceApplication>> GetAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the HabitatRestoration for a specific application and compartment, if one exists.
    /// </summary>
    /// <param name="applicationId">The Id of the FLA that owns the restoration.</param>
    /// <param name="compartmentId">The Id of the compartment to retrieve the restoration for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="HabitatRestoration"/> if found; otherwise <see cref="Maybe{T}.None"/>.</returns>
    Task<Maybe<HabitatRestoration>> GetHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns all HabitatRestoration records for the given application.
    /// </summary>
    Task<IReadOnlyList<HabitatRestoration>> GetHabitatRestorationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a HabitatRestoration for a given application/compartment when a matching linked profile exists.
    /// </summary>
    Task<UnitResult<UserDbErrorReason>> AddHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates a HabitatRestoration entity with provided values.
    /// </summary>
    Task<UnitResult<UserDbErrorReason>> UpdateHabitatRestorationAsync(
        HabitatRestoration habitatRestoration, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a HabitatRestoration for a specific application and compartment.
    /// </summary>
    Task<UnitResult<UserDbErrorReason>> DeleteHabitatRestorationAsync(
        Guid applicationId,
        Guid compartmentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes multiple HabitatRestoration records for a specific application.
    /// </summary>
    Task<UnitResult<UserDbErrorReason>> DeleteHabitatRestorationsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves details for a specific compartment on a felling licence application.
    /// </summary>
    /// <remarks>A value is only returned if a compartment is located with all of the given
    /// Id values matching.</remarks>
    /// <param name="applicationId">The Id of the FLA that the compartment belongs to.</param>
    /// <param name="woodlandOwnerId">The Id of the woodland owner that should own the FLA.</param>
    /// <param name="compartmentId">The Id of the compartment to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="ApplicationCompartmentDetail"/> instance, if the compartment was located by the given Ids, otherwise no value.</returns>
    Task<Maybe<ApplicationCompartmentDetail>> GetApplicationCompartmentDetailAsync(
        Guid applicationId,
        Guid woodlandOwnerId,
        Guid compartmentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the FLA for the given Id is currently editable by an external applicant,
    /// i.e. is in the Draft or With Applicant state.
    /// </summary>
    /// <param name="fellingLicenceApplicationId">The Id of the Felling Licence Application to check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the FLA is currently editable by an external applicant user, otherwise false.</returns>
    Task<bool> GetIsEditable(Guid fellingLicenceApplicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the <see cref="LinkedPropertyProfile"/> entity for the FLA with the given Id.
    /// </summary>
    /// <param name="applicationId">The Id of the FLA to retrieve the <see cref="LinkedPropertyProfile"/> for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<LinkedPropertyProfile> GetLinkedPropertyProfileAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a list of Guid's of the properties linked to an application.
    /// </summary>
    /// <param name="applicationId"></param>
    /// <param name="cancellationToken">A instance of <see cref="CancellationToken" /> </param>
    /// <returns>A list of Properties attached to the application </returns>
    Task<Maybe<List<Guid>>> GetApplicationComparmentIdsAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the existing submitted FLA property detail for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve the submitted FLA property detail for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Maybe{SubmittedFlaPropertyDetail}"/> containing the existing submitted FLA property detail if found; otherwise, an empty value.
    /// </returns>
    Task<Maybe<SubmittedFlaPropertyDetail>> GetExistingSubmittedFlaPropertyDetailAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new Submitted FLA Property Detail record to the database.
    /// </summary>
    /// <param name="submittedFlaPropertyDetail">The <see cref="SubmittedFlaPropertyDetail"/> entity to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    Task AddSubmittedFlaPropertyDetailAsync(SubmittedFlaPropertyDetail submittedFlaPropertyDetail, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the given Submitted FLA Property Detail record from the database.
    /// </summary>
    /// <param name="submittedFlaPropertyDetail">The <see cref="SubmittedFlaPropertyDetail"/> entity to delete.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An awaitable task.</returns>
    Task DeleteSubmittedFlaPropertyDetailAsync(SubmittedFlaPropertyDetail submittedFlaPropertyDetail, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes any existing Submitted FLA Property Details records for the given application.
    /// </summary>
    /// <param name="applicationId">The id of the application to delete the records for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<UnitResult<UserDbErrorReason>> DeleteSubmittedFlaPropertyDetailForApplicationAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the <see cref="FellingLicenceApplication"/> and the linked entities, recommended for use only with draft applicaitons.
    /// It will require any documents to be deleted by their specific location.
    /// </summary>
    /// <param name="fellingLicenceApplication">The FLA to be removed <see cref="FellingLicenceApplication"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="UnitResult"/> indicating if there were any failure.</returns>
    Task<UnitResult<UserDbErrorReason>> DeleteFlaAsync(FellingLicenceApplication fellingLicenceApplication, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the case notes for an application, optionally filtering by visibility to applicant only.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve case notes for.</param>
    /// <param name="visibleToApplicantOnly">A filter to indicate whether to only return case notes that are visible to applicants.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of case note entities.</returns>
    Task<IList<CaseNote>> GetCaseNotesAsync(Guid applicationId, bool visibleToApplicantOnly, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies that the given woodland owner Id is associated with the given application Id.
    /// </summary>
    /// <param name="woodlandOwnerId">The woodland owner id to check.</param>
    /// <param name="applicationId">The application id to check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the ids match, otherwise false.</returns>
    Task<bool> VerifyWoodlandOwnerIdForApplicationAsync(Guid woodlandOwnerId, Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current step status for the application with the given Id.
    /// </summary>
    /// <param name="applicationId">The application to retrieve the step status for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="FellingLicenceApplicationStepStatus"/> for the given application id.</returns>
    Task<FellingLicenceApplicationStepStatus> GetApplicationStepStatus(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a specific Submitted FLA Property Compartment by its ID.
    /// </summary>
    /// <param name="compartmentId">The ID of the compartment to retrieve by its ID.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="SubmittedFlaPropertyCompartment"/> if one exists for the given ID, otherwise <see cref="Maybe{T}.None"/></returns>
    Task<Maybe<SubmittedFlaPropertyCompartment>> GetSubmittedFlaPropertyCompartmentByIdAsync(Guid compartmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the zone information for a specific Submitted FLA Property Compartment.
    /// </summary>
    /// <param name="compartmentId">The ID of the submitted FLA property compartment to update.</param>
    /// <param name="zone1">A flag to indicate the compartment is in Zone 1.</param>
    /// <param name="zone2">A flag to indicate the compartment is in Zone 2.</param>
    /// <param name="zone3">A flag to indicate the compartment is in Zone 3.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="UnitResult"/> indicating if the operation failed.</returns>
    Task<UnitResult<UserDbErrorReason>> UpdateSubmittedFlaPropertyCompartmentZonesAsync(Guid compartmentId, bool zone1, bool zone2, bool zone3, CancellationToken cancellationToken);

    /// <summary>
    /// Updates any existing Woodland Officer Review task completion flags for tasks that will need to be redone on
    /// resubmitting the application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to update.</param>
    /// <param name="updatedDate">The date and time that the resubmission is triggered.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="UnitResult"/> struct indicating if the operation fails.</returns>
    Task<UnitResult<UserDbErrorReason>> UpdateExistingWoodlandOfficerReviewFlagsForResubmission(
        Guid applicationId,
        DateTime updatedDate,
        CancellationToken cancellationToken);
}