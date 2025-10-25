using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for a service that retrieves <see cref="FellingLicenceApplication"/> for External applicant users.
/// </summary>
public interface IGetFellingLicenceApplicationForExternalUsers
{
    /// <summary>
    /// Retrieves a list of all felling licence applications for the given woodland owner.
    /// </summary>
    /// TODO return type should be a model
    /// <param name="woodlandOwnerId">The id of the woodland owner.</param>
    /// <param name="userAccessModel">User access model to test against.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of felling licence applications for the woodland owner, or <see cref="Result.Failure"/> if
    /// the user cannot access the given woodland owner id.</returns>
    Task<Result<IEnumerable<FellingLicenceApplication>>> GetApplicationsForWoodlandOwnerAsync(
        Guid woodlandOwnerId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a <see cref="FellingLicenceApplication"/> by Id when the supplied <see cref="UserAccessModel"/> is satisfied.
    /// </summary>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="userAccessModel">User access model to test access against</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="FellingLicenceApplication"/> entity.</returns>
    Task<Result<FellingLicenceApplication>> GetApplicationByIdAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the FLA for the given Id is currently editable by an external applicant,
    /// i.e. is in the Draft, With Applicant or Returned To Applicant state.
    /// </summary>
    /// <param name="fellingLicenceApplicationId">The Id of the Felling Licence Application to check.</param>
    /// <param name="userAccessModel">User access model to test access against.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the FLA is currently editable by an external applicant user, otherwise false.</returns>
    Task<Result<bool>> GetIsEditable(
        Guid fellingLicenceApplicationId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the existing submitted FLA property detail for a given application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve the submitted FLA property detail for.</param>
    /// <param name="userAccessModel">User access model to test access against.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Maybe{SubmittedFlaPropertyDetail}"/> containing the existing submitted FLA property detail if found; otherwise, an empty value.
    /// </returns>
    Task<Result<Maybe<SubmittedFlaPropertyDetail>>> GetExistingSubmittedFlaPropertyDetailAsync(
        Guid applicationId, 
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);
}