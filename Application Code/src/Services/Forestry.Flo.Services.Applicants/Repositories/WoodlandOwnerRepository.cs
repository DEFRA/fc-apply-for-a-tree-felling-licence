using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.Applicants.Repositories;

public class WoodlandOwnerRepository : IWoodlandOwnerRepository
{
    private readonly ApplicantsContext _context;

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    public WoodlandOwnerRepository(ApplicantsContext context) => _context = context  ?? throw new ArgumentNullException(nameof(context));

    ///<inheritdoc />
    public async Task<Result<WoodlandOwner, UserDbErrorReason>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var woodlandOwner = await _context
            .WoodlandOwners
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);

        return woodlandOwner == null 
            ? Result.Failure<WoodlandOwner, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<WoodlandOwner, UserDbErrorReason>(woodlandOwner);
    }

    ///<inheritdoc />
    public async Task<Result<IEnumerable<WoodlandOwner>, UserDbErrorReason>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.WoodlandOwners.ToListAsync(cancellationToken);
    }

    ///<inheritdoc />
    public void Remove(WoodlandOwner woodlandOwner) => _context.WoodlandOwners.Remove(woodlandOwner);

    ///<inheritdoc />
    public async Task<IEnumerable<UserAccount>> GetActiveApplicantUsers(Guid woodlandOwnerId, CancellationToken cancellationToken)
    {
        // Get the agent authority that relates to the woodland owner

        var agentAuthoritiesForWoodlandOwnerId = _context.AgentAuthorities.Where(x => x.WoodlandOwner.Id == woodlandOwnerId && x.Status == AgentAuthorityStatus.FormUploaded);

        // Get external user accounts where either directly related to woodland owner or via agent / agent authority

        var externalUserAccountsByWoodlandOwnerId = _context.UserAccounts.Where(
            x => x.WoodlandOwnerId == woodlandOwnerId 
            && x.Status == UserAccountStatus.Active);

        var externalUserAccountsByWoodlandOwnerAgencyId = _context.UserAccounts.Where(
            x => agentAuthoritiesForWoodlandOwnerId.Any(y => y.Agency.Id == x.AgencyId) 
            && x.Status == UserAccountStatus.Active);

        // Execute the query

        IEnumerable<UserAccount> userAccounts = await externalUserAccountsByWoodlandOwnerId.Union(externalUserAccountsByWoodlandOwnerAgencyId).ToListAsync(cancellationToken);

        // Ensure we return a distinct set of accounts

        userAccounts = userAccounts.Distinct(new UserAccountEqualityComparer());

        return userAccounts;
    }

    ///<inheritdoc />
    public async Task<Result<WoodlandOwner, UserDbErrorReason>> AddWoodlandOwnerAsync(
        WoodlandOwner entity, 
        CancellationToken cancellationToken)
    {
        var result = (await _context.WoodlandOwners.AddAsync(entity, cancellationToken)).Entity;
        return await UnitOfWork.SaveEntitiesAsync(cancellationToken).Map(() => result);
    }

    ///<inheritdoc />
    public async Task<IList<WoodlandOwner>> GetWoodlandOwnersForActiveAccountsAsync(CancellationToken cancellationToken)
    {
        return await _context.UserAccounts
            .Include(x => x.WoodlandOwner)
            .Where(x => x.Status != UserAccountStatus.Deactivated && x.WoodlandOwner != null)
            .Select(x => x.WoodlandOwner!)
            .Distinct()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<IList<WoodlandOwner>> GetWoodlandOwnersWithIdNotIn(List<Guid> ids, CancellationToken cancellationToken)
    {
        return await _context.WoodlandOwners
            .Where(x => !ids.Contains(x.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}