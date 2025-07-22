using System.Security.Claims;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.Applicants.Services;

public class SignInApplicantWithEf : ISignInApplicant
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly FcAgencyOptions _fcAgencyOptions;
    private readonly IInvitedUserValidator _invitedUserValidator;
    private readonly IAuditService<SignInApplicantWithEf> _auditService;
    private readonly ILogger<SignInApplicantWithEf> _logger;
    private readonly RequestContext _requestContext;
    private const string IdentityProviderEmailClaimType = "emails";

    public SignInApplicantWithEf(
        IUserAccountRepository userAccountRepository,
        IInvitedUserValidator invitedUserValidator,
        ILogger<SignInApplicantWithEf> logger,
        IAuditService<SignInApplicantWithEf> auditService,
        IOptions<FcAgencyOptions> fcAgencyOptions,
        RequestContext requestContext)
    {
        _userAccountRepository = userAccountRepository ?? throw new ArgumentNullException(nameof(userAccountRepository));
        _invitedUserValidator = invitedUserValidator;
        _auditService = auditService;
        _logger = logger;
        _fcAgencyOptions = Guard.Against.Null(fcAgencyOptions.Value);
        _requestContext = requestContext;
    }

    public async Task HandleUserLoginAsync(ClaimsPrincipal user, string? inviteToken, CancellationToken cancellationToken = default)
    {
        var userIdentifier = user.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdentifier))
        {
            return;
        }

        await _userAccountRepository.GetByUserIdentifierAsync(userIdentifier, cancellationToken)
            .Tap(userAccount =>
            {
                // This condition runs if user is identifiable in database by identifier.
                // Note that Authorize succeeds regardless of user.AddIdentity

                var claimsIdentity = ClaimsIdentityHelper.CreateClaimsIdentityFromApplicantUserAccount(userAccount, _fcAgencyOptions.PermittedEmailDomainsForFcAgent);
                user.AddIdentity(claimsIdentity);
            })
            .OnFailure(async () =>
            {
                await TryGetClaimsFromInvitedUser(user, inviteToken, cancellationToken);
            });
    }

    private async Task TryGetClaimsFromInvitedUser(ClaimsPrincipal user, string? inviteToken,
        CancellationToken cancellationToken = default)
    {
        var userIdentifier = user.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        var userEmail = user.Claims.SingleOrDefault(x => x.Type == IdentityProviderEmailClaimType)?.Value;

        if (inviteToken == null) return;

        if (userEmail is null) return;

        await _userAccountRepository.GetByEmailAsync(userEmail, cancellationToken)
            .MapError(_ =>
            {
                _logger.LogWarning("User account with email: {UserEmail} is not found in the database", userEmail);
                return "Invited user is not found in the database";
            })
            .Check(userAccount => _invitedUserValidator.VerifyInvitedUser(inviteToken, userAccount))
            .Ensure(async userAccount => await AcceptInvitedUser(userAccount, userIdentifier, cancellationToken)
                .MapError(_ => "Error occurred on saving the user account"))
            .Tap(async userAccount =>
            {
                var claimsIdentity = ClaimsIdentityHelper.CreateClaimsIdentityFromApplicantUserAccount(userAccount, _fcAgencyOptions.PermittedEmailDomainsForFcAgent);
                user.AddIdentity(claimsIdentity);
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.AcceptInvitationSuccess,
                        userAccount.Id, userAccount.Id,
                       _requestContext,
                        auditData: new { EmailAddress = userEmail }
                        ),
                    cancellationToken);
            })
            .OnFailure(async error =>
            {
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.AcceptInvitationFailure,
                        null, 
                        null, 
                        _requestContext,
                        auditData: new { EmailAddress = userEmail, Error = error }),
                    cancellationToken);
            });
    }

    private async Task<UnitResult<UserDbErrorReason>> AcceptInvitedUser(UserAccount user, string? userIdentifier,
        CancellationToken cancellationToken = default)
    {
        user.Status = UserAccountStatus.Active;
        user.IdentityProviderId = userIdentifier;
        _userAccountRepository.Update(user);
        return await _userAccountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }

   
}