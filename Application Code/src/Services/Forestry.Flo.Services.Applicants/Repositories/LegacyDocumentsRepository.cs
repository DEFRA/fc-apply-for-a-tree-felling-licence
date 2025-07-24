using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.Applicants.Repositories;

/// <summary>
/// Entity framework implementation for <see cref="ILegacyDocumentsRepository"/>.
/// </summary>
public class LegacyDocumentsRepository : ILegacyDocumentsRepository
{
    private readonly ApplicantsContext _context;

    /// <summary>
    /// Creates a new instance of a <see cref="LegacyDocumentsRepository"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public LegacyDocumentsRepository(ApplicantsContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Result<LegacyDocument, UserDbErrorReason>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await _context.LegacyDocuments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return document == null
            ? Result.Failure<LegacyDocument, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<LegacyDocument, UserDbErrorReason>(document);
    }

    ///<inheritdoc />
    public async Task<IEnumerable<LegacyDocument>> GetAllLegacyDocumentsAsync(CancellationToken cancellationToken)
    {
        return await _context.LegacyDocuments.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    ///<inheritdoc />
    public async Task<IEnumerable<LegacyDocument>> GetAllForWoodlandOwnerIdsAsync(IList<Guid> woodlandOwnerIdIds, CancellationToken cancellationToken)
    {
        var distinctWoodlandOwnerIds = woodlandOwnerIdIds.Distinct();

        return await _context.LegacyDocuments
            .Where(x => distinctWoodlandOwnerIds.Contains(x.WoodlandOwnerId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}