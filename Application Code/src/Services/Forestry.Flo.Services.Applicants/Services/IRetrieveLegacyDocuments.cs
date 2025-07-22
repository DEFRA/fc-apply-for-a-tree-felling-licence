using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.FileStorage.Model;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Contract for a class that retrieves legacy documents.
/// </summary>
public interface IRetrieveLegacyDocuments
{
    /// <summary>
    /// Retrieves a list of <see cref="LegacyDocumentModel"/> for the given user id.
    /// </summary>
    /// <param name="userId">The id of the user retrieving documents.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of <see cref="LegacyDocumentModel"/> that the current user can access.</returns>
    Task<Result<IList<LegacyDocumentModel>>> RetrieveLegacyDocumentsForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a specific document from storage.
    /// </summary>
    /// <param name="userId">The id of the user retrieving documents.</param>
    /// <param name="legacyDocumentId">The id of the document the user wishes to access.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="FileToStoreModel"/> representing the document file content.</returns>
    Task<Result<FileToStoreModel>> RetrieveLegacyDocumentContentAsync(Guid userId, Guid legacyDocumentId, CancellationToken cancellationToken);
}