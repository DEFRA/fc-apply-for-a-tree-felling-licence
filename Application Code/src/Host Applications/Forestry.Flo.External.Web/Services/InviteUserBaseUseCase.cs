using System.Web;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Base class for inviting users to organisations.
/// </summary>
public abstract class InviteUserBaseUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IInvitedUserValidator _invitedUserValidator;
    private readonly IClock _clock;
    private readonly UserInviteOptions _settings;

    private const string ErrorReceivedDuringInvitationProcessing =
        "An error received during processing of the user invitation request, please try again";

    /// <summary>
    /// Creates a new instance of <see cref="InviteUserBaseUseCase"/>.
    /// </summary>
    /// <param name="userAccountRepository">A <see cref="IUserAccountRepository"/> to interact with the database.</param>
    /// <param name="invitedUserValidator">A validator for new invited user accounts.</param>
    /// <param name="clock">A clock to get the current date and time.</param>
    /// <param name="options">Configuration options related to inviting users.</param>
    protected InviteUserBaseUseCase( 
        IUserAccountRepository userAccountRepository,
        IInvitedUserValidator invitedUserValidator,
        IClock clock,
        IOptions<UserInviteOptions> options)
    {
        _userAccountRepository = userAccountRepository;
        _invitedUserValidator = invitedUserValidator;
        _clock = clock;
        _settings = Guard.Against.Null(options).Value;
    }
    
    /// <summary>
    /// Verifies an invited user registration details: email and invitation token, against user database records
    /// </summary>
    /// <param name="email">Invited user email address</param>
    /// <param name="token">Invited user invitation token </param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>Success if the user invitation details are valid otherwise Failure result with the error message</returns>
    public async Task<Result<InvitedUserModel>> VerifyInvitedUserAccountAsync(
        string email, 
        string token,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(email);
        Guard.Against.Null(token);
        const string invitationNotFoundError =
            "We cannot find your registration details, please contact your organisation administrator";

        return await _userAccountRepository.GetByEmailAsync(email, cancellationToken)
            .MapError(r =>
                r == UserDbErrorReason.NotFound ? invitationNotFoundError : ErrorReceivedDuringInvitationProcessing)
            .Ensure(userAccount => _invitedUserValidator.VerifyInvitedUser(token, userAccount))
            .Map(userAccount =>
                new InvitedUserModel(userAccount.Email,
                    userAccount.AccountType is AccountTypeExternal.WoodlandOwner or AccountTypeExternal.WoodlandOwner
                        ? userAccount.WoodlandOwner?.OrganisationName ?? userAccount.WoodlandOwner?.ContactName
                        : userAccount.Agency?.OrganisationName ?? userAccount.Agency?.ContactName, token));
    }

    /// <summary>
    /// Invites a user to an organisation.
    /// </summary>
    /// <param name="organisationUserModel">A model of the invited user.</param>
    /// <param name="userAccount">A new <see cref="UserAccount"/> for the invited user.</param>
    /// <param name="systemUser">The user sending the invite.</param>
    /// <param name="inviteAcceptanceLink">The acceptance link to send in the email.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct containing either the <see cref="UserAccount"/> of
    /// the invited user, or a <see cref="InviteUserErrorDetails"/> if an error occurs.</returns>
    protected async Task<Result<UserAccount, InviteUserErrorDetails>> InviteUserToOrganisationAsync(
        IInvitedUser organisationUserModel,
        UserAccount userAccount,
        ExternalApplicant systemUser,
        string inviteAcceptanceLink,
        CancellationToken cancellationToken) 
    {
        Guard.Against.Null(organisationUserModel);
        Guard.Against.Null(userAccount);
        Guard.Against.Null(inviteAcceptanceLink);

        _userAccountRepository.Add(userAccount);
        return await Result.Success<UserAccount, InviteUserErrorDetails>(userAccount)
            .Check(async _ =>
                await _userAccountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
                    .MapError(async e =>
                        await CreateInviteUserErrorDetails(organisationUserModel, e, cancellationToken))
            )
            .Check<UserAccount, InviteUserErrorDetails>(async u =>
            {
                var result = await SendInvitationEmail(
                    organisationUserModel,
                    userAccount.InviteToken!.Value,
                    inviteAcceptanceLink, systemUser.FullName, cancellationToken);
                return result.Map(() => u)
                    .Compensate<UserAccount, InviteUserErrorDetails>(error =>
                        new InviteUserErrorDetails(InviteUserErrorResult.EmailSendingFailed, error));
            });
    }

    /// <summary>
    /// Resend an invitation to an already invited user.
    /// </summary>
    /// <param name="organisationUserModel">A model of the invited user.</param>
    /// <param name="user">The user triggering the resend.</param>
    /// <param name="inviteAcceptanceLink">The acceptance link to go in the resent email.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct containing either the <see cref="UserAccount"/> of the
    /// invited user, or an error.</returns>
    protected async Task<Result<UserAccount>> ReInviteUserToOrganisationAsync(
        IInvitedUser organisationUserModel,
        ExternalApplicant user,
        string inviteAcceptanceLink,
        CancellationToken cancellationToken) 
    {
        Guard.Against.Null(organisationUserModel);
        Guard.Against.Null(user);

        var now = _clock.GetCurrentInstant();

        return
            await _userAccountRepository.GetByEmailAsync(organisationUserModel.Email, cancellationToken: cancellationToken).MapError(e =>
                    e == UserDbErrorReason.NotFound
                        ? "We cannot find user registration details, please check the user email address"
                        : ErrorReceivedDuringInvitationProcessing)
                .Ensure(account => account.Status == UserAccountStatus.Invited, "Please check the user status before resending the invite")
                .Check(async account =>
                {
                    account.InviteToken = Guid.NewGuid();
                    account.InviteTokenExpiry = now.ToDateTimeUtc().AddDays(_settings.InviteLinkExpiryDays);
                    account.LastChanged = now.ToDateTimeUtc();
                    _userAccountRepository.Update(account);
                    return await _userAccountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken).MapError(_ => ErrorReceivedDuringInvitationProcessing);
                })
                .Check(async userAccount => 
                    await SendInvitationEmail(organisationUserModel, userAccount.InviteToken!.Value, inviteAcceptanceLink, user.FullName, cancellationToken));
    }

    private async Task<InviteUserErrorDetails> CreateInviteUserErrorDetails(
        IInvitedUser organisationUserModel, 
        UserDbErrorReason error,
        CancellationToken cancellationToken) 
    {
        switch (error)
        {
            case UserDbErrorReason.NotUnique:
            {
                var (_, isFailure, userAccount) = await _userAccountRepository.GetByEmailAsync(organisationUserModel.Email,
                    cancellationToken: cancellationToken);
                if (isFailure)
                {
                    return new InviteUserErrorDetails(InviteUserErrorResult
                        .OperationFailed, ErrorReceivedDuringInvitationProcessing);
                }

                if (userAccount.Status == UserAccountStatus.Active || CheckIfTheUserIsAlreadyInvitedByAnotherUser(userAccount, organisationUserModel))
                {
                    return new InviteUserErrorDetails(InviteUserErrorResult.UserAlreadyExists,
                        $"An account for the provided email address already exists");
                }

                return new InviteUserErrorDetails(InviteUserErrorResult.UserAlreadyInvited,
                    $"An invitation email has already been sent to the provided email address");
            }
            default:
                return new InviteUserErrorDetails(InviteUserErrorResult
                    .OperationFailed, ErrorReceivedDuringInvitationProcessing);
        }
    }

    /// <summary>
    /// Creates an invitation link for the invited user.
    /// </summary>
    /// <param name="organisationUserModel">A model of the user being invited.</param>
    /// <param name="token">The access token for the invite.</param>
    /// <param name="inviteAcceptanceLink">The base url for the accept invite location.</param>
    /// <returns>A full invite link containing parameters to go in an email.</returns>
    protected static string CreateInviteLink(IInvitedUser organisationUserModel, Guid token, string inviteAcceptanceLink) =>
        $"{inviteAcceptanceLink}?email={HttpUtility.UrlEncode((string?)organisationUserModel.Email)}&token={token.ToString()}";


    /// <summary>
    /// Checks if an existing user account has already been invited to another organisation.
    /// </summary>
    /// <param name="userAccount">The user account to check.</param>
    /// <param name="organisationUserModel">The invite model to check the existing account against.</param>
    /// <returns>True of the state of the user account is invited to another organisation, otherwise false.</returns>
    protected abstract bool CheckIfTheUserIsAlreadyInvitedByAnotherUser(UserAccount userAccount, IInvitedUser organisationUserModel) ;

    /// <summary>
    /// Send an invitation email to the invited user.
    /// </summary>
    /// <param name="invitedUserModel">A model representing the invited user.</param>
    /// <param name="token">The invite token to go in the email.</param>
    /// <param name="inviteAcceptanceLink">The link to go in the email.</param>
    /// <param name="userName">The name of the user sending the invite.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct indicating the outcome.</returns>
    protected abstract Task<Result> SendInvitationEmail(
        IInvitedUser invitedUserModel,
        Guid token,
        string inviteAcceptanceLink,
        string? userName,
        CancellationToken cancellationToken);

}

public interface IInvitedUser
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string OrganisationName { get; }
    public Guid OrganisationId { get; }
}

public enum InviteUserErrorResult
{
    UserAlreadyInvited,
    UserAlreadyExists,
    EmailSendingFailed,
    OperationFailed
}

public record InviteUserErrorDetails(InviteUserErrorResult ErrorResult, string Message);