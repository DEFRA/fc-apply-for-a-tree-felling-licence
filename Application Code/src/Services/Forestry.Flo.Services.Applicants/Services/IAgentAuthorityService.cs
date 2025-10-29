using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FileStorage.Model;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Contract for a service that implements tasks related to <see cref="AgentAuthority"/> entities.
/// </summary>
public interface IAgentAuthorityService
{
    /// <summary>
    /// Adds a new <see cref="AgentAuthority"/> entity to the system along with the woodland owner.
    /// </summary>
    /// <param name="request">A populated <see cref="AddAgentAuthorityRequest"/> model with details of
    /// the agent authority to be added.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="AddAgentAuthorityResponse"/> model, or a failure result.</returns>
    Task<Result<AddAgentAuthorityResponse>> AddAgentAuthorityAsync(AddAgentAuthorityRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Removes an <see cref="AgentAuthority"/> entity with the given id.
    /// </summary>
    /// <param name="agentAuthorityId">The id of the <see cref="AgentAuthority"/> to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the operation.</returns>
    Task<Result> DeleteAgentAuthorityAsync(Guid agentAuthorityId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of all Agent Authority entries for a user.
    /// </summary>
    /// <param name="userId">The id of the performing user.</param>
    /// <param name="agencyId">The id of the agency to retrieve data for.</param>
    /// <param name="filter">An optional array of <see cref="AgentAuthorityStatus"/> values to filter the results by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="GetAgentAuthoritiesResponse"/> model, or Failure if the retrieval errored.</returns>
    Task<Result<GetAgentAuthoritiesResponse>> GetAgentAuthoritiesAsync(
        Guid userId,
        Guid agencyId,
        AgentAuthorityStatus[]? filter, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the Agent Authority Forms and the woodland owner details for the provided authority Id linked to the performing user.
    /// </summary>
    /// <param name="userId">The id of the performing user.</param>
    /// <param name="agentAuthorityId">The id of the <see cref="AgentAuthority"/> to get.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="AgentAuthorityFormsWithWoodlandOwnerResponseModel"/>, or Failure if the retrieval errored.</returns>
    Task<Result<AgentAuthorityFormsWithWoodlandOwnerResponseModel>> GetAgentAuthorityFormDocumentsByAuthorityIdAsync(
        Guid userId,
        Guid agentAuthorityId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to upload a new AAF document to an Agent Authority.
    /// </summary>
    /// <param name="request">A populated <see cref="AddAgentAuthorityFormRequest"/> request model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> containing details of the created the <see cref="AgentAuthorityForm"/> entity if successful,
    /// otherwise failure details.</returns>
    /// <remarks>This call will update the <see cref="AgentAuthority"/> status to <see cref="AgentAuthorityStatus.FormUploaded"/>
    /// if it is not already, and update any existing <see cref="AgentAuthorityForm"/> entities with no Valid To Date to have
    /// a Valid To Date of now.</remarks>
    Task<Result<AgentAuthorityFormResponseModel>> AddAgentAuthorityFormAsync(
        AddAgentAuthorityFormRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes the <see cref="AgentAuthorityForm"/> with the given id, i.e. sets the Valid To Date on the entity to now.
    /// </summary>
    /// <param name="request">A populated <see cref="RemoveAgentAuthorityFormRequest"/> request model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    /// <remarks>If the <see cref="AgentAuthorityForm"/> with the given ID already has a Valid To Date, this will not
    /// be updated.  If the <see cref="AgentAuthority"/> linked to the <see cref="AgentAuthorityForm"/> with the given ID no longer
    /// has a valid <see cref="AgentAuthorityForm"/>, it's status will be updated to <see cref="AgentAuthorityStatus.Created"/>.</remarks>
    Task<Result> RemoveAgentAuthorityFormAsync(
        RemoveAgentAuthorityFormRequest request, 
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the file contents for the documents that make up an Agent Authority Form.  Verifies the permissions of the
    /// applicant user before accessing the file content.
    /// </summary>
    /// <param name="request">A populated <see cref="GetAgentAuthorityFormDocumentsRequest"/> request model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="FileToStoreModel"/> if the files could be retrieved, otherwise <see cref="Result.Failure"/>.</returns>
    /// <remarks>If the AAF requested has multiple files then the response will take the form of a single ZIP file
    /// containing all of the documents.</remarks>
    Task<Result<FileToStoreModel>> GetAgentAuthorityFormDocumentsAsync(
        GetAgentAuthorityFormDocumentsRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks the system for an approved AAF for the given agency and woodland owner specified
    /// in the request.
    /// </summary>
    /// <param name="request">A populated <see cref="EnsureAgencyOwnsWoodlandOwnerRequest"/> model.</param>
    /// <param name="validStatuses">Valid statuses to check for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><see langword="true"/> if the agency has an approved AAF for the woodland owner, otherwise <see langword="false"/>.</returns>
    Task<Result<bool>> EnsureAgencyAuthorityStatusAsync(
        EnsureAgencyOwnsWoodlandOwnerRequest request,
        AgentAuthorityStatus[] validStatuses,
        CancellationToken cancellationToken);

    /// <summary>
    /// Looks up any existing approved agent authority forms for a particular woodland owner
    /// and returns the linked agency if one is found.
    /// </summary>
    /// <param name="woodlandOwnerId">The id of the woodland owner</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A populated <see cref="AgencyModel"/> model with the Agency details
    /// if one is found, otherwise <see cref="Maybe.None"/>.</returns>
    Task<Maybe<AgencyModel>> GetAgencyForWoodlandOwnerAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes a newly created agency and removes references to it from a user account.
    /// </summary>
    /// <param name="userAccount">The user account linked to the agency to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the agency has been successfully removed, or an error message if unsuccessful.</returns>
    /// <remarks>This should only be executed for newly created user accounts and agencies.</remarks>
    Task<Result> RemoveNewAgencyAsync(
        UserAccount userAccount,
        CancellationToken cancellationToken);

    /// A <see cref="Maybe{T}"/> containing an <see cref="AgentAuthorityModel"/> if agent authority details exist for the woodland owner,
    /// or <see cref="Maybe.None"/> if no agent authority is found.
    /// </returns>
    /// <remarks>
    /// The returned model includes details about the agent authority relationship, such as the current status, 
    /// associated forms, and linked woodland owner information.
    /// </remarks>
    Task<Maybe<AgentAuthorityModel>> GetAgentAuthorityForWoodlandOwnerAsync(Guid woodlandOwnerId, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to locate an AgentAuthority id by Agency and Woodland Owner ids.
    /// </summary>
    /// <param name="agencyId">The id of the agency.</param>
    /// <param name="woodlandOwnerId">The id of the woodland owner.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Maybe containing the AgentAuthority id if found.</returns>
    Task<Maybe<Guid>> FindAgentAuthorityIdAsync(Guid agencyId, Guid woodlandOwnerId, CancellationToken cancellationToken);
}