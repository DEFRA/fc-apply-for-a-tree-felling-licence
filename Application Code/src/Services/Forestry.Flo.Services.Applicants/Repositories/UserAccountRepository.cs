using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Repositories;

public class UserAccountRepository(ApplicantsContext context, ILogger<UserAccountRepository> logger) : IUserAccountRepository
{
    private readonly ApplicantsContext _context = context  ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<UserAccountRepository> _logger = logger ?? new NullLogger<UserAccountRepository>();

    ///<inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

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

        return userAccount == null 
            ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound) 
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
        
        return userAccount == null 
            ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound) 
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

        return userAccount == null ? Result.Failure<UserAccount, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<UserAccount, UserDbErrorReason>(userAccount);
    }

    ///<inheritdoc />
    public async Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithIdsInAsync(IList<Guid> userIds, CancellationToken cancellationToken)
    {
        var distinctUserIds = userIds.Distinct();

        try
        {
            var users = await _context.UserAccounts
                .Where(x => distinctUserIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

            return Result.Success<IList<UserAccount>, UserDbErrorReason>(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in GetUsersWithIdsInAsync");
            return Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.NotFound);
        }
    }

    ///<inheritdoc />
    public async Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithWoodlandOwnerIdAsync(Guid woodlandOwnerId, CancellationToken cancellationToken)
    {
        var users = await _context.UserAccounts
            .Where(x => x.WoodlandOwnerId == woodlandOwnerId && x.Status == UserAccountStatus.Active)
            .ToListAsync(cancellationToken);

        return users.Count == 0 
            ? Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<IList<UserAccount>, UserDbErrorReason>(users);
    }

    ///<inheritdoc />
    public async Task<Result<IList<UserAccount>, UserDbErrorReason>> GetUsersWithAgencyIdAsync(Guid agencyId, CancellationToken cancellationToken)
    {
        var users = await _context.UserAccounts
            .Where(x => x.AgencyId == agencyId && x.Status == UserAccountStatus.Active)
            .ToListAsync(cancellationToken);

        return users.Count == 0 
            ? Result.Failure<IList<UserAccount>, UserDbErrorReason>(UserDbErrorReason.NotFound) 
            : Result.Success<IList<UserAccount>, UserDbErrorReason>(users);
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