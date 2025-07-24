using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.EntityFrameworkCore;

namespace Forestry.Flo.Services.InternalUsers.Repositories;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly InternalUsersContext _context;

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    public UserAccountRepository(InternalUsersContext context) => _context = context  ?? throw new ArgumentNullException(nameof(context));

    ///<inheritdoc />
    public async Task<UserAccount> AddAsync(UserAccount userAccount, CancellationToken cancellationToken)
    {
        var result = await _context.UserAccounts.AddAsync(userAccount, cancellationToken);
        return result.Entity;
    } 

    ///<inheritdoc />
    public void Update(UserAccount userAccount) => _context.Entry(userAccount).State = EntityState.Modified;

    ///<inheritdoc />
    public async Task<Result<UserAccount, UserDbErrorReason>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var userAccount = await _context
            .UserAccounts
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: cancellationToken);

        return userAccount == null ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<UserAccount, UserDbErrorReason>(userAccount);
    }

    ///<inheritdoc />
    public async Task<Result<UserAccount, UserDbErrorReason>> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var userAccount = await _context.UserAccounts
            .SingleOrDefaultAsync(a => a.Email.ToLower() == email.ToLower(),
                cancellationToken: cancellationToken);

        return userAccount == null ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<UserAccount, UserDbErrorReason>(userAccount);
    }

    /// <inheritdoc />
    public async Task<Result<UserAccount, UserDbErrorReason>> GetByIdentityProviderIdAsync(string identityProviderId,
        CancellationToken cancellationToken)
    {
        var userAccount = await _context.UserAccounts.FirstOrDefaultAsync(x => x.IdentityProviderId == identityProviderId, cancellationToken);

        return userAccount is null 
            ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound)
            : Result.Success<UserAccount, UserDbErrorReason>(userAccount);
    }

    ///<inheritdoc />
    public async Task<IList<UserAccount>> GetByFullnameAsync(string firstName, string lastName, CancellationToken cancellationToken)
    {
        return await _context.UserAccounts
            .Where(a => a.FirstName.ToLower() == firstName.ToLower()
                   && a.LastName.ToLower() == lastName.ToLower())
            .ToListAsync(cancellationToken: cancellationToken);
    }

    ///<inheritdoc />
    public async Task<IList<UserAccount>> GetByFullnameAsync(string title, string firstName, string lastName, CancellationToken cancellationToken)
    {
        return await _context.UserAccounts
            .Where(a => a.FirstName.ToLower() == firstName.ToLower()
                        && a.LastName.ToLower() == lastName.ToLower()
                        && a.Title.ToLower() == title.ToLower())
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithIdsInAsync(IList<Guid> userIds, CancellationToken cancellationToken)
    {
        var users = await _context.UserAccounts.Where(x => userIds.Contains(x.Id)).ToListAsync(cancellationToken);

        // Perform distinct operation following DB query else throws exception as untranslatable

        users = users.Distinct(new UserAccountEqualityComparer()).ToList();

        return Result.Success<IList<UserAccount>, UserDbErrorReason>(users);
    }

    ///<inheritdoc />
    public async Task<IEnumerable<UserAccount>> GetConfirmedUserAccountsAsync(CancellationToken cancellationToken, List<Guid>? excludeIds = null)
    {
        return excludeIds is null
            ? await _context.UserAccounts.Where(x => x.Status == Status.Confirmed).ToListAsync(cancellationToken)
            : await _context.UserAccounts.Where(x => !excludeIds.Contains(x.Id) && x.Status == Status.Confirmed).ToListAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<IEnumerable<UserAccount>> GetUnconfirmedUserAccountsAsync(CancellationToken cancellationToken)
    {
        return await _context.UserAccounts.Where(x => x.Status == Status.Requested && x.AccountType != AccountTypeInternal.FcStaffMember).ToListAsync(cancellationToken);
    }

    ///<inheritdoc />
    public async Task<IEnumerable<UserAccount>> GetConfirmedUserAccountsByAccountTypeAsync(
        AccountTypeInternal accountType,
        AccountTypeInternalOther? accountTypeOther, 
        CancellationToken cancellationToken)
    {
        if (accountTypeOther.HasValue)
        {
            return await _context.UserAccounts.Where(x =>
                x.AccountType == accountType && x.AccountTypeOther == accountTypeOther && x.Status == Status.Confirmed)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return await _context.UserAccounts.Where(x =>
            x.AccountType == accountType && x.Status == Status.Confirmed)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}