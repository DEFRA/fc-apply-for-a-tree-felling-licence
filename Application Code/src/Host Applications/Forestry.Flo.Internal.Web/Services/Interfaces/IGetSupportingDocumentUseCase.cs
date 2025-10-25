using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Contract for a use case that handles retrieving or viewing a supporting document
/// from a felling licence application for an internal user.
/// </summary>
public interface IGetSupportingDocumentUseCase
{
    /// <summary>
    /// Retrieves a supporting document for a given application and document identifier.
    /// </summary>
    /// <param name="user">The internal user requesting the document.</param>
    /// <param name="applicationId">The ID of the felling licence application.</param>
    /// <param name="documentIdentifier">The ID of the supporting document.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>s
    /// A <see cref="Result{T}"/> containing a <see cref="FileContentResult"/> if successful,
    /// or a failure result if the document could not be retrieved.
    /// </returns>
    Task<Result<FileContentResult>> GetSupportingDocumentAsync(
        InternalUser user,
        Guid applicationId,
        Guid documentIdentifier,
        CancellationToken cancellationToken);
}
