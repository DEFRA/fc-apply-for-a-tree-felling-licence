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
    public async Task<Result<bool>> UpdateUserAccountDetailsAsync(UpdateUserAccountModel model, CancellationToken cancellationToken)
    {
        var (_, isFailure, userAccount, error) = await _userAccountRepository.GetAsync(model.UserAccountId, cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Unable to retrieve user account with id {id}, error: {error}", model.UserAccountId, error);
            return Result.Failure<bool>($"Unable to retrieve user account, error: {error}");
        }

        userAccount.FirstName = model.FirstName;
        userAccount.LastName = model.LastName;
        userAccount.Title = model.Title;
        userAccount.PreferredContactMethod = model.PreferredContactMethod;

        userAccount.ContactAddress = model.ContactAddress;
        userAccount.ContactMobileTelephone = model.ContactMobileNumber;
        userAccount.ContactTelephone = model.ContactTelephoneNumber;

        userAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();

        try
        {
            var result = await _userAccountRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return result > 0;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("Unable to update agent user account with id {id}, exception: {Exception}", model.UserAccountId, ex.ToString());
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