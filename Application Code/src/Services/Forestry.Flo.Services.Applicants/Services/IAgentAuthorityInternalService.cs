using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FileStorage.Model;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Contract for a service that implements tasks related to <see cref="AgentAuthority"/> entities,
/// intended for use by the Internal web app only.
/// </summary>
public interface IAgentAuthorityInternalService
{
    /// <summary>
    /// Retrieves the file contents for the documents that make up an Agent Authority Form.
    /// </summary>
    /// <param name="request">A populated <see cref="GetAgentAuthorityFormDocumentsRequest"/> request model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="FileToStoreModel"/> if the files could be retrieved, otherwise <see cref="Result.Failure"/>.</returns>
    /// <remarks>If the AAF requested has multiple files then the response will take the form of a single ZIP file
    /// containing all of the documents.</remarks>
    Task<Result<FileToStoreModel>> GetAgentAuthorityFormDocumentsAsync(
        GetAgentAuthorityFormDocumentsInternalRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the relevant ids for the <see cref="AgentAuthority"/> and <see cref="AgentAuthorityForm"/> entities
    /// relevant to the given <see cref="GetAgentAuthorityFormRequest"/>, in order to then be able to download
    /// the specific documents.
    /// </summary>
    /// <param name="request">A populated <see cref="GetAgentAuthorityFormRequest"/> request model.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="GetAgentAuthorityFormResponse"/> if possible, otherwise <see cref="Result.Failure"/>.</returns>
    Task<Result<GetAgentAuthorityFormResponse>> GetAgentAuthorityFormAsync(
        GetAgentAuthorityFormRequest request,
        CancellationToken cancellationToken);
}