using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Forestry.Flo.Services.Applicants.Services;

public class InvitedUserValidator : IInvitedUserValidator
{
    private readonly ILogger<InvitedUserValidator> _logger;
    private readonly IClock _clock;

    public InvitedUserValidator(ILogger<InvitedUserValidator> logger,IClock clock)
    {
        _logger = logger;
        _clock = clock;
    }
    
    public Result VerifyInvitedUser(string queryToken, UserAccount userAccount)
    {
        if (queryToken != userAccount.InviteToken.ToString())
        {
            _logger.LogError("Invited user token {QueryToken} for the user with id {UserAccountId} does not match our records",
                queryToken, userAccount.Id);
            return Result.Failure("The invitation link is invalid");
        }
        
        if (userAccount.InviteTokenExpiry < _clock.GetCurrentInstant().ToDateTimeUtc())
        {
            _logger.LogError("Invited user invitation token has expired, user id: {Id}, token expiry: {TokenExpiry}",
                queryToken, userAccount.Id);
            return Result.Failure("The invitation link has expired, please contact your organisation administrator to send you another user invitation email");
        }

        if (userAccount.Status != UserAccountStatus.Invited)
        {
            _logger.LogError("Invited user status is not Invited for the user with id {UserAccountId}", userAccount.Id);
            return Result.Failure("The invitation link is no longer active");
        }
        return Result.Success();
    }
}