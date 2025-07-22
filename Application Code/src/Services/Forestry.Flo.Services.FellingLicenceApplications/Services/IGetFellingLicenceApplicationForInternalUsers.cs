using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

/// <summary>
/// Contract for a service that retrieves <see cref="FellingLicenceApplication"/> for Internal users
/// </summary>
public interface IGetFellingLicenceApplicationForInternalUsers
{
    /// <summary>
    /// Retrieves a <see cref="FellingLicenceApplication"/> by Id.
    /// </summary>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
   
    /// <returns>A populated <see cref="FellingLicenceApplication"/> entity.</returns>
    Task<Result<FellingLicenceApplication>> GetApplicationByIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all applications that need to be removed from the Consultation Public Register as have expired.
    /// </summary> 
    /// <param name="cancellationToken"> A cancellation token.</param>
    /// <returns>A result containing a list of <see cref="PublicRegisterPeriodEndModel"/> entities for the applications that need to be removed from the Consultation Public Register.</returns>
    Task<IList<PublicRegisterPeriodEndModel>> RetrieveApplicationsHavingExpiredOnTheConsultationPublicRegisterAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all finalised applications (having status of <see cref="FellingLicenceStatus.Approved"/> or <see cref="FellingLicenceStatus.Refused"/>
    /// or <see cref="FellingLicenceStatus.ReferredToLocalAuthority"/>) that need to be removed from the Decision Public Register as have expired.
    /// </summary> 
    /// <param name="cancellationToken"> A cancellation token.</param>
    /// <returns>A result containing a list of <see cref="PublicRegisterPeriodEndModel"/> entities for the applications that need to be removed from the Decision Public Register.</returns>
    Task<IList<PublicRegisterPeriodEndModel>> RetrieveFinalisedApplicationsHavingExpiredOnTheDecisionPublicRegisterAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the public register details for a given application id along with the required data for removing it from the PR.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve data for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="PublicRegisterPeriodEndModel"/> model of the public register details of the application,
    /// if there is one.</returns>
    Task<Maybe<PublicRegisterPeriodEndModel>> RetrievePublicRegisterForRemoval(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the notification details for a given application id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve the details for.</param>
    /// <param name="userAccessModel">A <see cref="UserAccessModel"/> to check the user has access to the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ApplicationNotificationDetails"/> model containing the details for the application,
    /// or <see cref="Result.Failure"/> if it could not be retrieved.</returns>
    Task<Result<ApplicationNotificationDetails>> RetrieveApplicationNotificationDetailsAsync(
        Guid applicationId,
        UserAccessModel userAccessModel,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the list of applicants and internal users assigned to an application.
    /// </summary>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="userAccessModel">A <see cref="UserAccessModel"/> to check the user has access to the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="ApplicationAssigneeModel"/> models representing the users assigned to the application.</returns>
    Task<Result<List<ApplicationAssigneeModel>>> GetApplicationAssignedUsers(Guid applicationId, UserAccessModel userAccessModel, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the status history for an application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve the status history for.</param>
    /// <param name="userAccessModel">A <see cref="UserAccessModel"/> to check the user has access to the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="StatusHistoryModel"/> models representing the status history of the application.</returns>
    Task<Result<List<StatusHistoryModel>>> GetApplicationStatusHistory(
        Guid applicationId, 
        UserAccessModel userAccessModel, 
        CancellationToken cancellationToken);
}