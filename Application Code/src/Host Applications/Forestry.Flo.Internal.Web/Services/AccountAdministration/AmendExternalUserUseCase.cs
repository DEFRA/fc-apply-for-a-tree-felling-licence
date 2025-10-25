using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.AccountAdministration;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Internal.Web.Services.AccountAdministration;

/// <summary>
/// Handles use case for an account administrator to amend external user accounts.
/// </summary>
public class AmendExternalUserUseCase : IAmendExternalUserUseCase
{
    private readonly ILogger<AmendExternalUserUseCase> _logger;
    private readonly IAmendUserAccounts _amendExternalAccounts;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly IAuditService<AmendExternalUserUseCase> _audit;
    private readonly RequestContext _requestContext;

    public AmendExternalUserUseCase(
        ILogger<AmendExternalUserUseCase> logger,
        IAmendUserAccounts amendExternalAccounts,
        IRetrieveUserAccountsService retrieveUserAccountsService,
        IAuditService<AmendExternalUserUseCase> audit,
        RequestContext requestContext)
    {
        _logger = Guard.Against.Null(logger);
        _amendExternalAccounts = Guard.Against.Null(amendExternalAccounts);
        _audit = Guard.Against.Null(audit);
        _requestContext = Guard.Against.Null(requestContext);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
    }

    /// <summary>
    /// Retrieves a populated <see cref="AmendExternalUserAccountModel"/> to update an external user account.
    /// </summary>
    /// <param name="id">The user ID to update.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="AmendExternalUserAccountModel"/>.</returns>
    public async Task<Result<AmendExternalUserAccountModel>> RetrieveExternalUserAccountAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var (isSuccess, _, user, error) = await _retrieveUserAccountsService.RetrieveUserAccountEntityByIdAsync(id, cancellationToken);

        return isSuccess
            ? new AmendExternalUserAccountModel
            {
                PersonContactsDetails = new AccountPersonContactModel
                {
                    ContactAddress = user.ContactAddress is not null 
                        ? ModelMapping.ToAddressModel(user.ContactAddress)
                        : null,
                    ContactMobileNumber = user.ContactMobileTelephone,
                    ContactTelephoneNumber = user.ContactTelephone,
                    PreferredContactMethod = user.PreferredContactMethod
                },
                PersonName = new AccountPersonNameModel
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Title = user.Title
                },
                UserId = id,
                IsFcAgent = user.Agency?.IsFcAgency ?? false
            }
            : Result.Failure<AmendExternalUserAccountModel>(error);
    }

    /// <summary>
    /// Updates a <see cref="UserAccount"/> entity using a populated <see cref="AmendExternalUserAccountModel"/>.
    /// </summary>
    /// <param name="loggedInUser">The logged in internal user performing the update.</param>
    /// <param name="model">A populated <see cref="AmendExternalUserAccountModel"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the <see cref="UserAccount"/> has been successfully updated.</returns>
    public async Task<Result> UpdateExternalAccountDetailsAsync(
        InternalUser loggedInUser,
        AmendExternalUserAccountModel model,
        CancellationToken cancellationToken)
    {
        var updateModel = new UpdateUserAccountModel
        {
            ContactAddress = model.PersonContactsDetails!.ContactAddress is not null
                ? ModelMapping.ToAddressEntity(model.PersonContactsDetails.ContactAddress)
                : null,
            ContactMobileNumber = model.PersonContactsDetails.ContactMobileNumber,
            ContactTelephoneNumber = model.PersonContactsDetails.ContactTelephoneNumber,
            FirstName = model.PersonName!.FirstName,
            LastName = model.PersonName.LastName,
            Title = model.PersonName.Title,
            PreferredContactMethod = model.PersonContactsDetails.PreferredContactMethod,
            UserAccountId = model.UserId
        };

        var (_, isFailure, userUpdated, error) = await _amendExternalAccounts.UpdateUserAccountDetailsAsync(updateModel, cancellationToken);

        if (isFailure)
        {
            var errorMessage = $"Unable to update external user account {model.UserId}, error: {error}";

            _logger.LogError(errorMessage);
            
            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.AdministratorUpdateExternalAccountFailure,
                    model.UserId,
                    loggedInUser.UserAccountId,
                    _requestContext,
                    new
                    {
                        Error = errorMessage,
                        AdministratorAccountType = loggedInUser.AccountType!.GetDisplayName()
                    }), cancellationToken);

            return Result.Failure(errorMessage);
        }

        if (userUpdated)
        {
            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.AdministratorUpdateExternalAccount,
                    model.UserId,
                    loggedInUser.UserAccountId,
                    _requestContext,
                    new
                    {
                        UpdatedFullName = $"{model.PersonName.Title} {model.PersonName.FirstName} {model.PersonName.LastName}".Trim().Replace("  ", " "),
                        AdministratorAccountType = loggedInUser.AccountType!.GetDisplayName()
                    }), cancellationToken);
        }

        return Result.Success();
    }

    /// <summary>
    /// Retrieves a populated <see cref="CloseExternalUserModel"/> to close an external user account.
    /// </summary>
    /// <param name="id">The ID of the user account to close.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="CloseExternalUserModel"/> model.</returns>
    public async Task<Result<CloseExternalUserModel>> RetrieveCloseExternalUserModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var (_, isFailure, userAccount) = await _retrieveUserAccountsService.RetrieveUserAccountEntityByIdAsync(id, cancellationToken);

        if (isFailure)
        {
            return Result.Failure<CloseExternalUserModel>($"Unable to retrieve user account model for user {id}");
        }

        return new CloseExternalUserModel()
        {
            AccountToClose = new ExternalUserModel
            {
                ExternalUser = new ExternalUserAccountModel
                {
                    AccountType = userAccount.AccountType,
                    Email = userAccount.Email,
                    FirstName = userAccount.FirstName,
                    Id = userAccount.Id,
                    LastName = userAccount.LastName,
                    Title = userAccount.Title
                },
                AgencyModel = userAccount.Agency is not null 
                    ? new AgencyModel
                    {
                        Address = userAccount.Agency.Address,
                        AgencyId = userAccount.AgencyId,
                        ContactEmail = userAccount.Agency.ContactEmail,
                        ContactName = userAccount.Agency.ContactName,
                        OrganisationName = userAccount.Agency.OrganisationName,
                        IsFcAgency = userAccount.Agency.IsFcAgency
                    }
                    : null
            }
        };
    }

    /// <summary>
    /// Closes an external <see cref="UserAccount"/>.
    /// </summary>
    /// <param name="userAccountId">The id of the user to close.</param>
    /// <param name="internalUser">The <see cref="InternalUser"/> performing the action.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result representing whether the account has been closed.</returns>
    public async Task<Result> CloseExternalUserAccountAsync(Guid userAccountId, InternalUser internalUser, CancellationToken cancellationToken)
    {
        if (internalUser.AccountType is not AccountTypeInternal.AccountAdministrator)
        {
            _logger.LogDebug("User must be an {accountAdministrator} to close user accounts, requesting id: {id}, user id: {userId}",
                AccountTypeInternal.AccountAdministrator.GetDisplayName(), internalUser.UserAccountId, userAccountId);
            return Result.Failure($"User must be an {AccountTypeInternal.AccountAdministrator.GetDisplayName()} to close user accounts");
        }

        if (internalUser.UserAccountId == userAccountId)
        {
            _logger.LogDebug("User cannot close their own account, user id: {userId}", userAccountId);
            return Result.Failure("User cannot close their own account");
        }

        var (_, isFailure, user, error) =
            await _amendExternalAccounts.UpdateApplicantAccountStatusAsync(userAccountId, UserAccountStatus.Deactivated, cancellationToken);

        if (isFailure)
        {
            _logger.LogError(error);

            await _audit.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.AccountAdministratorCloseAccountFailure,
                    userAccountId,
                    internalUser.UserAccountId,
                    _requestContext,
                    new
                    {
                        Error = error,
                        AdministratorAccountType = internalUser.AccountType.GetDisplayName()
                    }),
                cancellationToken);

            return Result.Failure(error);
        }

        _logger.LogDebug("Closing FC staff user account with id {userId}, requested by: {internalUserId}", userAccountId, internalUser.UserAccountId);

        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.AccountAdministratorCloseAccount,
                userAccountId,
                internalUser.UserAccountId,
                _requestContext,
                new
                {
                    ClosedAccountFullName = user.FullName,
                    ClosedAccountType = user.AccountType
                }),
            cancellationToken);

        return Result.Success();
    }

    public async Task<Result> VerifyAgentCanBeClosedAsync(Guid userId, Guid agencyId, CancellationToken cancellationToken)
    {
        var userAccount = await _retrieveUserAccountsService.RetrieveUserAccountEntityByIdAsync(userId, cancellationToken);
        var agencyUsers = await _retrieveUserAccountsService.RetrieveUsersLinkedToAgencyAsync(agencyId, cancellationToken);

        if (userAccount.IsFailure || agencyUsers.IsFailure)
        {
            return Result.Failure("Agent user account could not be retrieved.");
        }

        if (userAccount.Value.Status is not UserAccountStatus.Active)
        {
            return Result.Failure("Agent user is not active.");
        }

        var administratorCount = agencyUsers.Value.Count(x => x.AccountType is AccountTypeExternal.AgentAdministrator)
                                 - (userAccount.Value.AccountType is AccountTypeExternal.AgentAdministrator ? 1 : 0);

        return administratorCount > 0 
            ? Result.Success()
            : Result.Failure("There must be at least one agent administrator account at the agency.");
    }

    public async Task<bool> VerifyWoodlandOwnerCanBeClosedAsync(Guid userId, Guid woodlandOwnerId, CancellationToken cancellationToken)
    {
        var userAccount = await _retrieveUserAccountsService.RetrieveUserAccountEntityByIdAsync(userId, cancellationToken);
        var woodlandOwnerUsers = await _retrieveUserAccountsService.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, cancellationToken);

        if (userAccount.IsFailure || woodlandOwnerUsers.IsFailure)
        {
            return false;
        }

        var administratorCount = woodlandOwnerUsers.Value.Count(x => x.AccountType is AccountTypeExternal.WoodlandOwnerAdministrator)
                                 - (userAccount.Value.AccountType is AccountTypeExternal.WoodlandOwnerAdministrator ? 1 : 0);

        return userAccount.Value.Status is UserAccountStatus.Active
               && administratorCount > 0;
    }
}