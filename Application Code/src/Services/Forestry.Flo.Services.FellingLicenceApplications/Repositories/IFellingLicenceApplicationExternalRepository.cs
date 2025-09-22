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

    Task AddSubmittedFlaPropertyDetailAsync(SubmittedFlaPropertyDetail submittedFlaPropertyDetail, CancellationToken cancellationToken);

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
    /// <param name="felingLicenceApplication">The FLA to be removed <see cref="FellingLicenceApplication"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<UnitResult<UserDbErrorReason>> DeleteFlaAsync(FellingLicenceApplication felingLicenceApplication, CancellationToken cancellationToken);

    Task<IList<AssigneeHistory>> GetCurrentlyAssignedAssigneeHistoryAsync(Guid applicationId, CancellationToken cancellationToken);

    Task<IList<CaseNote>> GetCaseNotesAsync(Guid applicationId, bool visibleToApplicantOnly, CancellationToken cancellationToken);

    Task<bool> VerifyWoodlandOwnerIdForApplicationAsync(Guid woodlandOwnerId, Guid applicationId, CancellationToken cancellationToken);

    Task<FellingLicenceApplicationStepStatus> GetApplicationStepStatus(Guid applicationId);
    Task<Maybe<SubmittedFlaPropertyCompartment>> GetSubmittedFlaPropertyCompartmentByIdAsync(Guid compartmentId, CancellationToken cancellationToken);
    Task<UnitResult<UserDbErrorReason>> UpdateSubmittedFlaPropertyCompartmentZonesAsync(Guid compartmentId, bool zone1, bool zone2, bool zone3, CancellationToken cancellationToken);
}