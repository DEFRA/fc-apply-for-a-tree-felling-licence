using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Implementation of <see cref="IAmendUserAccounts"/>.
/// </summary>
public class AmendUserAccountsService : IAmendUserAccounts
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly ILogger<AmendUserAccountsService> _logger;
    private readonly IClock _clock;

    /// <summary>
    /// Creates a new instance of the <see cref="AmendUserAccountsService"/>.
    /// </summary>
    /// <param name="userAccountRepository">A repository to interact with the database.</param>
    /// <param name="logger">A logging instance.</param>
    /// <param name="clock">A clock to get the current date and time.</param>
    public AmendUserAccountsService(
        IUserAccountRepository userAccountRepository,
        ILogger<AmendUserAccountsService> logger,
        IClock clock)
    {
        _userAccountRepository = Guard.Against.Null(userAccountRepository);
        _logger = logger;
        _clock = Guard.Against.Null(clock);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> UpdateUserAccountDetailsAsync(UpdateUserAccountModel userModel, CancellationToken cancellationToken)
    {
        var (_, isFailure, userAccount, error) = await _userAccountRepository.GetAsync(userModel.UserAccountId, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Unable to retrieve user account with id {id}, error: {error}", userModel.UserAccountId, error);
            return Result.Failure<bool>($"Unable to retrieve user account, error: {error}");
        }

        userAccount.FirstName = userModel.FirstName;
        userAccount.LastName = userModel.LastName;
        userAccount.Title = userModel.Title;
        userAccount.PreferredContactMethod = userModel.PreferredContactMethod;

        userAccount.ContactAddress = userModel.ContactAddress;
        userAccount.ContactMobileTelephone = userModel.ContactMobileNumber;
        userAccount.ContactTelephone = userModel.ContactTelephoneNumber;

        userAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();

        try
        {
            var result = await _userAccountRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return result > 0;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("Unable to update agent user account with id {id}, exception: {Exception}", userModel.UserAccountId, ex.ToString());
            return Result.Failure<bool>("Unable to update agent user account");
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserAccountModel>> UpdateApplicantAccountStatusAsync(
        Guid userId,
        UserAccountStatus requestedStatus,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, user, error) = await _userAccountRepository.GetAsync(userId, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Unable to retrieve user account with id {id}, error: {error}", userId, error);
            return Result.Failure<UserAccountModel>($"Unable to retrieve user account, error: {error}");
        }

        user.Status = requestedStatus;

        var result = await _userAccountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Unable to save changes when setting status to {status} for user with id {id}, error: {error}", requestedStatus, userId, result.Error);
            return Result.Failure<UserAccountModel>($"Unable to save changes when setting user account status, error: {result.Error}");
        }

        return new UserAccountModel
        {
            AccountType = user.AccountType,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            UserAccountId = user.Id,
            Status = UserAccountStatus.Active
        };
    }
}