using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.ConditionsBuilder.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.ConditionsBuilder.Repositories;

/// <summary>
/// An Entity Framework implementation of <see cref="IConditionsBuilderRepository"/>.
/// </summary>
public class ConditionsBuilderRepository : IConditionsBuilderRepository
{
    private readonly ConditionsBuilderContext _context;

    /// <summary>
    /// Creates a new instance of a <see cref="ConditionsBuilderRepository"/>.
    /// </summary>
    /// <param name="context">An entity framework database context to work with.</param>
    public ConditionsBuilderRepository(ConditionsBuilderContext context)
    {
        _context = Guard.Against.Null(context);
    }

    /// <inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    /// <inheritdoc />
    public async Task<IList<FellingLicenceCondition>> GetConditionsForApplicationAsync(
        Guid applicationId, 
        CancellationToken cancellationToken)
    {
        return await _context.FellingLicenceConditions
            .Where(x => x.FellingLicenceApplicationId == applicationId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ClearConditionsForApplicationAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var conditions = await _context.FellingLicenceConditions
            .Where(x => x.FellingLicenceApplicationId == applicationId)
            .ToListAsync(cancellationToken);

        _context.FellingLicenceConditions.RemoveRange(conditions);
    }

    /// <inheritdoc />
    public async Task SaveConditionsForApplicationAsync(IList<FellingLicenceCondition> conditions, CancellationToken cancellationToken)
    {
        await _context.FellingLicenceConditions.AddRangeAsync(conditions, cancellationToken);
    }
}