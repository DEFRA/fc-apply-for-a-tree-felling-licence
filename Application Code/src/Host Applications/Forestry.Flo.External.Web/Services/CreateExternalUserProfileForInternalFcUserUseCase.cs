using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services.MassTransit.Consumers;
using Forestry.Flo.Services.Applicants.Configuration;
using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.User;
using Microsoft.Extensions.Options;
using NodaTime;
using Agency = Forestry.Flo.Services.Applicants.Entities.Agent.Agency;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Class handling the use case of adding a newly approved INTERNAL FC user as
/// an External User assigned to the FC agency.
/// </summary>
/// <remarks>This class is called by the <see cref="InternalFcUserAccountApprovedEventConsumer"/>, which handles
/// the message event <see cref="InternalFcUserAccountApprovedEvent"/> dispatched by the Internal application.</remarks>
public class CreateExternalUserProfileForInternalFcUserUseCase
{
    private readonly IAccountRegistrationService _accountRegistrationService;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly IAgencyRepository _agencyRepository;
    private readonly FcAgencyOptions _fcAgencyOptions;
    private readonly IAuditService<CreateExternalUserProfileForInternalFcUserUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly IClock _clock;
    private readonly ILogger<CreateExternalUserProfileForInternalFcUserUseCase> _logger;

    public CreateExternalUserProfileForInternalFcUserUseCase(
        IAccountRegistrationService accountRegistrationService,
        IRetrieveUserAccountsService retrieveUserAccountsService,
        IAgencyRepository agencyRepository,
        IOptions<FcAgencyOptions> fcAgencyOptions,
        RequestContext requestContext,
        IAuditService<CreateExternalUserProfileForInternalFcUserUseCase> auditService,
        IClock clock,
        ILogger<CreateExternalUserProfileForInternalFcUserUseCase> logger)
    {
        _accountRegistrationService = Guard.Against.Null(accountRegistrationService);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _agencyRepository = Guard.Against.Null(agencyRepository);
        _fcAgencyOptions = Guard.Against.Null(fcAgencyOptions).Value;
        _auditService = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _clock = Guard.Against.Null(clock);
        _logger = logger;
    }

    /// <summary>
    /// Processes the <see cref="InternalFcUserAccountApprovedEvent"/> event message to
    /// create the new external user account for an FC user. 
    /// </summary>
    /// <param name="eventMessage">A populated <see cref="InternalFcUserAccountApprovedEvent"/> message containing the required detail needed to process this request</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> object for the consumer, indicating whether the operation completed successfully, or an error if not.</returns>
    public async Task<Result> ProcessAsync(
        InternalFcUserAccountApprovedEvent eventMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received event to add an FC user account to the external application, user email address is {emailAddress}, identityProvider is {identityProvider}. ",
            eventMessage.EmailAddress, eventMessage.IdentityProviderId);

        var emailIsPermitted = IsEmailDomainPermitted(eventMessage.EmailAddress);

        if (emailIsPermitted.IsFailure)
        {
            _logger.LogError("Failed to create external FC user account for email address {EmailAddress} with error {Error}",
                eventMessage.EmailAddress, emailIsPermitted.Error);
            return await HandleFailureAsync(eventMessage, emailIsPermitted.Error, cancellationToken);
        }

        var emailAddressInUse = await IsEmailAddressInUseAsync(eventMessage.EmailAddress, cancellationToken);

        if (emailAddressInUse)
        {
            _logger.LogError("User account already exists using the email address provided {EmailAddress}", eventMessage.EmailAddress);
            return await HandleFailureAsync(
                eventMessage,
                $"User account already exists using the email address provided {eventMessage.EmailAddress}",
                cancellationToken);
        }

        var fcAgency = await _agencyRepository.FindFcAgency(cancellationToken);

        if (fcAgency.HasNoValue)
        {
            _logger.LogError("User account for email address {EmailAddress} could not be created as there is no FC agency to assign the user to",
                eventMessage.EmailAddress);
            return await HandleFailureAsync(
                eventMessage,
                $"User account for email address {eventMessage.EmailAddress} could not be created as there is no FC agency to assign the user to",
                cancellationToken);
        }

        var createUserAccountResult = await CreateNewExternalAccountForFcUserAsync(eventMessage, fcAgency.Value, cancellationToken);

        if (createUserAccountResult.IsFailure)
        {
            _logger.LogError("Could not create the user account with the email address {EmailAddress} with the assigned FC agency, an error occurred {Error}",
                eventMessage.EmailAddress, createUserAccountResult.Error);
            return await HandleFailureAsync(
                eventMessage,
                $"Could not create the user account with the email address {eventMessage.EmailAddress} with the assigned FC agency, an error occurred {createUserAccountResult.Error}",
                cancellationToken);
        }

        return await HandleSuccessAsync(createUserAccountResult.Value, eventMessage, cancellationToken);
    }

    private async Task<Result> HandleSuccessAsync(
        AddExternalUserResponse newAccount,
        InternalFcUserAccountApprovedEvent eventMessage,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("User account with the email address {emailAddress} has been successfully " +
                         "created and assigned to the FC agency", eventMessage.EmailAddress);

        await CreateAuditForSuccess(eventMessage, newAccount, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> HandleFailureAsync(
        InternalFcUserAccountApprovedEvent eventMessage,
        string failureError,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(failureError);

        await CreateAuditForFailure(eventMessage, failureError, cancellationToken);

        return Result.Failure(failureError);
    }

    private Result IsEmailDomainPermitted(
        string emailAddress)
    {
        var domain = emailAddress.Split("@")[1];

        if (!_fcAgencyOptions.PermittedEmailDomainsForFcAgent.Any(x => x.Equals(domain, StringComparison.InvariantCultureIgnoreCase)))
        {
            _logger.LogWarning("User account with the email address {emailAddress} is not in the list of permitted domains, " +
                               "therefore, an external user account shall not be created as it cannot belong to the FC Agency", emailAddress);

            return Result.Failure($"Could not be assigned to FC agency - as invalid email domain {emailAddress}");
        }

        return Result.Success();
    }

    private async Task<bool> IsEmailAddressInUseAsync(
        string emailAddress,
        CancellationToken cancellationToken)
    {
        var getExistingAccount = await _retrieveUserAccountsService.RetrieveUserAccountByEmailAddressAsync(emailAddress, cancellationToken);

        if (getExistingAccount.HasNoValue)
        {
            return false;
        }

        _logger.LogWarning($"User account with the email {emailAddress} already exists in the external application, " +
                           $"account has user id of {getExistingAccount.Value.UserAccountId} " +
                           "therefore, an external user account cannot be created",
            emailAddress, getExistingAccount.Value.UserAccountId);

        return true;
    }

    private async Task<Result<AddExternalUserResponse>> CreateNewExternalAccountForFcUserAsync(
        InternalFcUserAccountApprovedEvent eventMessage,
        Agency fcAgency,
        CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant().ToDateTimeUtc();

        var userAccount = new AddExternalUserRequest
        {
            Email = eventMessage.EmailAddress,
            Status = UserAccountStatus.Active,
            FirstName = eventMessage.FirstName,
            LastName = eventMessage.LastName,
            AccountType = AccountTypeExternal.FcUser,
            AgencyId = fcAgency.Id,
            ContactAddress = CreateAddressFromOtherAddress(fcAgency.Address!),
            LastChanged = now,
            IdentityProviderId = eventMessage.IdentityProviderId,
            DateAcceptedPrivacyPolicy = now,
            DateAcceptedTermsAndConditions = now,
            PreferredContactMethod = PreferredContactMethod.Email,
            ContactTelephone = "0300 067 4000"
        };

        var addUserResult = await _accountRegistrationService.CreateUserAccountAsync(userAccount, cancellationToken);

        return addUserResult.IsSuccess 
            ? Result.Success(addUserResult.Value) 
            : Result.Failure<AddExternalUserResponse>($"Unable to save new user account for user with email address of {eventMessage.EmailAddress}, error was {addUserResult.Error}.");
    }

    private static Address CreateAddressFromOtherAddress(Address sourceAddress)
    {
        return new Address(sourceAddress.Line1, sourceAddress.Line2, sourceAddress.Line3, sourceAddress.Line4,
            sourceAddress.PostalCode);
    }

    private Task CreateAuditForSuccess(
        InternalFcUserAccountApprovedEvent eventMessage,
        AddExternalUserResponse newAccount,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.RegisterAccountDueToNewInternalFcUserAccountApprovalAuditEvent,
                eventMessage.ApprovedByInternalFcUserId,
                null,
                _requestContext,
                new
                {
                    newAccount.Id,
                    newAccount.AccountType,
                    newAccount.IdentityProviderId,
                    newAccount.Email,
                    eventMessage.ApprovedByInternalFcUserId
                }),
            cancellationToken);

    private Task CreateAuditForFailure(
        InternalFcUserAccountApprovedEvent eventMessage,
        string error,
        CancellationToken cancellationToken = default) =>
        _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.RegisterAccountDueToNewInternalFcUserAccountApprovalAuditEventFailure,
                eventMessage.ApprovedByInternalFcUserId,
                null,
                _requestContext,
                new
                {
                    eventMessage.IdentityProviderId,
                    Email = eventMessage.EmailAddress,
                    eventMessage.ApprovedByInternalFcUserId,
                    error
                }),
            cancellationToken);
}
