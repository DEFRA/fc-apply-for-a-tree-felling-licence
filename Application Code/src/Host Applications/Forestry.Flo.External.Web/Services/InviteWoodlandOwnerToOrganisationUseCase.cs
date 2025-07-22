using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.Notifications.Entities;
using Forestry.Flo.Services.Notifications.Models;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.External.Web.Services;

public class InviteWoodlandOwnerToOrganisationUseCase : InviteUserBaseUseCase
{
    private readonly IAuditService<OrganisationWoodlandOwnerUserModel> _auditService;
    private readonly RequestContext _requestContext;
    private readonly IWoodlandOwnerRepository _woodlandOwnerRepository;
    private readonly UserInviteOptions _settings;
    private readonly ILogger<InviteWoodlandOwnerToOrganisationUseCase> _logger;
    private readonly ISendNotifications _emailService;
    private readonly IClock _clock;


    public InviteWoodlandOwnerToOrganisationUseCase(
        IAuditService<OrganisationWoodlandOwnerUserModel> auditService,
        IUserAccountRepository userAccountRepository,
        IWoodlandOwnerRepository woodlandOwnerRepository,
        ILogger<InviteWoodlandOwnerToOrganisationUseCase> logger,
        ISendNotifications emailService,
        IClock clock,
        IOptions<UserInviteOptions> options,
        RequestContext requestContext,
        IInvitedUserValidator invitedUserValidator): base(userAccountRepository, invitedUserValidator, clock, options)
    {
        _auditService = auditService;
        _requestContext = Guard.Against.Null(requestContext);
        _woodlandOwnerRepository = Guard.Against.Null(woodlandOwnerRepository);
        _settings = Guard.Against.Null(options).Value;
        _logger = Guard.Against.Null(logger);
        _emailService = Guard.Against.Null(emailService);
        _clock = Guard.Against.Null(clock);
    }

    /// <summary>
    /// Resends an invitation email to the already invited woodland owner user
    /// </summary>
    /// <param name="woodlandOwnerUserModel">Woodland owner user details</param>
    /// <param name="user">Current application user</param>
    /// <param name="inviteAcceptanceLink">The url to accept woodland owner organisation invite </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure of the operation</returns>
    public async Task<Result> ReInviteWoodlandOwnerToOrganisationAsync(
        OrganisationWoodlandOwnerUserModel woodlandOwnerUserModel,
        ExternalApplicant user,
        string inviteAcceptanceLink,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(woodlandOwnerUserModel);
        Guard.Against.Null(user);
        return
            await ReInviteUserToOrganisationAsync(woodlandOwnerUserModel, user, inviteAcceptanceLink, cancellationToken)
                .OnFailure(async error => await PublishAuditEvent(user, null, woodlandOwnerUserModel.Email,
                    null, AuditEvents.WoodlandOwnerUserInvitationFailure,
                    cancellationToken, $"Error received during sending an invitation email: {error}")
                )
                .Tap(async userAccount => await PublishAuditEvent(user, userAccount.Id, userAccount.Email,
                    userAccount.InviteTokenExpiry,
                    AuditEvents.WoodlandOwnerUserInvitationSent, cancellationToken));
    }

    /// <summary>
    /// Sends an invitation email to the new woodland owner user
    /// </summary>
    /// <param name="woodlandOwnerUserModel">Woodland owner user details</param>
    /// <param name="user">Current application user</param>
    /// <param name="inviteAcceptanceLink">The url to accept woodland owner organisation invite </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure of the operation containing operation status</returns>
    public async Task<Result<UserAccount, InviteUserErrorDetails>> InviteWoodlandOwnerToOrganisationAsync(
        OrganisationWoodlandOwnerUserModel woodlandOwnerUserModel,
        ExternalApplicant user,
        string inviteAcceptanceLink,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(woodlandOwnerUserModel);
        Guard.Against.Null(user);
        Guard.Against.Null(inviteAcceptanceLink);

        var now = _clock.GetCurrentInstant();

        var userAccount = new UserAccount
        {
            Email = woodlandOwnerUserModel.Email,
            Status = UserAccountStatus.Invited,
            AccountType = woodlandOwnerUserModel.WoodlandOwnerUserRole == WoodlandOwnerUserRole.WoodlandOwnerAdministrator
                ? AccountTypeExternal.WoodlandOwnerAdministrator
                : AccountTypeExternal.WoodlandOwner,
            InviteToken = Guid.NewGuid(),
            InviteTokenExpiry = now.ToDateTimeUtc().AddDays(_settings.InviteLinkExpiryDays),
            WoodlandOwnerId = user.WoodlandOwnerId != null ? Guid.Parse(user.WoodlandOwnerId) : null,
            LastChanged = now.ToDateTimeUtc()
        };
        return await InviteUserToOrganisationAsync(woodlandOwnerUserModel, userAccount, user, inviteAcceptanceLink, cancellationToken)
            .Tap(async u =>
            {
                await PublishAuditEvent(user, u.Id, u.Email, u.InviteTokenExpiry, AuditEvents.WoodlandOwnerUserInvitationSent, cancellationToken);
            })
            .OnFailure(async error => await PublishAuditEvent(user, null, woodlandOwnerUserModel.Email, null, 
                AuditEvents.WoodlandOwnerUserInvitationFailure, cancellationToken, $"Error received during sending an invitation email: {error.Message}")
            );
    }

    protected override async Task<Result> SendInvitationEmail(IInvitedUser woodlandOwnerUserModel,
        Guid token,
        string inviteAcceptanceLink,
        string? userName,
        CancellationToken cancellationToken) 
    {
        var inviteeModel = new InviteWoodlandOwnerToOrganisationDataModel
        {
            Name = woodlandOwnerUserModel.Name,
            WoodlandOwnerName = woodlandOwnerUserModel.OrganisationName,
            InviteLink =
                CreateInviteLink(woodlandOwnerUserModel, token, inviteAcceptanceLink)
        };

        var result = await _emailService.SendNotificationAsync(inviteeModel,
            NotificationType.InviteWoodlandOwnerUserToOrganisation,
            new NotificationRecipient(woodlandOwnerUserModel.Email, woodlandOwnerUserModel.Name),
            senderName: userName,
            cancellationToken: cancellationToken);
        return result;
    }

    protected override bool CheckIfTheUserIsAlreadyInvitedByAnotherUser(UserAccount userAccount,IInvitedUser organisationUserModel) =>
        userAccount.Status == UserAccountStatus.Invited &&
        userAccount.WoodlandOwnerId !=
        organisationUserModel.OrganisationId;
    
    /// <summary>
    /// Return the current user woodland owner organisation
    /// </summary>
    /// <param name="user">Current system user</param>
    /// <param name="cancellationToken">Cancellation user</param>
    /// <returns>Success or failure of the operation containing the requested woodland owner organisation</returns>
    public async Task<Result<WoodlandOwnerDetails>> RetrieveUserWoodlandOwnerOrganisationAsync(ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        if (user.WoodlandOwnerId is null)
        {
            return Result.Failure<WoodlandOwnerDetails>("Your account is not linked to any Woodland owner");
        }

        if (user.AccountType != AccountTypeExternal.WoodlandOwnerAdministrator)
        {
            return Result.Failure<WoodlandOwnerDetails>("You do not have permissions to manage other users");
        }

        return await _woodlandOwnerRepository.GetAsync(Guid.Parse(user.WoodlandOwnerId!),
                cancellationToken).MapError(_ =>
            {
                _logger.LogError("Woodland owner with id: {UserWoodlandOwnerId} has not been found", user.WoodlandOwnerId);
                return "Woodland owner does not exist";
            })
            .Map(owner => new WoodlandOwnerDetails(owner.Id, owner.IsOrganisation ? owner.OrganisationName! : owner.ContactName!));
    }
    
    private async Task PublishAuditEvent(ExternalApplicant user, Guid? inviteeAccountId,
        string email, DateTime? tokenExpiry, string eventName, CancellationToken cancellationToken,
        string? error = null)
    {
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                eventName,
                inviteeAccountId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    InvitedByUserId = user.UserAccountId,
                    InviteeEmailAddress = email,
                    InviteExpiryDateTime = tokenExpiry,
                    Error = error
                }),
            cancellationToken);
    }
}

