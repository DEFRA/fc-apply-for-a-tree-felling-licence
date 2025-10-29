using System.Security.Claims;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.InternalUsers.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using MassTransit;

namespace Forestry.Flo.Internal.Web.Services;

public class RegisterUserAccountUseCase : IRegisterUserAccountUseCase
{
    private readonly RequestContext _requestContext;
    private readonly IAuditService<RegisterUserAccountUseCase> _auditService;
    private readonly ISignInInternalUser _signInInternalUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<RegisterUserAccountUseCase> _logger;
    private readonly IUserAccountService _userAccountService;
    private readonly ISendNotifications _notificationsService;
    private readonly IPublishEndpoint _publishEndpoint;

    public RegisterUserAccountUseCase(
        IHttpContextAccessor httpContextAccessor,
        ISignInInternalUser signInInternalUser,
        IAuditService<RegisterUserAccountUseCase> auditService,
        RequestContext requestContext,
        ILogger<RegisterUserAccountUseCase> logger,
        IUserAccountService userAccountService,
        ISendNotifications notificationsService,
        IPublishEndpoint publishEndpoint)
    {
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(signInInternalUser);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(userAccountService);
        ArgumentNullException.ThrowIfNull(notificationsService);
        ArgumentNullException.ThrowIfNull(publishEndpoint);

        _requestContext = requestContext;
        _auditService = auditService;
        _signInInternalUser = signInInternalUser;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _userAccountService = userAccountService;
        _notificationsService = notificationsService;
        _publishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Retrieves a populated <see cref="UserRegistrationDetailsModel"/> from an internal user's identity provider ID.
    /// </summary>
    /// <param name="user">An internal user.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="UserRegistrationDetailsModel"/> model.</returns>
    public async Task<Maybe<UserRegistrationDetailsModel>> GetUserAccountModelAsync(InternalUser user, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        var userAccount =
            await _userAccountService.GetUserAccountByIdentityProviderIdAsync(
                user.IdentityProviderId!,
                cancellationToken);

        if (userAccount.HasNoValue)
        {
            _logger.LogError("Unable to retrieve user account from identity provider ID");
            return Maybe<UserRegistrationDetailsModel>.None;
        }

        return ModelMapping.ToUserRegistrationDetailsModel(userAccount.Value);
    }

    /// <summary>
    /// Retrieves a <see cref="UserRegistrationDetailsModel"/> model from a user account ID.
    /// </summary>
    /// <param name="id">A user account ID.</param>
    /// <param name="actingUser">The user performing the operation.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="UserRegistrationDetailsModel"/>.</returns>
    public async Task<Result<UpdateUserRegistrationDetailsModel>> GetUserAccountModelByIdAsync(
        Guid id, 
        InternalUser actingUser, 
        CancellationToken cancellationToken)
    {
        var userAccount = await _userAccountService.GetUserAccountAsync(id, cancellationToken);

        if (userAccount.HasNoValue)
        {
            _logger.LogError("Unable to retrieve user account with id {id}", id);
            return Result.Failure<UpdateUserRegistrationDetailsModel>($"Unable to retrieve user account with id {id}");
        }

        var result = ModelMapping.ToUpdateUserRegistrationDetailsModel(userAccount.Value);

        result.AllowRoleChange = id != actingUser.UserAccountId;
        if (result.RequestedAccountType != AccountTypeInternal.AccountAdministrator)
        {
            result.DisallowedRoles.Add(AccountTypeInternal.AccountAdministrator);
        }

        return Result.Success(result);
    }

    public async Task<Result> UpdateAccountRegistrationDetailsByIdAsync(
        InternalUser performingUser,
        UpdateUserRegistrationDetailsModel model,
        CancellationToken cancellationToken)
    {
        var updateDetailsModel = new UpdateRegistrationDetailsModel
        {
            AccountType = model.RequestedAccountType,
            Title = model.Title,
            FirstName = model.FirstName,
            LastName = model.LastName,
            UserAccountId = model.Id,
            Roles = model.RequestedAccountType is AccountTypeInternal.AccountAdministrator
                ? new List<Roles> { Roles.FcAdministrator, Roles.FcUser }
                : new List<Roles> { Roles.FcUser },
            CanApproveApplications = model is { CanApproveApplications: true, RequestedAccountType: AccountTypeInternal.FieldManager or AccountTypeInternal.WoodlandOfficer }
        };

        var (_, isFailure, userAccount, error) = await _userAccountService.UpdateUserAccountDetailsAsync(
            updateDetailsModel,
            cancellationToken);

        if (isFailure)
        {
            await _auditService.PublishAuditEventAsync(new AuditEvent(
                    AuditEvents.AccountAdministratorUpdateExistingAccountFailure,
                    model.Id,
                    performingUser.UserAccountId,
                    _requestContext,
                    new
                    {
                        Error = error,
                        RequestedAccountType = model.RequestedAccountType,
                        RequestedFullName = updateDetailsModel.FullName
                    }),
                cancellationToken);

            _logger.LogError("Unable to update user account {id}, error: {error}", model.Id, error);

            return Result.Failure($"Unable to update user account {model.Id}, error: {error}");
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.AccountAdministratorUpdateExistingAccount,
            model.Id,
            performingUser.UserAccountId,
            _requestContext,
            new
            {
                UpdatedAccountType = model.RequestedAccountType,
                UpdatedFullName = updateDetailsModel.FullName
            }),
            cancellationToken);

        if (performingUser.UserAccountId == userAccount.Id)
        {
            await RefreshSigninAsync(performingUser, userAccount);
        }

        _logger.LogDebug("Successfully updated registration details for user account {id}, by user {performingId}", model.Id, performingUser.UserAccountId);

        return Result.Success();
    }

    /// <summary>
    /// Attempts to update the name details in the local account for the given user with values from a UI model.
    /// </summary>
    /// <param name="user">An <see cref="InternalUser"/> representing the current user.</param>
    /// <param name="model">The model containing updated values.</param>
    /// <param name="confirmAccountBaseUrl">The base URL for the confirm account page, to go in the notification.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result> UpdateAccountRegistrationDetailsAsync(
        InternalUser user, 
        UserRegistrationDetailsModel model, 
        string confirmAccountBaseUrl,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        var (_, isFailure, userAccount, error) = await _userAccountService.UpdateUserAccountDetailsAsync(
            new UpdateRegistrationDetailsModel
            {
                AccountType = model.RequestedAccountType,
                AccountTypeOther = model.RequestedAccountType == AccountTypeInternal.Other ? model.RequestedAccountTypeOther : null,
                Title = model.Title,
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserAccountId = user.UserAccountId!.Value,
                Roles = model.RequestedAccountType is AccountTypeInternal.AccountAdministrator
                    ? new List<Roles> { Roles.FcAdministrator, Roles.FcUser }
                    : new List<Roles> { Roles.FcUser },
                CanApproveApplications = model.RequestedAccountType is AccountTypeInternal.FieldManager
            },
            cancellationToken);

        if (isFailure)
        {
            _logger.LogError("Exception caught updating existing user account for terms and conditions");

            var auditData = new
            {
                AccountType = model.RequestedAccountType,
                user.IdentityProviderId,
                error
            };

            await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateAccountFailureEvent,
                    user.UserAccountId,
                    user.UserAccountId,
                    _requestContext,
                    auditData), cancellationToken);
            return Result.Failure(error);
        }

        await AuditUserAccountEvent(AuditEvents.UpdateAccountEvent, user, userAccount, cancellationToken).ConfigureAwait(false);

        await InformAdminsOfNewAccountAsync(user, model, confirmAccountBaseUrl, cancellationToken).ConfigureAwait(false);

        await RefreshSigninAsync(user, userAccount).ConfigureAwait(false);

        return Result.Success();
    }

    /// <summary>
    /// Updates the <see cref="Status"/> of an internal <see cref="UserAccount"/>.
    /// </summary>
    /// <param name="performingUser">The user performing this action</param>
    /// <param name="userId">The identifier for the <see cref="UserAccount"/>.</param>
    /// <param name="requestedStatus">The <see cref="Status"/> to update the account with.</param>
    /// <param name="setCanApproveApplications">A flag indicating if the user should be allowed to approve applications.</param>
    /// <param name="loginUrl">A URL to allow the recipient of a notification to log in.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result indicating whether the <see cref="Status"/> has been successfully updated for the <see cref="UserAccount"/>.</returns>
    public async Task<Result> UpdateUserAccountStatusAsync(
        InternalUser performingUser,
        Guid userId,
        Status requestedStatus,
        bool? setCanApproveApplications,
        string loginUrl,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update account status to {status}, for account {id}", requestedStatus, userId);

        if (requestedStatus is not (Status.Confirmed or Status.Denied))
        {
            return Result.Failure("Requested status must be confirmed or denied");
        }

        try
        {
            if (requestedStatus is Status.Confirmed)
            {
                await _userAccountService.UpdateUserAccountConfirmedAsync(userId, setCanApproveApplications, cancellationToken);

                _logger.LogInformation("Successfully updated user account status to {status}", Status.Confirmed);
            }
            else
            {
                await _userAccountService.UpdateUserAccountDeniedAsync(userId, cancellationToken);

                _logger.LogInformation("Successfully updated user account status to {status}", Status.Denied);
                return Result.Success();
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Unable to update account status for user {userId}", userId);
            return Result.Failure($"Unable to update account status for user {userId}");
        }

        var userModel = await _userAccountService.GetUserAccountAsync(userId, cancellationToken);

        if (userModel.HasNoValue)
        {
            // still return a success, as the entity has been updated
            _logger.LogWarning("Unable to retrieve user account to send account approval notification");
            return Result.Success();
        }

        await RequestNewExternalAccountForFcUserAsync(userId, userModel.Value, performingUser, cancellationToken);

        _logger.LogInformation("Attempting to inform user {id} of their account's approval", userId);

        var notificationModel = new InformInternalUserOfAccountApprovalDataModel
        {
            Name = userModel.Value.FullName(),
            LoginUrl = loginUrl
        };

        await _notificationsService.SendNotificationAsync(
            notificationModel,
            NotificationType.InformInternalUserOfAccountApproval,
            new NotificationRecipient(userModel.Value.Email, userModel.Value.FullName()),
            cancellationToken: cancellationToken
        );

        return Result.Success();
    }

    private async Task RequestNewExternalAccountForFcUserAsync(
        Guid userId,
        UserAccount userModel, 
        InternalUser performingUser, 
        CancellationToken cancellationToken)
    {
        var createExternalUserProfileForFcUserEvent = new InternalFcUserAccountApprovedEvent(
            userModel.Email,
            userModel.FirstName!,
            userModel.LastName!,
            userModel.IdentityProviderId!,
            performingUser.UserAccountId!.Value);

        _logger.LogDebug("About to publish event to request a new EXTERNAL user account for this newly approved FC user having id of {userId}", userId);
 
        await _publishEndpoint.Publish(
            createExternalUserProfileForFcUserEvent,
            cancellationToken);
        
        _logger.LogDebug("Event published");
    }
    private async Task RefreshSigninAsync(InternalUser user, UserAccount account)
    {
        // refresh logged in user claims for new local account
        List<ClaimsIdentity> identities = new()
        {
            _signInInternalUser.CreateClaimsIdentityFromUserAccount(account)
        };

        identities.AddRange(user.Principal.Identities.Where(x => x.AuthenticationType != FloClaimTypes.ClaimsIdentityAuthenticationType));

        user = new InternalUser(new ClaimsPrincipal(identities));
            
        if (_httpContextAccessor.HttpContext != null)
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await _httpContextAccessor.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user.Principal);
        }
    }

    private Task AuditUserAccountEvent(string eventName, InternalUser user, UserAccount account, CancellationToken cancellationToken = default) =>
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

    private async Task InformAdminsOfNewAccountAsync(
        InternalUser user, 
        UserRegistrationDetailsModel model, 
        string confirmAccountBaseUrl,
        CancellationToken cancellationToken)
    {
        var users = await _userAccountService
            .GetConfirmedUsersByAccountTypeAsync(AccountTypeInternal.AccountAdministrator, null, cancellationToken)
            .ConfigureAwait(false);

        if (users.IsFailure)
        {
            _logger.LogWarning("Failed to retrieve admin users for new account notification, error: {Error}", users.Error);
            return;
        }

        if (users.Value.NotAny())
        {
            _logger.LogWarning("No admin users found for new account notification");
            return;
        }

        var recipients = users.Value
            .Select(x => new NotificationRecipient(x.Email, x.FullName))
            .ToArray();

        foreach (var recipient in recipients)
        {
            _logger.LogInformation("Sending new account notification to {Recipient}", recipient.Name);

            var notification = new InformAdminOfNewAccountSignupDataModel
            {
                Name = recipient.Name,
                AccountEmail = user.EmailAddress,
                AccountName = $"{model.FirstName} {model.LastName}",
                ConfirmAccountUrl = $"{confirmAccountBaseUrl}?userAccountId={user.UserAccountId}"
            };

            var notificationResult = await _notificationsService.SendNotificationAsync(
                    notification, NotificationType.InformAdminOfNewAccountSignup, recipient, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (notificationResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to send new account notification to {Name}, error: {Error}",
                    recipient.Name,
                    notificationResult.Error);
            }
        }

    }
}