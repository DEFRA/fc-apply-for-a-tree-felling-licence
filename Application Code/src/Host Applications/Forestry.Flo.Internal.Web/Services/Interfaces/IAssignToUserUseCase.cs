using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for assigning felling licence applications to internal users.
/// </summary>
public interface IAssignToUserUseCase
{
    /// <summary>
    /// Retrieves a confirmation model for reassigning an application to a user with a specific role.
    /// </summary>
    /// <param name="applicationId">The ID of the application to reassign.</param>
    /// <param name="selectedRole">The role to assign.</param>
    /// <param name="returnUrl">The URL to return to after confirmation.</param>
    /// <param name="user">The internal user performing the action.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the confirmation model or an error.</returns>
    Task<Result<ConfirmReassignApplicationModel>> ConfirmReassignApplicationForRole(
        Guid applicationId,
        AssignedUserRole selectedRole,
        string returnUrl,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves details required to assign a felling licence application to a user.
    /// </summary>
    /// <param name="applicationId">The ID of the application to assign.</param>
    /// <param name="selectedRole">The role to assign.</param>
    /// <param name="returnUrl">The URL to return to after assignment.</param>
    /// <param name="user">The internal user performing the action.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing the assignment details model or an error.</returns>
    Task<Result<AssignToUserModel>> RetrieveDetailsToAssignFlaToUserAsync(
        Guid applicationId,
        AssignedUserRole selectedRole,
        string returnUrl,
        InternalUser user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Assigns a felling licence application to a specified internal user.
    /// </summary>
    /// <param name="applicationId">The ID of the application to assign.</param>
    /// <param name="assignToUserId">The ID of the user to assign the application to.</param>
    /// <param name="selectedRole">The role to assign.</param>
    /// <param name="selectedFcAreaCostCode">The cost code of the FC area.</param>
    /// <param name="performingUser">The internal user performing the assignment.</param>
    /// <param name="linkToApplication">A link to the application.</param>
    /// <param name="caseNote">An optional case note.</param>
    /// <param name="adminHubName">The name of the admin hub.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="visibleToApplicant">Whether the assignment is visible to the applicant.</param>
    /// <param name="visibleToConsultee">Whether the assignment is visible to the consultee.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AssignToUserAsync(
        Guid applicationId,
        Guid assignToUserId,
        AssignedUserRole selectedRole,
        string selectedFcAreaCostCode,
        InternalUser performingUser,
        string linkToApplication,
        string? caseNote,
        string adminHubName,
        CancellationToken cancellationToken,
        bool visibleToApplicant = false,
        bool visibleToConsultee = false);
}