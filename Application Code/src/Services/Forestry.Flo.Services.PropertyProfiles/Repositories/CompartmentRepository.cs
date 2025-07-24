using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.PropertyProfiles.Repositories;

public class CompartmentRepository : ICompartmentRepository
{
    private readonly PropertyProfilesContext _context;

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    public CompartmentRepository(PropertyProfilesContext context) =>
        _context = context ?? throw new ArgumentNullException(nameof(context));

    ///<inheritdoc />
    public Compartment Add(Compartment compartment) => _context.Compartments.Add(compartment).Entity;

    ///<inheritdoc />
    public Compartment Remove(Compartment compartment) => _context.Compartments.Remove(compartment).Entity;

    ///<inheritdoc />
    public async Task<Result<Compartment, UserDbErrorReason>> UpdateAsync(Compartment compartment)
    {
        var existingProfile = await _context.Compartments.FindAsync(compartment.Id);
        if (existingProfile == null)
        {
            return Result.Failure<Compartment, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        _context.Entry(existingProfile).CurrentValues.SetValues(compartment);
        return Result.Success<Compartment, UserDbErrorReason>(compartment);
    }

    ///<inheritdoc />
    public async Task<Result<Compartment, UserDbErrorReason>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var compartment = await _context
            .Compartments
            .Include(x => x.PropertyProfile)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);

        return compartment == null
            ? Result.Failure<Compartment, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<Compartment, UserDbErrorReason>(compartment);
    }

    ///<inheritdoc />
    public async Task<Result<Compartment, UserDbErrorReason>> GetAsync(Guid id, Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        var compartment = await _context.Compartments
            .Include(u => u.PropertyProfile)
            .SingleOrDefaultAsync(a => a.Id == id && a.PropertyProfile.WoodlandOwnerId == woodlandOwnerId,
                cancellationToken: cancellationToken);

        return compartment == null
            ? Result.Failure<Compartment, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<Compartment, UserDbErrorReason>(compartment);
    }

    public async Task<IEnumerable<Compartment>> ListAsync(Guid propertyProfileId, Guid woodlandOwnerId,
        CancellationToken cancellationToken) =>
        await _context
            .Compartments
            .Include(c => c.PropertyProfile)
            .Where(c => c.PropertyProfile.WoodlandOwnerId == woodlandOwnerId &&
                        c.PropertyProfileId == propertyProfileId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<Compartment>> ListAsync(
        IList<Guid> ids, 
        CancellationToken cancellationToken)
    {
        var compartments = _context
            .Compartments
            .Include(x => x.PropertyProfile)
            .Where(x => ids.Contains(x.Id));

        return await compartments.ToListAsync(cancellationToken);
    }

    public async Task<Result<bool>> CheckUserCanAccessCompartmentAsync(Guid compartmentId, UserAccessModel userAccessModel,
        CancellationToken cancellationToken)
    {
        var woodlandOwnerId = await _context.Compartments
            .Where(c => c.Id == compartmentId)
            .Include(u => u.PropertyProfile)
            .Select(c=>c.PropertyProfile.WoodlandOwnerId)
            .SingleOrDefaultAsync(cancellationToken);
        
        if (woodlandOwnerId == Guid.Empty)
        {
            return Result.Failure<bool>("Could not locate property profile with the given id");
        }

        var result = userAccessModel.CanManageWoodlandOwner(woodlandOwnerId);

        return Result.Success(result);
    }
}