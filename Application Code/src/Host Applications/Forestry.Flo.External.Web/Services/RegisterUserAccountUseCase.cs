using System.Security.Claims;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Exceptions;
using Forestry.Flo.External.Web.Models.Agency;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.Services.Applicants;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Migrations;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NodaTime;
using Agency = Forestry.Flo.Services.Applicants.Entities.Agent.Agency;
using TenantType = Forestry.Flo.Services.Applicants.Entities.WoodlandOwner.TenantType;
using UserAccountEntity = Forestry.Flo.Services.Applicants.Entities.UserAccount.UserAccount;
using WoodlandOwnerEntity = Forestry.Flo.Services.Applicants.Entities.WoodlandOwner.WoodlandOwner;

namespace Forestry.Flo.External.Web.Services;

public class RegisterUserAccountUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly RequestContext _requestContext;
    private readonly IAuditService<RegisterUserAccountUseCase> _auditService;
    private readonly IClock _clock;
    private readonly ISignInApplicant _signInApplicant;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDbContextFactory<ApplicantsContext> _dbContextFactory;
    private readonly ILogger<RegisterUserAccountUseCase> _logger;
    private readonly IPropertyProfileRepository _propertyProfileRepository;
    private readonly IFellingLicenceApplicationExternalRepository _fellingLicenceApplicationRepository;
    private readonly FcAgencyOptions _fcAgencyOptions;


    public RegisterUserAccountUseCase(
        IDbContextFactory<ApplicantsContext> dbContextFactory,
        IHttpContextAccessor httpContextAccessor,
        ISignInApplicant signInApplicant,
        IClock clock,
        IAuditService<RegisterUserAccountUseCase> auditService,
        ILogger<RegisterUserAccountUseCase> logger,
        IPropertyProfileRepository propertyProfileRepository,
        IFellingLicenceApplicationExternalRepository fellingLicenceApplicationRepository,
        IUserAccountRepository userAccountRepository,
        IOptions<FcAgencyOptions> fcAgencyOptions,
        RequestContext requestContext,
        IAgentAuthorityService agentAuthorityService)
    {
        _userAccountRepository = userAccountRepository;
        _requestContext = requestContext;
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
        _auditService = Guard.Against.Null(auditService);
        _clock = Guard.Against.Null(clock);
        _signInApplicant = Guard.Against.Null(signInApplicant);
        _httpContextAccessor = Guard.Against.Null(httpContextAccessor);
        _dbContextFactory = Guard.Against.Null(dbContextFactory);
        _logger = logger ?? new NullLogger<RegisterUserAccountUseCase>();
        _propertyProfileRepository = Guard.Against.Null(propertyProfileRepository);
        _fellingLicenceApplicationRepository = Guard.Against.Null(fellingLicenceApplicationRepository);
        _fcAgencyOptions = Guard.Against.Null(fcAgencyOptions.Value);
    }

    /// <summary>
    /// Attempts to retrieve an existing local user account for the current user.
    /// </summary>
    /// <param name="user">The current user of the system.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    public async Task<Maybe<UserAccount>> RetrieveExistingAccountAsync(
        ExternalApplicant user, 
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        if (user.HasRegisteredLocalAccount)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var localAccount = await dbContext.UserAccounts
                .Include(x => x.WoodlandOwner)
                .Include(x => x.Agency)
                .SingleOrDefaultAsync(x => x.Id == user.UserAccountId!, cancellationToken);

            if (localAccount is null)
                throw new NotExistingUserAccountException(
                    $"User account for a user with id: {user.UserAccountId} not found");
            
            return Maybe<UserAccount>.From(localAccount);
        }

        return Maybe<UserAccount>.None;
    }

    public async Task<AccountSignupValidityCheckOutcome> AccountSignupValidityCheck(
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Attempting to check that a new account is not using the same email address as an existing invited account");
        var getAccountResult = await _userAccountRepository.GetByEmailAsync(user.EmailAddress!, cancellationToken)
            .ConfigureAwait(false);

        if (getAccountResult.IsFailure)
        {
            //New account
            return AccountSignupValidityCheckOutcome.IsValidSignUp;
        }

        switch (getAccountResult.Value.Status)
        {
            case UserAccountStatus.Invited:
                _logger.LogWarning(
                    "An invited user account already exists for the email address a user is attempting to sign up with - userId is {userId}",
                    user.UserAccountId);
                return AccountSignupValidityCheckOutcome.IsAlreadyInvited;
            case UserAccountStatus.Migrated:
                _logger.LogWarning(
                    "A migrated user account already exists for the email address a user is attempting to sign up with - userId is {userId}",
                    user.UserAccountId);
                return AccountSignupValidityCheckOutcome.IsMigratedUser;
            case UserAccountStatus.Deactivated:
                _logger.LogWarning(
                    "A deactivated user account already exists for the email address a user is attempting to sign up with - userId is {userId}",
                    user.UserAccountId);
                return AccountSignupValidityCheckOutcome.IsDeactivated;
            default:
                throw new ArgumentOutOfRangeException(nameof(UserAccountStatus),"Unhandled enum value.");
        }
    }

    /// <summary>
    /// Attempts to update an existing accounts account type 
    /// </summary>
    /// <param name="user">An <see cref="ExternalApplicant"/> instance representing the user's registration in the external identity provider system.</param>
    /// <param name="accountType">The <see cref="Models.UserAccount.AccountType"/> to update the user account to.</param>
    /// <param name="landlordDetails">The details of a Crown Land tenant's landlord.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<Result> UpdateAccountTypeAsync(
        ExternalApplicant user, 
        UserTypeModel accountType,
        LandlordDetails? landlordDetails = null,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        var userAccount = await RetrieveExistingAccountAsync(user, cancellationToken);

        if (userAccount.HasNoValue)
        {
            _logger.LogError("Could not locate account with id {AccountId} to update account type", user.UserAccountId);
            return Result.Failure($"Could not locate account with id {user.UserAccountId} to update account type");
        }

        if (IsAccountTypeEqual(userAccount.Value, accountType, landlordDetails))
        {
            await RefreshSigninAsync(user, userAccount.Value);
            return Result.Success();
        }

        var removalResult = userAccount.Value.AccountType switch
        {
            AccountTypeExternal.WoodlandOwner or AccountTypeExternal.WoodlandOwnerAdministrator =>
                await RemoveWoodlandOwnerAsync(user, userAccount.Value, cancellationToken),
            AccountTypeExternal.Agent or AccountTypeExternal.AgentAdministrator =>
                await RemoveAgencyAsync(user, userAccount.Value, cancellationToken),
            _ => Result.Success()
        };

        if (removalResult.IsFailure)
        {
            var entityName = userAccount.Value.IsAgent()
                ? "agency"
                : "woodland owner";

            _logger.LogError("Unable to remove {entityName} for user {userId}", entityName, user.UserAccountId);
            return Result.Failure("Unable to remove woodland owner or agency for user");
        }

        var woodlandOwnerType = accountType.AccountType switch
        {
            AccountType.Tenant => WoodlandOwnerType.Tenant,
            AccountType.Trust => WoodlandOwnerType.Trust,
            _ => WoodlandOwnerType.WoodlandOwner
        };

        userAccount = accountType.AccountType is AccountType.Agent
            ? InitializeAgentAccount(user, accountType.IsOrganisation, userAccount.Value)
            : InitializeWoodlandOwnerAccount(
                user,
                accountType.IsOrganisation,
                woodlandOwnerType,
                landlordDetails,
                userAccount.Value);
        
        userAccount.Value.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.UserAccounts.Update(userAccount.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        await RefreshSigninAsync(user, userAccount.Value);

        return Result.Success();
    }

    /// <summary>
    /// Attempts to register a new local account for the given external applicant of the given type.
    /// </summary>
    /// <param name="user">An <see cref="ExternalApplicant"/> instance representing the user's registration in the external identity provider system.</param>
    /// <param name="accountType">The <see cref="Models.UserAccount.AccountType"/> for the new user account.</param>
    /// <param name="landlordDetails">The details of a Crown Land tenant's landlord.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result> RegisterNewAccountAsync(
        ExternalApplicant user,
        UserTypeModel accountType,
        LandlordDetails? landlordDetails = null,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        if (user.HasRegisteredLocalAccount)
        {
            _logger.LogWarning("Attempt to register a new account for a user that already has one was detected");
            return Result.Failure("Given user already has a local account.");
        }

        var woodlandOwnerType = accountType.AccountType switch
        {
            AccountType.Tenant => WoodlandOwnerType.Tenant,
            AccountType.Trust => WoodlandOwnerType.Trust,
            _ => WoodlandOwnerType.WoodlandOwner
        };

        var userAccount = accountType.AccountType is AccountType.Agent
            ? InitializeAgentAccount(user, accountType.IsOrganisation)
            : InitializeWoodlandOwnerAccount(
                user, 
                accountType.IsOrganisation, 
                woodlandOwnerType,
                landlordDetails);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            // check for existing account with same identity provider id - this shouldn't be possible under normal use of the app
            var accountWithIdentityProviderId = await dbContext.UserAccounts
                .Include(x => x.WoodlandOwner)
                .Include(x => x.Agency)
                .SingleOrDefaultAsync(x => userAccount.IdentityProviderId != null
                                           && x.IdentityProviderId != null 
                                           && x.IdentityProviderId == userAccount.IdentityProviderId, cancellationToken: cancellationToken);
            if (accountWithIdentityProviderId != null)
            {
                _logger.LogWarning("Attempt to register a new account for a user with an identity provider id that already exists in the database was detected");

                if (user.EmailAddress!.Equals(accountWithIdentityProviderId.Email, StringComparison.OrdinalIgnoreCase))
                {
                    await RefreshSigninAsync(user, accountWithIdentityProviderId);
                    return Result.Success();
                }

                _logger.LogError("A user with the same identity provider id but different email address to a user account in the database was detected");
                return Result.Failure("A user account with the same Identity Provider Id but different email address to the current user was found.");
            }
            userAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
            await dbContext.UserAccounts.AddAsync(userAccount, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await AuditUserAccountEvent(AuditEvents.RegisterAuditEvent, user, userAccount, cancellationToken);

            await RefreshSigninAsync(user, userAccount);

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception caught saving new user account");
            var auditData = new
            {
                AccountType = accountType,
                user.IdentityProviderId,
                e.Message
            };
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.RegisterFailureEvent, 
                    null, 
                    user.UserAccountId,
                    _requestContext,
                    auditData),
                cancellationToken);
            return Result.Failure(e.Message);
        }
    }

    /// <summary>
    /// Attempts to update the name details in the local account for the given user with values from a UI model.
    /// </summary>
    /// <param name="user">An <see cref="ExternalApplicant"/> representing the current user.</param>
    /// <param name="model">The model containing updated values.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result> UpdateAccountPersonNameDetailsAsync(ExternalApplicant user, UserAccountModel model, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        if (user.HasRegisteredLocalAccount == false)
        {
            return Result.Failure("There is no local user account for the current user.");
        }

        UserAccountEntity? existingAccount = null;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            existingAccount = await dbContext.UserAccounts
                .Include(x => x.WoodlandOwner)
                .Include(x => x.Agency)
                .SingleAsync(x => x.Id == user.UserAccountId!, cancellationToken);

            existingAccount.Title = model.PersonName?.Title;
            existingAccount.FirstName = model.PersonName?.FirstName;
            existingAccount.LastName = model.PersonName?.LastName;

            //also update contact name on woodland owner
            //TODO this will need to be revisited if and when we re-enable multiple accounts per woodland owner/agency
            if (model.UserTypeModel.AccountType is AccountType.Tenant or AccountType.Trust or AccountType.WoodlandOwner
                && existingAccount.WoodlandOwner != null)
            {
                existingAccount.WoodlandOwner.ContactName = existingAccount.FullName();
            }
            // or on agency
            if (model.UserTypeModel.AccountType == AccountType.Agent && existingAccount.Agency is not null)
            {
                existingAccount.Agency.ContactName = existingAccount.FullName(false);
            }

            existingAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
            await dbContext.SaveChangesAsync(cancellationToken);

            await AuditUserAccountEvent(AuditEvents.UpdateAccountEvent, user, existingAccount, cancellationToken);

            await RefreshSigninAsync(user, existingAccount);

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception caught updating existing user account for terms and conditions");
            var auditData = new
            {
                AccountType = existingAccount?.AccountType,
                user.IdentityProviderId,
                e.Message
            };
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountFailureEvent, 
                    existingAccount?.Id, 
                    user.UserAccountId,
                    _requestContext,
                    auditData)
                , cancellationToken); 
            return Result.Failure(e.Message);
        }
    }

    /// <summary>
    /// Attempts to update the contact details in the local account for the given user with values from a UI model.
    /// </summary>
    /// <param name="user">An <see cref="ExternalApplicant"/> representing the current user.</param>
    /// <param name="model">The model containing updated values.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result> UpdateAccountPersonContactDetailsAsync(ExternalApplicant user, UserAccountModel model, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        if (user.HasRegisteredLocalAccount == false)
        {
            return Result.Failure("There is no local user account for the current user.");
        }

        UserAccountEntity? existingAccount = null;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            existingAccount = await dbContext.UserAccounts
                .Include(x => x.WoodlandOwner)
                .Include(x => x.Agency)
                .SingleAsync(x => x.Id == user.UserAccountId!, cancellationToken);

            existingAccount.ContactAddress = new Address(
                model.PersonContactsDetails?.ContactAddress?.Line1,
                model.PersonContactsDetails?.ContactAddress?.Line2,
                model.PersonContactsDetails?.ContactAddress?.Line3,
                model.PersonContactsDetails?.ContactAddress?.Line4,
                model.PersonContactsDetails?.ContactAddress?.PostalCode);
            existingAccount.ContactTelephone = model.PersonContactsDetails?.ContactTelephoneNumber;
            existingAccount.ContactMobileTelephone = model.PersonContactsDetails?.ContactMobileNumber;
            existingAccount.PreferredContactMethod = model.PersonContactsDetails?.PreferredContactMethod;

            //also update contact details on woodland owner
            //TODO this will need to be revisited if and when we re-enable multiple accounts per woodland owner/agency
            if (model.UserTypeModel.AccountType is AccountType.WoodlandOwner or AccountType.Tenant or AccountType.Trust 
                && existingAccount.WoodlandOwner != null)
            {
                existingAccount.WoodlandOwner.ContactTelephone = model.PersonContactsDetails?.ContactTelephoneNumber;
                existingAccount.WoodlandOwner.ContactAddress = new Address(
                    model.PersonContactsDetails?.ContactAddress?.Line1,
                    model.PersonContactsDetails?.ContactAddress?.Line2,
                    model.PersonContactsDetails?.ContactAddress?.Line3,
                    model.PersonContactsDetails?.ContactAddress?.Line4,
                    model.PersonContactsDetails?.ContactAddress?.PostalCode);
            }
            //or agency
            if (model.UserTypeModel.AccountType == AccountType.Agent && existingAccount.Agency is not null)
            {
                existingAccount.Agency.Address = new Address(
                    model.PersonContactsDetails?.ContactAddress?.Line1,
                    model.PersonContactsDetails?.ContactAddress?.Line2,
                    model.PersonContactsDetails?.ContactAddress?.Line3,
                    model.PersonContactsDetails?.ContactAddress?.Line4,
                    model.PersonContactsDetails?.ContactAddress?.PostalCode);
            }

            existingAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
            await dbContext.SaveChangesAsync(cancellationToken);

            await AuditUserAccountEvent(AuditEvents.UpdateAccountEvent, user, existingAccount, cancellationToken);

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception caught updating existing user account for contact details");
            var auditData = new
            {
                AccountType = existingAccount?.AccountType,
                user.IdentityProviderId,
                e.Message
            };
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountFailureEvent, 
                    null, 
                    user.UserAccountId,
                    _requestContext,
                    auditData),
                cancellationToken); 
            return Result.Failure(e.Message);
        }
    }
    
    /// <summary>
    /// Updates user agency details for a user account
    /// </summary>
    /// <param name="user">A system user</param>
    /// <param name="model">An agency details model</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result of the updated agency details operation</returns>
    public async Task<Result> UpdateUserAgencyDetails(ExternalApplicant user, AgencyModel model,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);
        if (!user.HasRegisteredLocalAccount || !user.UserAccountId.HasValue)
        {
            return Result.Failure("There is no local user account for the current user.");
        }

        if (string.IsNullOrWhiteSpace(user.AgencyId))
        {
            return Result.Failure("User account is not linked to Agency Details");
        }

        if (user.AccountType != AccountTypeExternal.AgentAdministrator)
        {
            return Result.Failure("User account does not have permissions to edit agency details");
        }

        return await _userAccountRepository.GetAsync(user.UserAccountId.Value, cancellationToken)
            .Check(async userAccount =>
            {
                userAccount.Agency!.Address =
                    model.Address != null ? ModelMapping.ToAddressEntity(model.Address) : null;
                userAccount.Agency.ContactEmail = model.ContactEmail;
                userAccount.Agency.ContactName = model.ContactName;
                userAccount.Agency.OrganisationName = model.OrganisationName;
                userAccount.Agency.IsOrganisation = string.IsNullOrWhiteSpace(model.OrganisationName) is false;
                userAccount.Agency.ShouldAutoApproveThinningApplications =
                    model.ShouldAutoApproveThinningApplications;
                 userAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
                _userAccountRepository.Update(userAccount);
                return await _userAccountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
                    .Map(() => userAccount);
            })
            .MapError(e => "An error received during processing of the user agency details update, please try again")
            .OnFailure(async error => await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountFailureEvent,
                    user.UserAccountId,
                    user.UserAccountId,
                    _requestContext,
                    new { user.AccountType, user.IdentityProviderId, Error = error }),
                cancellationToken)
            )
            .Tap(async _ => await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountEvent,
                    user.UserAccountId,
                    user.UserAccountId,
                    _requestContext,
                    new { user.AccountType, user.IdentityProviderId }),
                cancellationToken));
    }

    /// <summary>
    /// Attempts to revert the agency organisation type to individual for the given user.
    /// </summary>
    /// <param name="user"> An <see cref="ExternalApplicant"/> representing the current user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> RevertAgencyOrganisationToIndividualAsync(ExternalApplicant user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!user.HasRegisteredLocalAccount || !user.UserAccountId.HasValue)
        {
            return Result.Failure("There is no local user account for the current user.");
        }

        if (string.IsNullOrWhiteSpace(user.AgencyId))
        {
            return Result.Failure("User account is not linked to Agency Details");
        }

        if (user.AccountType is not AccountTypeExternal.AgentAdministrator)
        {
            return Result.Failure("User account does not have permissions to edit agency details");
        }

        return await _userAccountRepository.GetAsync(user.UserAccountId.Value, cancellationToken)
            .Check(async userAccount =>
            {
                userAccount.Agency!.IsOrganisation = false;
                userAccount.Agency.OrganisationName = null;
                userAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
                _userAccountRepository.Update(userAccount);
                return await _userAccountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
            })
            .MapError(e => "An error received during processing of the user agency details update, please try again")
            .OnFailure(async error => await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountFailureEvent,
                    user.UserAccountId,
                    user.UserAccountId,
                    _requestContext,
                    new { user.AccountType, user.IdentityProviderId, Error = error }),
                CancellationToken.None)
            )
            .Tap(async _ => await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountEvent,
                    user.UserAccountId,
                    user.UserAccountId,
                    _requestContext,
                    new { user.AccountType, user.IdentityProviderId }),
                CancellationToken.None));
    }
    
    /// <summary>
    /// Attempts to update the woodland owner details linked to the local account for the given user with values from a UI model.
    /// </summary>
    /// <param name="user">An <see cref="ExternalApplicant"/> representing the current user.</param>
    /// <param name="model">The model containing updated values.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result> UpdateAccountWoodlandOwnerDetailsAsync(ExternalApplicant user, WoodlandOwnerModel model, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        if (user.HasRegisteredLocalAccount == false)
        {
            return Result.Failure("There is no local user account for the current user.");
        }

        if (string.IsNullOrWhiteSpace(user.WoodlandOwnerId))
        {
            return Result.Failure("User account is not linked to Woodland Owner Details");
        }

        UserAccountEntity? existingAccount = null;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            existingAccount = await dbContext.UserAccounts
                .Include(x => x.WoodlandOwner)
                .Include(x => x.Agency)
                .SingleAsync(x => x.Id == user.UserAccountId!, cancellationToken);

            existingAccount.WoodlandOwner!.ContactName = model.ContactName;
            existingAccount.WoodlandOwner!.ContactEmail = model.ContactEmail;
            existingAccount.WoodlandOwner!.ContactTelephone = model.ContactTelephoneNumber;
            existingAccount.WoodlandOwner!.ContactAddress = model.ContactAddress != null
                ? new Address(
                    model.ContactAddress.Line1,
                    model.ContactAddress.Line2,
                    model.ContactAddress.Line3,
                    model.ContactAddress.Line4,
                    model.ContactAddress.PostalCode)
                : null;
            existingAccount.WoodlandOwner!.OrganisationName = model.OrganisationName;
            existingAccount.WoodlandOwner!.OrganisationAddress = model.OrganisationAddress == null
                ? null
                : new Address(
                    model.OrganisationAddress.Line1,
                    model.OrganisationAddress.Line2,
                    model.OrganisationAddress.Line3,
                    model.OrganisationAddress.Line4,
                    model.OrganisationAddress.PostalCode);
            existingAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
            await dbContext.SaveChangesAsync(cancellationToken);

            await AuditWoodlandOwnerEvent(existingAccount, user, cancellationToken);

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception caught updating existing user account for terms and conditions");
            var auditData = new
            {
                UserAccountId = existingAccount?.Id,
                existingAccount?.WoodlandOwner?.IsOrganisation,
                e.Message
            };
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateWoodlandOwnerFailureEvent, 
                    null, 
                    user.UserAccountId,
                    _requestContext,
                    auditData
                ),
                cancellationToken); 
            return Result.Failure(e.Message);
        }
    }

    /// <summary>
    /// Attempts to update the accepted t&cs date on the local account for the given user with values from a UI model.
    /// </summary>
    /// <param name="user">An <see cref="ExternalApplicant"/> representing the current user.</param>
    /// <param name="model">A populated <see cref="AccountTermsAndConditionsModel"/> indicating if the user accepts the T&Cs/Privacy Policy.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result> UpdateUserAcceptsTermsAndConditionsAsync(ExternalApplicant user, AccountTermsAndConditionsModel model, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        if (user.HasRegisteredLocalAccount == false)
        {
            return Result.Failure("There is no local user account for the current user.");
        }

        UserAccountEntity? existingAccount = null;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            existingAccount = await dbContext.UserAccounts
                .Include(x => x.WoodlandOwner)
                .Include(x => x.Agency)
                .SingleAsync(x => x.Id == user.UserAccountId!, cancellationToken);

            var currentTime = _clock.GetCurrentInstant().ToDateTimeUtc();

            existingAccount.DateAcceptedTermsAndConditions = model.AcceptsTermsAndConditions
                ? currentTime
                : null;
            existingAccount.DateAcceptedPrivacyPolicy = model.AcceptsPrivacyPolicy
                ? currentTime
                : null;
            existingAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
            await dbContext.SaveChangesAsync(cancellationToken);

            await AuditUserAccountEvent(AuditEvents.UpdateAccountEvent, user, existingAccount, cancellationToken);

            await RefreshSigninAsync(user, existingAccount);

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception caught updating existing user account for terms and conditions");
            var auditData = new
            {
                AccountType = existingAccount?.AccountType,
                user.IdentityProviderId,
                e.Message
            };
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountFailureEvent, 
                    null, 
                    user.UserAccountId,
                    _requestContext,
                    auditData),
                cancellationToken);
            return Result.Failure(e.Message);
        }
    }

    private static UserAccountEntity InitializeWoodlandOwnerAccount(
        ExternalApplicant user, 
        bool isOrganisation, 
        WoodlandOwnerType woodlandOwnerType, 
        LandlordDetails? landlordDetails = null,
        UserAccountEntity? existingAccount = null)
    {
        var woodlandOwner = new WoodlandOwnerEntity
        {
            IsOrganisation = isOrganisation,
            ContactName = existingAccount?.FullName(false),
            ContactEmail = user.EmailAddress,
            WoodlandOwnerType = woodlandOwnerType,
            LandlordFirstName = landlordDetails?.FirstName,
            LandlordLastName = landlordDetails?.LastName,
            // landlord details are only set for Crown Land tenants
            TenantType = woodlandOwnerType is WoodlandOwnerType.Tenant 
                ? landlordDetails is not null 
                    ? TenantType.CrownLand 
                    : TenantType.NonCrownLand 
                : TenantType.None, 
            ContactTelephone = existingAccount?.ContactTelephone,
            ContactAddress = existingAccount?.ContactAddress != null
                ? new Address(
                    existingAccount.ContactAddress.Line1,
                    existingAccount.ContactAddress.Line2,
                    existingAccount.ContactAddress.Line3,
                    existingAccount.ContactAddress.Line4,
                    existingAccount.ContactAddress.PostalCode)
                : null
        };

        var userAccount = existingAccount ?? new UserAccountEntity();
        userAccount.IdentityProviderId = user.IdentityProviderId!;
        userAccount.Email = user.EmailAddress!;
        userAccount.WoodlandOwner = woodlandOwner;
        userAccount.AccountType = AccountTypeExternal.WoodlandOwnerAdministrator;

        return userAccount;
    }

    private UserAccountEntity InitializeAgentAccount(
        ExternalApplicant user, 
        bool isOrganisation,
        UserAccountEntity? existingAccount = null)
    {
        var userAccount = existingAccount ?? new UserAccountEntity();
        userAccount.IdentityProviderId = user.IdentityProviderId!;
        userAccount.Email = user.EmailAddress!;
        userAccount.AccountType = AccountTypeExternal.AgentAdministrator;
        
        var agency = new Agency
        {
            ContactEmail = user.EmailAddress,
            ContactName = existingAccount?.FullName(false),
            IsOrganisation = isOrganisation,
            Address = existingAccount?.ContactAddress != null
                ? new Address(
                    existingAccount.ContactAddress.Line1,
                    existingAccount.ContactAddress.Line2,
                    existingAccount.ContactAddress.Line3,
                    existingAccount.ContactAddress.Line4,
                    existingAccount.ContactAddress.PostalCode)
                : null
        };

        userAccount.Agency = agency;

        if (isOrganisation is false)
        {
            // initialise fields on Agency if the agent is not an organisation
            agency.OrganisationName = existingAccount?.FullName(false);
        }
        
        return userAccount;
    }

    private async Task<Result> RemoveWoodlandOwnerAsync(ExternalApplicant user, UserAccountEntity userAccount, CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(userAccount);

        if (user.HasCompletedAccountRegistration)
        {
            _logger.LogWarning("Attempt to remove woodland owner from an account that has completed signup has been detected");
            return Result.Failure("Account has already been completed.");
        }

        var propertyProfiles = await _propertyProfileRepository.ListAsync(userAccount.WoodlandOwnerId!.Value, cancellationToken);

        if (propertyProfiles.Value is not null && propertyProfiles.Value.Any())
        {
            _logger.LogWarning("Attempt to remove woodland owner from an account that has property profiles already associated with it");
            return Result.Failure("Account already has property profiles.");
        }

        var fellingLicenceApplications = await _fellingLicenceApplicationRepository.ListAsync(userAccount.WoodlandOwnerId.Value, cancellationToken);

        if (fellingLicenceApplications.Any())
        {
            _logger.LogWarning("Attempt to remove woodland owner from an account that has felling licence applications already associated with it");
            return Result.Failure("Account already has felling licence applications.");
        }

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            dbContext.WoodlandOwners.Remove(userAccount.WoodlandOwner!);

            userAccount.WoodlandOwner = null;
            userAccount.WoodlandOwnerId = null;
            userAccount.LastChanged = _clock.GetCurrentInstant().ToDateTimeUtc();
            dbContext.UserAccounts.Update(userAccount);
        
            await dbContext.SaveChangesAsync(cancellationToken);

            await RefreshSigninAsync(user, userAccount);

            return Result.Success();

        }
        catch (Exception e)
        {

            _logger.LogError(e, "Exception caught removing woodland owner");
            var auditData = new
            {
                user.IdentityProviderId,
                e.Message
            };
            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountFailureEvent,
                    null,
                    user.UserAccountId,
                    _requestContext,
                    auditData),
                cancellationToken);
            return Result.Failure(e.Message);
        }
    }

    private async Task<Result> RemoveAgencyAsync(ExternalApplicant user, UserAccountEntity userAccount, CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(userAccount);

        if (user.AgencyId is null)
        {
            return Result.Success("User has no associated agency to remove");
        }

        if (user.HasCompletedAccountRegistration)
        {
            _logger.LogWarning("Attempt to remove agency from an account that has completed signup has been detected");
            return Result.Failure("Account has already been completed");
        }

        if (user.IsAnInvitedUser)
        {
            _logger.LogWarning("An invited agent cannot remove an agency");
            return Result.Failure("An invited agent cannot remove an agency");
        }

        if (Guid.TryParse(user.AgencyId, out var agencyId))
        {
            var agencyAuthorities = await _agentAuthorityService
                .GetAgentAuthoritiesAsync(user.UserAccountId!.Value, agencyId, null, cancellationToken)
                .ConfigureAwait(false);
            
            if (agencyAuthorities.IsFailure)
            {
                return Result.Failure("Unable to retrieve agent authorities");
            }

            // only attempt to remove the agency if there are no agent authorities and the agent is not an invited user

            if (agencyAuthorities.Value.AgentAuthorities.Any())
            {
                _logger.LogWarning("Unable to remove agency that has agent authorities");
                return Result.Failure("Unable to remove agency that has agent authorities");
            }
        }

        var removalResult = await _agentAuthorityService.RemoveNewAgencyAsync(userAccount, cancellationToken);

        if (removalResult.IsFailure)
        {
            _logger.LogWarning("Unable to remove agency associated with user account, error {error}", removalResult.Error);

            var auditData = new
            {
                user.IdentityProviderId,
                removalResult.Error
            };

            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountFailureEvent,
                    null,
                    user.UserAccountId,
                    _requestContext,
                    auditData),
                cancellationToken);

            return Result.Failure("Unable to remove agency associated with user account");
        }

        await RefreshSigninAsync(user, userAccount);

        return Result.Success();
    }

    private bool IsAccountTypeEqual(UserAccountEntity userAccount, UserTypeModel accountType, LandlordDetails? landlordDetails = null)
    {
        var tenantType = accountType.AccountType is AccountType.Tenant
            ? landlordDetails == null 
                ? TenantType.NonCrownLand
                : TenantType.CrownLand
            : TenantType.None;

        return accountType.AccountType!.Value switch
        {
            AccountType.Agent when accountType.IsOrganisation => 
                userAccount.IsAgent() && userAccount.Agency?.IsOrganisation == accountType.IsOrganisation,
            AccountType.Agent when accountType.IsOrganisation is false =>
                userAccount.IsAgent() && userAccount.Agency?.IsOrganisation is false,
            AccountType.WoodlandOwner when accountType.IsOrganisation => 
                userAccount.IsWoodlandOwner() && userAccount.WoodlandOwner is { WoodlandOwnerType: WoodlandOwnerType.WoodlandOwner, IsOrganisation: true },
            AccountType.WoodlandOwner when accountType.IsOrganisation is false =>
                userAccount.IsWoodlandOwner() && userAccount.WoodlandOwner is { WoodlandOwnerType: WoodlandOwnerType.WoodlandOwner, IsOrganisation: false },
            AccountType.Trust when accountType.IsOrganisation =>
                userAccount.IsWoodlandOwner() && userAccount.WoodlandOwner is { WoodlandOwnerType: WoodlandOwnerType.Trust, IsOrganisation: true },
            AccountType.Trust when accountType.IsOrganisation is false =>
                userAccount.IsWoodlandOwner() && userAccount.WoodlandOwner is { WoodlandOwnerType: WoodlandOwnerType.Trust, IsOrganisation: false },
            AccountType.Tenant when accountType.IsOrganisation && tenantType is TenantType.CrownLand =>
                userAccount.IsWoodlandOwner() && userAccount.WoodlandOwner is { WoodlandOwnerType: WoodlandOwnerType.Tenant, IsOrganisation: true, TenantType: TenantType.CrownLand },
            AccountType.Tenant when accountType.IsOrganisation is false && tenantType is TenantType.CrownLand =>
                userAccount.IsWoodlandOwner() && userAccount.WoodlandOwner is { WoodlandOwnerType: WoodlandOwnerType.Tenant, IsOrganisation: true, TenantType: TenantType.CrownLand },
            AccountType.Tenant when accountType.IsOrganisation && tenantType is TenantType.NonCrownLand =>
                userAccount.IsWoodlandOwner() && userAccount.WoodlandOwner is { WoodlandOwnerType: WoodlandOwnerType.Tenant, IsOrganisation: true, TenantType: TenantType.NonCrownLand },
            AccountType.Tenant when accountType.IsOrganisation is false && tenantType is TenantType.NonCrownLand =>
                userAccount.IsWoodlandOwner() && userAccount.WoodlandOwner is { WoodlandOwnerType: WoodlandOwnerType.Tenant, IsOrganisation: false, TenantType: TenantType.NonCrownLand },
            _ => false
        };
    }

    private async Task RefreshSigninAsync(ExternalApplicant user, UserAccount account)
    {
        // refresh logged in user claims for new local account
        List<ClaimsIdentity> identities = new()
        {
            ClaimsIdentityHelper.CreateClaimsIdentityFromApplicantUserAccount(account, _fcAgencyOptions.PermittedEmailDomainsForFcAgent)
        };
        identities.AddRange(user.Principal.Identities.Where(x => x.AuthenticationType != FloClaimTypes.ClaimsIdentityAuthenticationType));

        user = new ExternalApplicant(new ClaimsPrincipal(identities));

        if (_httpContextAccessor.HttpContext != null)
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user.Principal);
        }
    }

    private Task AuditUserAccountEvent(string eventName, ExternalApplicant user, UserAccount account, CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
            eventName, 
            account.Id,
            user.UserAccountId,
            _requestContext,
                new
                {
                    user.AccountType,
                    user.IdentityProviderId
                }), 
            cancellationToken);

    private  Task AuditWoodlandOwnerEvent(UserAccountEntity account, ExternalApplicant user, CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.UpdateWoodlandOwnerEvent, 
            account.WoodlandOwner!.Id, 
            user.UserAccountId,
            _requestContext,
            new
            {
                UserAccountId = account.Id,
                account.WoodlandOwner!.IsOrganisation
            }), cancellationToken);
}