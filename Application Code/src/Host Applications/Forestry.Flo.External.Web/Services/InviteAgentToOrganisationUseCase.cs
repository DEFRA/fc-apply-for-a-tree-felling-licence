using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.Agency;
using Forestry.Flo.External.Web.Models.UserAccount;
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

public class InviteAgentToOrganisationUseCase : InviteUserBaseUseCase
{
    private readonly IAuditService<AgencyUserModel> _auditService;
    private readonly IAgencyRepository _agencyRepository;
    private readonly RequestContext _requestContext;
    private readonly UserInviteOptions _settings;
    private readonly ILogger<InviteAgentToOrganisationUseCase> _logger;
    private readonly ISendNotifications _emailService;
    private readonly IClock _clock;

    public InviteAgentToOrganisationUseCase(
        IAuditService<AgencyUserModel> auditService,
        IUserAccountRepository userAccountRepository,
        IAgencyRepository agencyRepository,
        ILogger<InviteAgentToOrganisationUseCase> logger,
        ISendNotifications emailService,
        IClock clock,
        IOptions<UserInviteOptions> options,
        RequestContext requestContext,
        IInvitedUserValidator invitedUserValidator): base(userAccountRepository, invitedUserValidator, clock, options)
    {
        _auditService = auditService;
        _agencyRepository = agencyRepository;
        _requestContext = Guard.Against.Null(requestContext);
        _settings = Guard.Against.Null(options).Value;
        _logger = Guard.Against.Null(logger);
        _emailService = Guard.Against.Null(emailService);
        _clock = Guard.Against.Null(clock);
    }

    /// <summary>
    /// Resends an invitation email to the already invited agent user
    /// </summary>
    /// <param name="agencyUserModel">Agent user details</param>
    /// <param name="user">Current application user</param>
    /// <param name="inviteAcceptanceLink">The url to accept the agent user invite </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure of the operation</returns>
    public async Task<Result> ReInviteAgentToOrganisationAsync(
        AgencyUserModel agencyUserModel,
        ExternalApplicant user,
        string inviteAcceptanceLink,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(agencyUserModel);
        Guard.Against.Null(user);
        return
            await ReInviteUserToOrganisationAsync(agencyUserModel, user, inviteAcceptanceLink, cancellationToken)
            .OnFailure(async error => await PublishAuditEvent(user, null, agencyUserModel.Email,
                    null, AuditEvents.AgencyUserInvitationFailure,
                    cancellationToken, $"Error received during sending an invitation email: {error}")
                )
            .Tap(async userAccount => await PublishAuditEvent(user, userAccount.Id, userAccount.Email,
              userAccount.InviteTokenExpiry,
               AuditEvents.AgencyUserInvitationSent, cancellationToken));
    }

    /// <summary>
    /// Sends an invitation email to the new agent user
    /// </summary>
    /// <param name="agencyUserModel">Agent user details</param>
    /// <param name="user">Current application user</param>
    /// <param name="inviteAcceptanceLink">The url to accept the agent user invite </param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success or failure of the operation containing operation status</returns>
    public async Task<Result<UserAccount, InviteUserErrorDetails>> InviteAgentToOrganisationAsync(
        AgencyUserModel agencyUserModel,
        ExternalApplicant user,
        string inviteAcceptanceLink,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(agencyUserModel);
        Guard.Against.Null(user);
        Guard.Against.Null(inviteAcceptanceLink);

        var now = _clock.GetCurrentInstant();

        var userAccount = new UserAccount
        {
            Email = agencyUserModel.Email,
            Status = UserAccountStatus.Invited,
            AccountType = agencyUserModel.AgencyUserRole == AgencyUserRole.AgencyAdministrator
                ? AccountTypeExternal.AgentAdministrator
                : AccountTypeExternal.Agent,
            InviteToken = Guid.NewGuid(),
            InviteTokenExpiry = now.ToDateTimeUtc().AddDays(_settings.InviteLinkExpiryDays),
            AgencyId = user.AgencyId != null ? Guid.Parse(user.AgencyId) : null,
            LastChanged = now.ToDateTimeUtc()
        };

        return await InviteUserToOrganisationAsync(agencyUserModel, userAccount, user, inviteAcceptanceLink, cancellationToken)
            .Tap(async u =>
            {
                await PublishAuditEvent(user, u.Id, u.Email, u.InviteTokenExpiry, AuditEvents.AgencyUserInvitationSent, cancellationToken);
            })
            .OnFailure(async error => await PublishAuditEvent(user, null, agencyUserModel.Email, null,
                AuditEvents.AgencyUserInvitationFailure, cancellationToken, $"Error received during sending an invitation email: {error.Message}")
            );
    }
    
    /// <summary>
    /// Return the current user agency organisation
    /// </summary>
    /// <param name="user">Current system user</param>
    /// <param name="cancellationToken">Cancellation user</param>
    /// <returns>Success or failure of the operation containing the requested agency organisation</returns>
    public async Task<Result<AgencyDetails>> RetrieveUserAgencyAsync(ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        if (user.AgencyId is null)
        {
            return Result.Failure<AgencyDetails>("Your account is not linked to any agency");
        }

        if (user.AccountType != AccountTypeExternal.AgentAdministrator)
        {
            return Result.Failure<AgencyDetails>("You do not have permissions to manage agents");
        }

        return await _agencyRepository.GetAsync(Guid.Parse(user.AgencyId!),
                cancellationToken).MapError(_ =>
            {
                _logger.LogError("The agency with id: {UserAgencyId} has not been found", user.AgencyId);
                return "The agency organisation does not exists";
            })
            .Ensure(agency => !string.IsNullOrEmpty(agency.OrganisationName),
                "Please check the agency organisation name")
            .Map(agency => new AgencyDetails(agency.Id, agency.OrganisationName!));
    }

    protected override async Task<Result> SendInvitationEmail(IInvitedUser agencyUserModel,
        Guid token,
        string inviteAcceptanceLink,
        string? userName,
        CancellationToken cancellationToken) 
    {
        var inviteeModel = new InviteAgentToOrganisationDataModel
        {
            Name = agencyUserModel.Name,
            AgencyName = agencyUserModel.OrganisationName,
            InviteLink =
                CreateInviteLink(agencyUserModel, token, inviteAcceptanceLink)
        };

        var result = await _emailService.SendNotificationAsync(inviteeModel,
            NotificationType.InviteAgentUserToOrganisation,
            new NotificationRecipient(agencyUserModel.Email, agencyUserModel.Name),
            senderName: userName,
            cancellationToken: cancellationToken);
        return result;
    }

    protected override bool CheckIfTheUserIsAlreadyInvitedByAnotherUser(UserAccount userAccount, IInvitedUser organisationUserModel) =>
        userAccount.Status == UserAccountStatus.Invited &&
        userAccount.AgencyId !=
        organisationUserModel.OrganisationId;

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

