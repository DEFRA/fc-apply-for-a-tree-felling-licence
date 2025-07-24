using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Implementation of <see cref="IAccountRegistrationService"/> that uses an <see cref="IUserAccountRepository"/>
/// to interact with the database.
/// </summary>
public class AccountRegistrationService : IAccountRegistrationService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IClock _clock;
    private readonly ILogger<AccountRegistrationService> _logger;

    public AccountRegistrationService(
        IUserAccountRepository userAccountRepository, 
        ILogger<AccountRegistrationService> logger, 
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(userAccountRepository);
        ArgumentNullException.ThrowIfNull(clock);

        _userAccountRepository = userAccountRepository;
        _clock = clock;

        _logger = logger ?? new NullLogger<AccountRegistrationService>(); ;
    }

    /// <inheritdoc />
    public async Task<Result<AddExternalUserResponse>> CreateUserAccountAsync(
        AddExternalUserRequest request, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);
        _logger.LogDebug("Received request to add new user account in the system.");

        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var userEntity = new UserAccount
        {
            IdentityProviderId = request.IdentityProviderId,
            AccountType = request.AccountType,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            ContactAddress = request.ContactAddress,
            ContactTelephone = request.ContactTelephone,
            ContactMobileTelephone = request.ContactMobileTelephone,
            PreferredContactMethod = request.PreferredContactMethod,
            Status = request.Status,
            DateAcceptedPrivacyPolicy = request.DateAcceptedPrivacyPolicy,
            DateAcceptedTermsAndConditions = request.DateAcceptedTermsAndConditions,
            LastChanged = now,
            AgencyId = request.AgencyId,
            WoodlandOwnerId = request.WoodlandOwnerId,
        };

        _userAccountRepository.Add(userEntity);
        var saveToDbResult = await _userAccountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        if (saveToDbResult.IsSuccess)
        {
            var response = new AddExternalUserResponse
            {
                Email = userEntity.Email,
                AccountType = userEntity.AccountType,
                Id = userEntity.Id,
                IdentityProviderId = userEntity.IdentityProviderId
            };

            return Result.Success(response);
        }
       
        _logger.LogError("Could not save new user account entity to database, error {Error}", saveToDbResult.Error);
        return Result.Failure<AddExternalUserResponse>(saveToDbResult.Error.ToString());
    }
}
