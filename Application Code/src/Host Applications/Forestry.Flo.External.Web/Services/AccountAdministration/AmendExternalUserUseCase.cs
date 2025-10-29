using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.AccountAdministration;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.External.Web.Services.AccountAdministration;

/// <summary>
/// Handles use case for external administrator users to amend external user accounts.
/// </summary>
public class AmendExternalUserUseCase
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
    /// <param name="loggedInUser">The logged in external user performing the update.</param>
    /// <param name="model">A populated <see cref="AmendExternalUserAccountModel"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the <see cref="UserAccount"/> has been successfully updated.</returns>
    public async Task<Result> UpdateExternalAccountDetailsAsync(
        ExternalApplicant loggedInUser,
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

        var (_, userAccountFailure, userAccount, userAccountError) =
            await _retrieveUserAccountsService.RetrieveUserAccountEntityByIdAsync(model.UserId, cancellationToken);

        if (userAccountFailure)
        {
            var errorMessage = $"Unable to retrieve user account with id {model.UserId}, error: {userAccountError}";

            _logger.LogError("Unable to retrieve user account with id {UserId}, error: {UserAccountError}", model.UserId, userAccountError);
            await CreateFailureAuditAsync(loggedInUser, model.UserId, errorMessage, cancellationToken);

            return Result.Failure("Unable to retrieve user account");
        }

        if (userAccount.IsWoodlandOwner() && (loggedInUser.AccountType != AccountTypeExternal.WoodlandOwnerAdministrator || userAccount.WoodlandOwnerId != Guid.Parse(loggedInUser.WoodlandOwnerId!)))
        {
            var errorMessage =
                $"Logged in user lacks authority to amend woodland owner user account with id {model.UserId}";

            _logger.LogError("Logged in user lacks authority to amend woodland owner user account with id {UserId}", model.UserId);
            await CreateFailureAuditAsync(loggedInUser, model.UserId, errorMessage, cancellationToken);

            return Result.Failure("Logged in user lacks authority to amend woodland owner user account");
        }

        if (userAccount.IsAgent() && (loggedInUser.AccountType != AccountTypeExternal.AgentAdministrator || userAccount.AgencyId != Guid.Parse(loggedInUser.AgencyId!)))
        {
            var errorMessage =
                $"Logged in user lacks authority to amend agent user account with id {model.UserId}";

            _logger.LogError("Logged in user lacks authority to amend agent user account with id {UserId}", model.UserId);
            await CreateFailureAuditAsync(loggedInUser, model.UserId, errorMessage, cancellationToken);

            return Result.Failure("Logged in user lacks authority to amend agent user account");
        }

        var (_, isFailure, userUpdated, error) = await _amendExternalAccounts.UpdateUserAccountDetailsAsync(updateModel, cancellationToken);

        if (isFailure)
        {
            var errorMessage = $"Unable to update external user account {model.UserId}, error: {error}";

            _logger.LogError("Unable to update external user account {UserId}, error: {Error}", model.UserId, error);
            
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

    private async Task CreateFailureAuditAsync(ExternalApplicant externalUser, Guid targetUserId, string error, CancellationToken cancellationToken)
    {
        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.AdministratorUpdateExternalAccountFailure,
                targetUserId,
                externalUser.UserAccountId,
                _requestContext,
                new
                {
                    Error = error,
                    AdministratorAccountType = externalUser.AccountType!.GetDisplayName()
                }), cancellationToken);
    }
}