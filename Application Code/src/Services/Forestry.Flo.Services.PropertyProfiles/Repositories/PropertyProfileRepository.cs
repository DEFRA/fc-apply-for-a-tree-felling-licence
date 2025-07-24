using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.PropertyProfiles.Repositories;

public class PropertyProfileRepository : IPropertyProfileRepository
{
    private readonly PropertyProfilesContext _context;

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    public PropertyProfileRepository(PropertyProfilesContext context) => _context = context  ?? throw new ArgumentNullException(nameof(context));

    ///<inheritdoc />
    public PropertyProfile Add(PropertyProfile propertyProfile) => _context.PropertyProfiles.Add(propertyProfile).Entity;

    ///<inheritdoc />
     public async Task<Result<PropertyProfile, UserDbErrorReason>> UpdateAsync(PropertyProfile propertyProfile)
    {
        var existingProfile = await _context.PropertyProfiles.FindAsync(propertyProfile.Id);
        if (existingProfile == null)
        {
            return Result.Failure<PropertyProfile, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }
        _context.Entry(existingProfile).CurrentValues.SetValues(propertyProfile);
        return Result.Success<PropertyProfile, UserDbErrorReason>(propertyProfile);
    }

    ///<inheritdoc />
    public async Task<Result<PropertyProfile, UserDbErrorReason>> GetByIdAsync(Guid id,
        CancellationToken cancellationToken)
    {
        var propertyProfile = await _context
            .PropertyProfiles
            .Include(x => x.Compartments)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);

        return propertyProfile == null ? Result.Failure<PropertyProfile, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<PropertyProfile, UserDbErrorReason>(propertyProfile);
    }
    
    ///<inheritdoc />
    public async Task<IEnumerable<PropertyProfile>> ListAsync(ListPropertyProfilesQuery query,
        CancellationToken cancellationToken)
    {
        var propertyProfileQuery = _context
            .PropertyProfiles
            .Include(x => x.Compartments)
            .Where(p => p.WoodlandOwnerId == query.WoodlandOwnerId);

        if (query.Ids.Any())
        {
            propertyProfileQuery = propertyProfileQuery.Where(p => query.Ids.Any(i => i == p.Id));
        }

        return await propertyProfileQuery.ToListAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<IEnumerable<PropertyProfile>> ListAsync(IList<Guid> ids, CancellationToken cancellationToken)
    {
        var propertyProfileQuery = _context
            .PropertyProfiles
            .Include(x => x.Compartments)
            .Where(p => ids.Contains(p.Id));

        return await propertyProfileQuery.ToListAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<Result<PropertyProfile, UserDbErrorReason>> GetAsync(
        Guid id, 
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        var propertyProfile = await _context.PropertyProfiles
            .Include(u => u.Compartments)
            .SingleOrDefaultAsync(a => a.Id == id && a.WoodlandOwnerId == woodlandOwnerId,
                cancellationToken: cancellationToken);
        
        return propertyProfile == null ? Result.Failure<PropertyProfile, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<PropertyProfile, UserDbErrorReason>(propertyProfile);
    }

    ///<inheritdoc />
    public async Task<Result<IEnumerable<PropertyProfile>, UserDbErrorReason>> ListAsync(Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        var propertyProfiles = await _context.PropertyProfiles
            .Include(u => u.Compartments)
            .Where(a => a.WoodlandOwnerId == woodlandOwnerId)
            .ToListAsync();

        return propertyProfiles == null ? Result.Failure<IEnumerable<PropertyProfile>, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<IEnumerable<PropertyProfile>, UserDbErrorReason>(propertyProfiles);
    }

    ///<inheritdoc />
    public async Task<Result<bool>> CheckUserCanAccessPropertyProfileAsync(
        Guid propertyProfileId, 
        UserAccessModel userAccess,
        CancellationToken cancellationToken)
    {
        var woodlandOwnerId = await _context.PropertyProfiles
            .Where(p=>p.Id == propertyProfileId)
            .AsNoTracking()
            .Select(p => p.WoodlandOwnerId)
            .SingleOrDefaultAsync(cancellationToken);

        if (woodlandOwnerId == Guid.Empty)
        {
            return Result.Failure<bool>("Could not locate property profile with the given id");
        }

        var result = userAccess.CanManageWoodlandOwner(woodlandOwnerId);

        return Result.Success(result);
    }

    public async Task<Result<bool>> CheckUserCanAccessPropertyProfilesAsync(
        ListPropertyProfilesQuery query, 
        UserAccessModel userAccess,
        CancellationToken cancellationToken)
    {
        var propertyProfileQuery = _context.PropertyProfiles
            .Where(p => p.WoodlandOwnerId == query.WoodlandOwnerId)
            .AsNoTracking();

        if (query.Ids.Any())
        {
            propertyProfileQuery = propertyProfileQuery.Where(p => query.Ids.Any(i => i == p.Id));
        }

        var woodlandOwnerIds = await propertyProfileQuery
            .Select(p => p.WoodlandOwnerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (query.Ids.Any() && woodlandOwnerIds.NotAny())
        {
            return Result.Failure<bool>("Could not locate property profile with the given ids for the woodland owner specified");
        }

        //okay to be zero results, if the the woodland owner has no properties.
     
        if (woodlandOwnerIds.Select(userAccess.CanManageWoodlandOwner).Any(result => result == false))
        {
            return Result.Success(false);
        }

        return Result.Success(true);
    }
}