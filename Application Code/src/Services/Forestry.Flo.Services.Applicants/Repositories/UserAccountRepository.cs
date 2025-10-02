using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.Applicants.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly ApplicantsContext _context;

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    public UserAccountRepository(ApplicantsContext context) => _context = context  ?? throw new ArgumentNullException(nameof(context));

    ///<inheritdoc />
    public UserAccount Add(UserAccount userAccount) => _context.UserAccounts.Add(userAccount).Entity;

    ///<inheritdoc />
    public void Update(UserAccount userAccount) => _context.Entry(userAccount).State = EntityState.Modified;

    ///<inheritdoc />
    public async Task<Result<UserAccount, UserDbErrorReason>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var userAccount = await _context
            .UserAccounts
            .Include(x => x.WoodlandOwner)
            .Include(x => x.Agency)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);

        return userAccount == null ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<UserAccount, UserDbErrorReason>(userAccount);
    }

    ///<inheritdoc />
    public async Task<Result<UserAccount, UserDbErrorReason>> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var userAccount = await _context.UserAccounts
            .Include(u => u.WoodlandOwner)
            .Include(u => u.Agency)
            .SingleOrDefaultAsync(a => a.Email.ToLower() == email.ToLower(),
                cancellationToken: cancellationToken);
        
        return userAccount == null ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<UserAccount, UserDbErrorReason>(userAccount);
    }

    ///<inheritdoc />
    public async Task<Result<UserAccount, UserDbErrorReason>> GetByUserIdentifierAsync(
        string userIdentifier,
        CancellationToken cancellationToken,
        string? email = null)
    {
        var userAccount = await _context.UserAccounts
            .Include(u => u.WoodlandOwner)
            .Include(u => u.Agency)
            .SingleOrDefaultAsync(a => a.IdentityProviderId == userIdentifier,
                cancellationToken: cancellationToken);

        // todo: this must be removed once the transition to Gov.UK One Login is complete as part of FLOV2-2485

        #region Remove after FLOV2-2485

        if (userAccount is null && email is not null)
        {
            var matchByEmail = await _context.UserAccounts
                .Include(u => u.WoodlandOwner)
                .Include(u => u.Agency)
                .SingleOrDefaultAsync(a =>
                        a.Email.ToLower() == email.ToLower() &&
                        a.IdentityProviderId == null,
                    cancellationToken: cancellationToken);

            if (matchByEmail is not null)
            {
                matchByEmail.IdentityProviderId = userIdentifier;
                await _context.SaveEntitiesAsync(cancellationToken);
                return matchByEmail;
            }
        }

        #endregion

        return userAccount == null ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<UserAccount, UserDbErrorReason>(userAccount);
    }

    ///<inheritdoc />
    public async Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithIdsInAsync(IList<Guid> userIds, CancellationToken cancellationToken)
    {
        var distinctUserIds = userIds.Distinct();

        var users = await _context.UserAccounts
            .Where(x => distinctUserIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        return users == null ? Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<IList<UserAccount>, UserDbErrorReason>(users);
    }

    ///<inheritdoc />
    public async Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithWoodlandOwnerIdAsync(Guid woodlandOwnerId, CancellationToken cancellationToken)
    {
        var users = await _context.UserAccounts
            .Where(x => x.WoodlandOwnerId == woodlandOwnerId && x.Status == UserAccountStatus.Active)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        return Result.Success<IList<UserAccount>, UserDbErrorReason>(users);
    }

    ///<inheritdoc />
    public async Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithAgencyIdAsync(Guid agencyId, CancellationToken cancellationToken)
    {
        var users = await _context.UserAccounts
            .Where(x => x.AgencyId == agencyId && x.Status == UserAccountStatus.Active)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }

        return Result.Success<IList<UserAccount>, UserDbErrorReason>(users);
    }

    ///<inheritdoc />
    public async Task<IList<UserAccount>> GetExternalUsersByAccountTypeAsync(
        IList<AccountTypeExternal> accountTypes,
        CancellationToken cancellationToken)
    {
        return await _context.UserAccounts.Where(x =>
            accountTypes.Contains(x.AccountType)
            && x.Status == UserAccountStatus.Active)
            .Include(x => x.Agency)
            .ToListAsync(cancellationToken);
    }
}