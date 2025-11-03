using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Entities;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.AdminHubs.Repositories;

/// <summary>
/// Service class implementation of a <see cref="IAdminHubRepository"/>
/// </summary>
public class AdminHubRepository : IAdminHubRepository
{
    private readonly AdminHubContext _context;

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    public AdminHubRepository(AdminHubContext context) => _context = context  ?? throw new ArgumentNullException(nameof(context));

    ///<inheritdoc />
    public async Task<Result<IReadOnlyCollection<AdminHub>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var adminHubs = await _context.AdminHubs
            .Include(a => a.Areas)
            .Include(a => a.AdminOfficers)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<AdminHub>>(adminHubs);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> AddAdminOfficerAsync(
        Guid adminHubId,
        Guid adminOfficerUserId,
        CancellationToken cancellationToken)
    {
        var adminHubInDatabase = await _context.AdminHubs.SingleOrDefaultAsync(x => x.Id == adminHubId, cancellationToken);

        if (adminHubInDatabase == null)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound); 
        }

        if (await _context.AdminHubOfficers.AnyAsync(
                x => x.AdminHub.Id == adminHubId && x.UserAccountId == adminOfficerUserId,
                cancellationToken))
        {
            return UnitResult.Failure(UserDbErrorReason.NotUnique);
        }
        
        var adminHubUser = new AdminHubOfficer(adminHubInDatabase, adminOfficerUserId);
        await _context.AdminHubOfficers.AddAsync(adminHubUser, cancellationToken);
        return await _context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> RemoveAdminOfficerAsync(
        Guid adminHubId, 
        Guid adminOfficerUserId, 
        CancellationToken cancellationToken)
    {
        var userToRemove = await _context.AdminHubOfficers
            .SingleOrDefaultAsync(x=>x.UserAccountId == adminOfficerUserId && x.AdminHub.Id == adminHubId, cancellationToken);

        if (userToRemove == null)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }
      
        _context.AdminHubOfficers.Remove(userToRemove);
        return await _context.SaveEntitiesAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<UnitResult<UserDbErrorReason>> UpdateAdminHubDetailsAsync(
        Guid adminHubId,
        Guid newManager,
        string name,
        string address,
        CancellationToken cancellationToken)
    {
        var adminHubInDatabase = await _context.AdminHubs.SingleOrDefaultAsync(x => x.Id == adminHubId, cancellationToken);

        if (adminHubInDatabase == null)
        {
            return UnitResult.Failure(UserDbErrorReason.NotFound);
        }

        adminHubInDatabase.AdminManagerId = newManager;
        adminHubInDatabase.Name = name;
        adminHubInDatabase.Address = address;

        return await _context.SaveEntitiesAsync(cancellationToken);
    }
}