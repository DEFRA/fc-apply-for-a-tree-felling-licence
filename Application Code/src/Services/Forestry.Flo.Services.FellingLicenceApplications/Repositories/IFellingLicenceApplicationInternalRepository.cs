using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

/// <summary>
/// Defines the contract for a repository of <see cref="FellingLicenceApplication"/> entities
/// that is intended only for use by the Internal user web app.
/// </summary>
public interface IFellingLicenceApplicationInternalRepository : IFellingLicenceApplicationBaseRepository
{
    /// <summary>
    /// Attempts to retrieve a <see cref="FellingLicenceApplication"/> with the given Id from the repository.
    /// </summary>
    /// <param name="applicationId">The Id of the required Felling Licence Application</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The <see cref="FellingLicenceApplication"/> instance, if it was found.</returns>
    Task<Maybe<FellingLicenceApplication>> GetAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to assign a felling licence application to a given staff member for a given role.  Will unassign
    /// the application from any other staff member that is already assigned to it in the given role.
    /// </summary>
    /// <param name="applicationId">The Id of the felling licence application to assign to.</param>
    /// <param name="assignedUserId">The Id of the user being assigned to the felling licence application.</param>
    /// <param name="assignedRole">The role to assign the user to the felling licence application.</param>
    /// <param name="timestamp">The timestamp to apply to the assignee history entry or entries.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A flag indicating if the provided assignment already exists, and the Id of any user that was unassigned.</returns>
    Task<(bool UserAlreadyAssigned, Maybe<Guid> UserUnassigned)> AssignFellingLicenceApplicationToStaffMemberAsync(
        Guid applicationId,
        Guid assignedUserId,
        AssignedUserRole assignedRole,
        DateTime timestamp,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to unassign a felling licence application for a given staff member Guid.
    /// </summary>
    /// <param name="applicationId">The Id of the felling licence application to remove assignment from.</param>
    /// <param name="assignedUserId">The Id of the user being unassigned from the felling licence application.</param>
    /// <param name="timestamp">The timestamp to apply to the assignee history entry or entries.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of unassigning the Internal user from the application.</returns>
    Task<UnitResult<UserDbErrorReason>> RemoveAssignedFellingLicenceApplicationStaffMemberAsync(
        Guid applicationId,
        Guid assignedUserId,
        DateTime timestamp,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes any current AssigneeHistory entries on a given felling licence application for the given roles.
    /// </summary>
    /// <param name="applicationId">The ID of the application to remove the assignee history entries from.</param>
    /// <param name="roles">An array of <see cref="AssignedUserRole"/> which should be removed from the application.</param>
    /// <param name="timestamp">The timestamp to apply to the removed assignee history entries.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the operations.</returns>
    Task<UnitResult<UserDbErrorReason>> RemoveAssignedRolesFromApplicationAsync(
        Guid applicationId,
        AssignedUserRole[] roles,
        DateTime timestamp,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of <see cref="AssigneeHistory"/> entities for a particular application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve assignee history entries for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="AssigneeHistory"/> for the specific application.</returns>
    Task<IList<AssigneeHistory>> GetAssigneeHistoryForApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of <see cref="StatusHistory"/> entities for a particular application.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve status history entries for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="StatusHistory"/> for the specific application.</returns>
    Task<IList<StatusHistory>> GetStatusHistoryForApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists submitted felling licence applications according to user options selection
    /// </summary>
    /// <param name="assignedToUserAccountIdOnly"></param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="userFellingLicenceSelectionOptions">The user felling licence selection options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IList<FellingLicenceApplication>> ListByIncludedStatus(
        bool assignedToUserAccountIdOnly,
        Guid userId,
        IList<FellingLicenceStatus> userFellingLicenceSelectionOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds an external access link for an invited external consultee.
    /// </summary>
    /// <param name="accessLink">The access link details</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result of the operation with optional error details</returns>
    Task<UnitResult<UserDbErrorReason>> AddExternalAccessLinkAsync(ExternalAccessLink accessLink,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the external access link for the invited external consultee.
    /// </summary>
    /// <param name="accessLink">The access link to update</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns> result of the operation with optional error details</returns>
    Task<UnitResult<UserDbErrorReason>> UpdateExternalAccessLinkAsync(ExternalAccessLink accessLink,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a list of application external links for a given user,an application id and a purpose.
    /// </summary>
    /// <param name="applicationId">The id of the application</param>
    /// <param name="name">The consultee name</param>
    /// <param name="userEmail">The user email</param>
    /// <param name="purpose">The purpose of providing access</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The list of external links</returns>
    Task<IList<ExternalAccessLink>> GetUserExternalAccessLinksByApplicationIdAndPurposeAsync(Guid applicationId,
        string name, string userEmail, string purpose, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the proposed felling and restocking details for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The proposed felling and restocking details for the application.</returns>
    Task<Result<(List<ProposedFellingDetail>, List<ProposedRestockingDetail>)>> GetProposedFellingAndRestockingDetailsForApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the confirmed felling and restocking details for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The confirmed felling and restocking details for the application.</returns>
    Task<Result<(List<ConfirmedFellingDetail>, List<ConfirmedRestockingDetail>)>> GetConfirmedFellingAndRestockingDetailsForApplicationAsync(
            Guid applicationId,
            CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the woodland officer review entity for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The woodland officer review entity for the application.</returns>
    Task<Maybe<WoodlandOfficerReview>> GetWoodlandOfficerReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the woodland officer review entity for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The woodland officer review entity for the application.</returns>
    Task<Maybe<ApproverReview>> GetApproverReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the admin officer review entity for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The admin officer review entity for the application.</returns>
    Task<Maybe<AdminOfficerReview>> GetAdminOfficerReviewAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the public register entity for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The public register entity for the application.</returns>
    Task<Maybe<PublicRegister>> GetPublicRegisterAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new public register entity to the repository.
    /// </summary>
    /// <param name="publicRegister">A <see cref="PublicRegister"/> to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>This method does not call save changes on the <see cref="IUnitOfWork"/> instance
    /// for the repository, it is left to the caller to do so.</remarks>
    Task AddPublicRegisterAsync(
        PublicRegister publicRegister, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds the required decision public register details to the public register entity in the repository for the application.
    /// </summary>
    /// <param name="applicationId">The id of the application to which the public register details belong to.</param>
    /// <param name="publishedDateTime">The point in time that the application was published to the decision public register</param>
    /// <param name="expiryDateTime">The point in time that the application expires on the decision public register, and so would need to be removed.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<UnitResult<UserDbErrorReason>> AddDecisionPublicRegisterDetailsAsync(
        Guid applicationId,
        DateTime publishedDateTime,
        DateTime expiryDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the decision public register entry for the application in the local system to removed.
    /// </summary>
    /// <param name="applicationId">The id of the application to which the decision public register details need to be set to expired.</param>
    /// <param name="removedDateTime">The point in time that the application was removed from the decision public register.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<UnitResult<UserDbErrorReason>> ExpireDecisionPublicRegisterEntryAsync(
        Guid applicationId,
        DateTime removedDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the consultation public register entry for the application in the local system to removed.
    /// </summary>
    /// <param name="applicationId">The id of the application to which the consultation public register details need to be set to expired.</param>
    /// <param name="removedDateTime">The point in time that the application was removed from the consultation public register.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task<UnitResult<UserDbErrorReason>> ExpireConsultationPublicRegisterEntryAsync(
        Guid applicationId,
        DateTime removedDateTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new woodland officer review entity to the repository.
    /// </summary>
    /// <param name="woodlandOfficerReview">A <see cref="WoodlandOfficerReview"/> to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>This method does not call save changes on the <see cref="IUnitOfWork"/> instance
    /// for the repository, it is left to the caller to do so.</remarks>
    Task AddWoodlandOfficerReviewAsync(
        WoodlandOfficerReview woodlandOfficerReview,
        CancellationToken cancellationToken);

    /// <summary>
    /// Add a case note to the repository.
    /// </summary>
    /// <param name="caseNote">A <see cref="CaseNote"/> to save.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>This method does not call save changes on the <see cref="IUnitOfWork"/> instance
    /// for the repository, it is left to the caller to do so.</remarks>
    Task AddCaseNoteAsync(
        CaseNote caseNote,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a valid <see cref="ExternalAccessLink"/> based on the given application id, access code
    /// and email address.  Will not return an entry where the expiry date is prior to the current point in
    /// time given by the parameter <paramref name="now"/>.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve an external access link for.</param>
    /// <param name="accessCode">The access code of the external access link to retrieve.</param>
    /// <param name="emailAddress">The email address of the external access link to retrieve.</param>
    /// <param name="now">The current point in time, to verify the expiry time of access links.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A valid <see cref="ExternalAccessLink"/> if one is found, otherwise Maybe.None.</returns>
    Task<Maybe<ExternalAccessLink>> GetValidExternalAccessLinkAsync(
        Guid applicationId,
        Guid accessCode,
        string emailAddress,
        DateTime now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the existing consultee comments for a felling licence application, optionally filtered to
    /// a specific author email address.
    /// </summary>
    /// <param name="applicationId">The id of the felling licence application to retrieve comments for.</param>
    /// <param name="emailAddress">An optional email address of the author to retrieve comments for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of existing consultee comments for the given application and author email.</returns>
    Task<IList<ConsulteeComment>> GetConsulteeCommentsAsync(
        Guid applicationId,
        string? emailAddress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new consultee comment to the repository.
    /// </summary>
    /// <param name="consulteeComment">The consultee comment to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="UnitResult{E}"/> representing the success of the operation.</returns>
    Task<UnitResult<UserDbErrorReason>> AddConsulteeCommentAsync(
        ConsulteeComment consulteeComment,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets all applications that have exceeded their final action date without being processed.
    /// </summary>
    /// <param name="currentTime">The current date time.</param>
    /// <param name="thresholdBeforeDate">The threshold before a final action date for an application to be retrieved.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of applications that have exceeded their final action date without being processed.</returns>
    Task<IList<FellingLicenceApplication>> GetApplicationsThatAreWithinThresholdOfFinalActionDateAsync(
        DateTime currentTime,
        TimeSpan thresholdBeforeDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets all applications that have been with the applicant for the threshold amount or more and no recent email sent.
    /// </summary>
    /// <param name="currentTime">The current date time.</param>
    /// <param name="thresholdAfterStatusCreatedDate">The threshold after the status was set to with applicant for an application to be retrieved.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of applications that have been with the applicant for longer then the threshold timespan and not been sent a notification recently.</returns>
    Task<IList<FellingLicenceApplication>> GetApplicationsThatAreWithinThresholdOfWithdrawalNotificationDateAsync(
        DateTime currentTime,
        TimeSpan thresholdAfterStatusCreatedDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets all applications that have been with the applicant for the threshold amount.
    /// </summary>
    /// <param name="currentTime">The current date time.</param>
    /// <param name="thresholdAfterStatusCreatedDate">The threshold after the status was set to with applicant for an application to be retrieved.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of applications that have been with the applicant for longer then the threshold timespan.</returns>
    Task<IList<FellingLicenceApplication>> GetApplicationsThatAreWithinThresholdAutomaticWithdrawalDateAsync(
        DateTime currentTime,
        TimeSpan thresholdAfterStatusCreatedDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets all finalized applications that have expired Consultation Public Register Periods.
    /// </summary>
    /// <param name="currentTime">The current date time.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="FellingLicenceApplication"/> entities for applications that have expired Consultation Public Register Periods.</returns>
    Task<IList<FellingLicenceApplication>> GetApplicationsWithExpiredConsultationPublicRegisterPeriodsAsync(
        DateTime currentTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets all finalized applications that have expired Decision Public Register Periods.
    /// </summary>
    /// <param name="currentTime">The current date time.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="FellingLicenceApplication"/> entities for finalised applications that have expired Decision Public Register Periods.</returns>
    Task<IList<FellingLicenceApplication>> GetFinalisedApplicationsWithExpiredDecisionPublicRegisterPeriodsAsync(
        DateTime currentTime,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes a query against the Felling Licence applications data.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IList<FellingLicenceApplication>> ExecuteFellingLicenceApplicationsReportQueryAsync(
        FellingLicenceApplicationsReportQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes the update to change the area code assigned to an application.
    /// </summary>
    /// <remarks>
    /// Note that this should also update the application reference, as the first segment is the area code.
    /// </remarks>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="newAreaCode">The area code to assign, which will also update the first segment of the application reference.</param>
    /// <param name="adminHubName">The admin hub that pertains to the new area code.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The operation UnitResult, either a success or failure object containing a <see cref="UserDbErrorReason"/>.</returns>
    Task<UnitResult<UserDbErrorReason>> UpdateAreaCodeAsync(
        Guid applicationId, 
        string newAreaCode,
        string? adminHubName,
        CancellationToken cancellationToken);

    Task<UnitResult<UserDbErrorReason>> AddOrUpdateApproverReviewAsync(ApproverReview approverReview, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the larch check details entity for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve details for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The larch check details entity for the application.</returns>
    Task<Maybe<LarchCheckDetails>> GetLarchCheckDetailsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Adds or updates a larch check details entity in the repository.
    /// </summary>
    /// <param name="larchCheck">The larch check details to add or update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<UnitResult<UserDbErrorReason>> AddOrUpdateLarchCheckDetailsAsync(
        LarchCheckDetails larchCheck,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes any existing approver review entry for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to delete the approver review for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="UnitResult{UserDbErrorReason}"/> representing the result of the operation.
    /// </returns>
    Task<UnitResult<UserDbErrorReason>> DeleteApproverReviewAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to set the approver id of the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to update.</param>
    /// <param name="approverId">The id of the approver to set on the application, or null to remove any existing one.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the update was successful.</returns>
    Task<UnitResult<UserDbErrorReason>> SetApplicationApproverAsync(
        Guid applicationId,
        Guid? approverId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all <see cref="SubmittedFlaPropertyCompartment"/> entities associated with a specific felling licence application.
    /// </summary>
    /// <param name="applicationId">The unique identifier of the felling licence application.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Result{T, TError}"/> containing a list of <see cref="SubmittedFlaPropertyCompartment"/> if successful.
    /// </returns>
    Task<Result<List<SubmittedFlaPropertyCompartment>>> GetSubmittedFlaPropertyCompartmentsByApplicationIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves all documents associated with a specific felling licence application.
    /// </summary>
    /// <param name="applicationId">The ID of the application to retrieve all the documents for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of documents linked to the specified application.</returns>
    Task<List<Document>> GetApplicationDocumentsAsync(
        Guid applicationId,
        CancellationToken cancellationToken);
}