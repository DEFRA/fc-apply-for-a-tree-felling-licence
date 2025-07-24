using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Services.Applicants.Repositories;

/// <summary>
/// Contract for a repository that deals with legacy documents.
/// </summary>
public interface ILegacyDocumentsRepository
{
    /// <summary>
    /// Get a specific legacy document by id.
    /// </summary>
    /// <param name="id">The id of the legacy document to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The legacy document with the given id if it exists or an error.</returns>
    public Task<Result<LegacyDocument, UserDbErrorReason>> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve all legacy documents in the repository.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all of the legacy documents in the repository.</returns>
    public Task<IEnumerable<LegacyDocument>> GetAllLegacyDocumentsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieve all legacy documents in the repository linked to one of the given woodland owner ids.
    /// </summary>
    /// <param name="woodlandOwnerIdIds">A list of woodland owner ids to retrieve legacy documents for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of all the legacy documents linked to the provided woodland owner ids.</returns>
    public Task<IEnumerable<LegacyDocument>> GetAllForWoodlandOwnerIdsAsync(IList<Guid> woodlandOwnerIdIds, CancellationToken cancellationToken);
}