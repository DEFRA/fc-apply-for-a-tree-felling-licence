using CSharpFunctionalExtensions;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Defines the contract for removing supporting documents from a felling licence application.
/// </summary>
public interface IRemoveSupportingDocumentUseCase
{
    /// <summary>
    /// Removes a supporting document from a felling licence application as an internal user.
    /// </summary>
    /// <param name="user">The internal user performing the removal.</param>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="documentId">The unique identifier of the document to remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    Task<Result> RemoveSupportingDocumentsAsync(
        InternalUser user,
        Guid applicationId,
        Guid documentId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Permanently removes a document from a felling licence application as an internal user.
    /// </summary>
    /// <param name="user">The internal user performing the removal.</param>
    /// <param name="applicationId">The unique identifier of the application.</param>
    /// <param name="documentIdentifier">The unique identifier of the document to permanently remove.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.</returns>
    Task<Result> RemoveFellingLicenceDocument(
        InternalUser user,
        Guid applicationId,
        Guid documentIdentifier,
        CancellationToken cancellationToken);
}