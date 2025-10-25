using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for a use case that retrieves agent authority form documents for internal use.
/// </summary>
public interface IViewAgentAuthorityFormUseCase
{
    /// <summary>
    /// Retrieves the file contents for the documents that make up an Agent Authority Form.
    /// </summary>
    /// <param name="agentAuthorityId">The ID of the Agent Authority entry the form is attached to.</param>
    /// <param name="agentAuthorityFormId">The ID of the Agent Authority Form to retrieve the files for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing a <see cref="FileContentResult"/> if the files could be retrieved,
    /// otherwise a failure result.
    /// </returns>
    Task<Result<FileContentResult>> GetAgentAuthorityFormDocumentsAsync(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId,
        CancellationToken cancellationToken);
}
